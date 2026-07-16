using System.Security.Cryptography;
using System.Text;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaEnvelopeTests
{
    [Fact]
    public void PrepareLocalDraft_AtomicallyReservesUuidIcvPihAndBlockedOutbox()
    {
        using var database = TestDatabase.Create();
        var service = new ZatcaEnvelopePreparationService(database.Context);
        var initialPih = Convert.ToBase64String(new byte[32]);
        var unit = service.RegisterEgsUnit("EGS-PILOT-001", initialPih,
            "zatca-admin", "Pilot EGS registered for local preparation");

        var result = service.PrepareLocalDraft(unit.Id, nameof(TaxInvoice), "501", Template(),
            "invoice-issuer", "Approved invoice reserved for G4 processing");

        Assert.Equal(1, result.Envelope.InvoiceCounterValue);
        Assert.NotEqual(Guid.Empty, Guid.Parse(result.Envelope.Uuid));
        Assert.Equal(initialPih, result.Envelope.PreviousInvoiceHashBase64);
        Assert.Equal(ZatcaEnvelopeState.AwaitingCanonicalization, result.Envelope.State);
        Assert.Equal(ZatcaEnvelopeState.AwaitingCanonicalization, result.OutboxMessage.Status);
        Assert.Equal(DateTime.MaxValue, result.OutboxMessage.NextAttemptAtUtc);
        Assert.Equal(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(result.Envelope.UnsignedXml))),
            result.Envelope.LocalPayloadSha256Base64);
        var storedUnit = database.Context.ZatcaEgsUnits.AsNoTracking().Single();
        Assert.True(storedUnit.HasOpenEnvelope);
        Assert.Equal(1, storedUnit.LastReservedInvoiceCounterValue);
    }

    [Fact]
    public void OpenEnvelope_BlocksNextIcvAndDuplicateSourceWithoutPartialRows()
    {
        using var database = TestDatabase.Create();
        var service = new ZatcaEnvelopePreparationService(database.Context);
        var unit = service.RegisterEgsUnit("EGS-PILOT-002", Convert.ToBase64String(new byte[32]),
            "zatca-admin", "Pilot EGS registered");
        service.PrepareLocalDraft(unit.Id, nameof(TaxInvoice), "601", Template(),
            "invoice-issuer", "First reservation");

        Assert.Throws<InvalidOperationException>(() => service.PrepareLocalDraft(
            unit.Id, nameof(TaxInvoice), "602", Template() with { DocumentNumber = "INV-2" },
            "invoice-issuer", "Must wait for canonicalization"));
        Assert.Single(database.Context.ZatcaDocumentEnvelopes);
        Assert.Single(database.Context.ZatcaOutboxMessages);
        Assert.Equal(1, database.Context.ZatcaEgsUnits.AsNoTracking().Single().LastReservedInvoiceCounterValue);
    }

    [Fact]
    public void EnvelopeAndOutboxHistory_AreProtectedByDatabaseTriggers()
    {
        using var database = TestDatabase.Create();
        var service = new ZatcaEnvelopePreparationService(database.Context);
        var unit = service.RegisterEgsUnit("EGS-PILOT-003", Convert.ToBase64String(new byte[32]),
            "zatca-admin", "Pilot EGS registered");
        var result = service.PrepareLocalDraft(unit.Id, nameof(TaxInvoice), "701", Template(),
            "invoice-issuer", "Protected evidence");
        database.Context.ChangeTracker.Clear();

        var envelope = database.Context.ZatcaDocumentEnvelopes.Single(value => value.Id == result.Envelope.Id);
        envelope.UnsignedXml = "<tampered/>";
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
        database.Context.ChangeTracker.Clear();
        var outbox = database.Context.ZatcaOutboxMessages.Single(value => value.Id == result.OutboxMessage.Id);
        database.Context.Remove(outbox);
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void Canonicalization_CreatesImmutableEvidenceAndMovesOutboxOnlyToAwaitingSignature()
    {
        using var database = TestDatabase.Create();
        var preparation = new ZatcaEnvelopePreparationService(database.Context);
        var initialPih = Convert.ToBase64String(new byte[32]);
        var unit = preparation.RegisterEgsUnit("EGS-PILOT-004", initialPih,
            "zatca-admin", "Pilot EGS registered");
        var envelope = preparation.PrepareLocalDraft(unit.Id, nameof(TaxInvoice), "801", Template(),
            "invoice-issuer", "Ready for canonicalization").Envelope;

        var evidence = new ZatcaCanonicalizationService(database.Context).CanonicalizeAndHash(
            envelope.Id, "zatca-hash-worker", "ZATCA exclusions and canonical hash applied");

        Assert.Equal(64, evidence.InvoiceHashHex.Length);
        Assert.Equal(44, evidence.InvoiceHashBase64.Length);
        Assert.Equal(ZatcaEnvelopeState.AwaitingSignature,
            database.Context.ZatcaOutboxMessages.AsNoTracking().Single().Status);
        var storedUnit = database.Context.ZatcaEgsUnits.AsNoTracking().Single();
        Assert.True(storedUnit.HasOpenEnvelope);
        Assert.Equal(initialPih, storedUnit.PreviousInvoiceHashBase64);
        Assert.Throws<InvalidOperationException>(() => new ZatcaCanonicalizationService(database.Context)
            .CanonicalizeAndHash(envelope.Id, "zatca-hash-worker", "Duplicate must fail"));

        database.Context.ChangeTracker.Clear();
        var storedEvidence = database.Context.ZatcaCanonicalizationEvidence.Single();
        storedEvidence.InvoiceHashHex = new string('0', 64);
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void InvoiceCounterSequence_ProducesTenThousandUniqueOrderedValuesAndGuardsBounds()
    {
        var values = new HashSet<long>();
        var current = 0L;
        for (var index = 0; index < 10_000; index++)
        {
            current = ZatcaInvoiceCounterSequence.Next(current);
            Assert.True(values.Add(current));
        }

        Assert.Equal(10_000, current);
        Assert.Equal(10_000, values.Count);
        Assert.Throws<InvalidOperationException>(() => ZatcaInvoiceCounterSequence.Next(-1));
        Assert.Throws<InvalidOperationException>(() => ZatcaInvoiceCounterSequence.Next(long.MaxValue));
    }

    private static ZatcaUblDocumentRequest Template() =>
        new("INV-1", Guid.Empty, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.FromHours(3)),
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
            var path = Path.Combine(Path.GetTempPath(), $"aquafarm-zatca-{Guid.NewGuid():N}.db");
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
