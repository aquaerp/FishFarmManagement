using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaSubmissionArchiveTests
{
    [Fact]
    public void AcceptedAttempt_IsArchivedAndAtomicallyAdvancesPihAndOpensEgs()
    {
        using var database = TestDatabase.Create();
        var prepared = PrepareReady(database.Context, "EGS-ARCHIVE-1", "901");
        var submission = Submission(prepared);
        var started = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);
        var result = Result(submission, ZatcaSubmissionDisposition.Accepted, 200, "CLEARED");

        var archive = new ZatcaSubmissionArchiveService(database.Context).RecordAttempt(
            prepared.Envelope.Id, submission, result, started, started.AddMilliseconds(245),
            "zatca-worker", "Clearance response archived");

        Assert.Equal(1, archive.AttemptNumber);
        Assert.Equal(245, archive.DurationMilliseconds);
        Assert.DoesNotContain("test-secret", archive.RequestPayloadJson);
        Assert.DoesNotContain("test-csid", archive.RequestPayloadJson);
        Assert.Equal(44, archive.RequestSha256Base64.Length);
        Assert.Equal(44, archive.ResponseSha256Base64.Length);
        var outbox = database.Context.ZatcaOutboxMessages.AsNoTracking().Single();
        Assert.Equal(ZatcaEnvelopeState.Submitted, outbox.Status);
        Assert.Equal(1, outbox.AttemptCount);
        var unit = database.Context.ZatcaEgsUnits.AsNoTracking().Single();
        Assert.False(unit.HasOpenEnvelope);
        Assert.Equal(submission.InvoiceHashBase64, unit.PreviousInvoiceHashBase64);
    }

    [Fact]
    public void TransientAttempt_IsArchivedAndSchedulesRetryWithoutAdvancingPih()
    {
        using var database = TestDatabase.Create();
        var initialPih = Convert.ToBase64String(new byte[32]);
        var prepared = PrepareReady(database.Context, "EGS-ARCHIVE-2", "902", initialPih);
        var submission = Submission(prepared);
        var completed = new DateTime(2026, 7, 16, 10, 1, 0, DateTimeKind.Utc);
        var result = Result(submission, ZatcaSubmissionDisposition.TransientFailure, 503, "RETRY");

        new ZatcaSubmissionArchiveService(database.Context).RecordAttempt(prepared.Envelope.Id,
            submission, result, completed.AddSeconds(-1), completed, "zatca-worker", "503 archived");

        var outbox = database.Context.ZatcaOutboxMessages.AsNoTracking().Single();
        Assert.Equal(ZatcaEnvelopeState.ReadyForSubmission, outbox.Status);
        Assert.Equal(completed.AddSeconds(30), outbox.NextAttemptAtUtc);
        var unit = database.Context.ZatcaEgsUnits.AsNoTracking().Single();
        Assert.True(unit.HasOpenEnvelope);
        Assert.Equal(initialPih, unit.PreviousInvoiceHashBase64);
    }

    [Fact]
    public void Archive_IsImmutableAndRejectsMismatchedHashOrPrematureAttempt()
    {
        using var database = TestDatabase.Create();
        var prepared = PrepareReady(database.Context, "EGS-ARCHIVE-3", "903");
        var submission = Submission(prepared);
        var now = new DateTime(2026, 7, 16, 10, 2, 0, DateTimeKind.Utc);
        var service = new ZatcaSubmissionArchiveService(database.Context);
        var wrong = submission with { InvoiceHashBase64 = Convert.ToBase64String(new byte[32]) };
        Assert.Throws<InvalidOperationException>(() => service.RecordAttempt(prepared.Envelope.Id,
            wrong, Result(wrong, ZatcaSubmissionDisposition.Accepted, 200, "CLEARED"),
            now, now, "zatca-worker", "Must fail"));

        var archive = service.RecordAttempt(prepared.Envelope.Id, submission,
            Result(submission, ZatcaSubmissionDisposition.Rejected, 400, "REJECTED"),
            now, now.AddMilliseconds(10), "zatca-worker", "Rejection archived");
        Assert.Equal(ZatcaEnvelopeState.Rejected,
            database.Context.ZatcaOutboxMessages.AsNoTracking().Single().Status);
        Assert.True(database.Context.ZatcaEgsUnits.AsNoTracking().Single().HasOpenEnvelope);
        database.Context.ChangeTracker.Clear();
        var stored = database.Context.ZatcaSubmissionArchives.Single(value => value.Id == archive.Id);
        stored.ResponseBody = "tampered";
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
        database.Context.ChangeTracker.Clear();
        stored = database.Context.ZatcaSubmissionArchives.Single(value => value.Id == archive.Id);
        database.Context.Remove(stored);
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    private static ZatcaEnvelopePreparationResult PrepareReady(FishFarmContext context,
        string deviceId, string sourceId, string? initialPih = null)
    {
        var preparation = new ZatcaEnvelopePreparationService(context);
        var unit = preparation.RegisterEgsUnit(deviceId, initialPih ?? Convert.ToBase64String(new byte[32]),
            "zatca-admin", "Archive test EGS");
        var prepared = preparation.PrepareLocalDraft(unit.Id, nameof(TaxInvoice), sourceId, Template(),
            "invoice-issuer", "Archive test envelope");
        _ = new ZatcaCanonicalizationService(context).CanonicalizeAndHash(prepared.Envelope.Id,
            "hash-worker", "Archive test hash");
        var outbox = context.ZatcaOutboxMessages.Single(value => value.Id == prepared.OutboxMessage.Id);
        outbox.Status = ZatcaEnvelopeState.ReadyForSubmission;
        outbox.NextAttemptAtUtc = DateTime.UtcNow;
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return new ZatcaEnvelopePreparationResult(
            context.ZatcaDocumentEnvelopes.AsNoTracking().Single(),
            context.ZatcaOutboxMessages.AsNoTracking().Single());
    }

    private static ZatcaApiSubmission Submission(ZatcaEnvelopePreparationResult prepared)
    {
        var hash = new ZatcaInvoiceHashService().Compute(prepared.Envelope.UnsignedXml).Base64;
        return new ZatcaApiSubmission(Guid.Parse(prepared.Envelope.Uuid), hash,
            prepared.Envelope.UnsignedXml, prepared.Envelope.SubmissionRoute,
            new ZatcaApiAuthentication("test-csid", "test-secret",
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    private static ZatcaApiResult Result(ZatcaApiSubmission submission,
        ZatcaSubmissionDisposition disposition, int code, string status) => new(
            disposition, code, status, $"{{\"status\":\"{status}\"}}",
            ZatcaSubmissionIdentity.Create(submission.Route, submission.Uuid, submission.InvoiceHashBase64), false);

    private static ZatcaUblDocumentRequest Template() => new(
        "INV-ARCHIVE-1", Guid.Empty,
        new DateTimeOffset(2026, 7, 16, 13, 0, 0, TimeSpan.FromHours(3)),
        ZatcaInvoiceProfile.Standard, ZatcaDocumentKind.TaxInvoice, 0, "ignored",
        Party("Aqua Farm", "310123456700003"), Party("Customer", "310987654300003"),
        new[] { new ZatcaDocumentLine("1", "Fish", 1m, "KGM", 100m, 0m, "S", 15m) });

    private static ZatcaParty Party(string name, string vat) =>
        new(name, vat, "King Road", "1234", "Riyadh", "12345");

    private sealed class TestDatabase : IDisposable
    {
        private readonly string _path;
        public FishFarmContext Context { get; }
        private TestDatabase(string path, FishFarmContext context) { _path = path; Context = context; }
        public static TestDatabase Create()
        {
            var path = Path.Combine(Path.GetTempPath(), $"aquafarm-zatca-archive-{Guid.NewGuid():N}.db");
            var context = new FishFarmContext(new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options);
            context.Database.Migrate();
            return new TestDatabase(path, context);
        }
        public void Dispose()
        {
            Context.Dispose();
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
