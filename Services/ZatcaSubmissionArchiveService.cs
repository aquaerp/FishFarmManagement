using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed class ZatcaSubmissionArchiveService
{
    private readonly FishFarmContext _context;

    public ZatcaSubmissionArchiveService(FishFarmContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public ZatcaSubmissionArchive RecordAttempt(
        long envelopeId,
        ZatcaApiSubmission submission,
        ZatcaApiResult result,
        DateTime startedAtUtc,
        DateTime completedAtUtc,
        string actor,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
        if (startedAtUtc.Kind != DateTimeKind.Utc || completedAtUtc.Kind != DateTimeKind.Utc
            || completedAtUtc < startedAtUtc)
            throw new InvalidOperationException("Archive timestamps must be ordered UTC values.");
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(result);

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var envelope = _context.ZatcaDocumentEnvelopes.AsNoTracking()
            .SingleOrDefault(value => value.Id == envelopeId)
            ?? throw new InvalidOperationException("The ZATCA envelope does not exist.");
        var evidence = _context.ZatcaCanonicalizationEvidence.AsNoTracking()
            .SingleOrDefault(value => value.ZatcaDocumentEnvelopeId == envelopeId)
            ?? throw new InvalidOperationException("Canonicalization evidence is required before archiving a submission.");
        var outbox = _context.ZatcaOutboxMessages.Single(value => value.ZatcaDocumentEnvelopeId == envelopeId);
        if (outbox.Status != ZatcaEnvelopeState.ReadyForSubmission)
            throw new InvalidOperationException("Only an outbox item ready for submission can be archived as an API attempt.");
        var unit = _context.ZatcaEgsUnits.Single(value => value.Id == envelope.ZatcaEgsUnitId);
        ValidateIdentity(envelope, evidence, submission, result);

        var attempt = checked(outbox.AttemptCount + 1);
        var requestPayload = JsonSerializer.Serialize(new
        {
            invoiceHash = submission.InvoiceHashBase64,
            uuid = submission.Uuid.ToString(),
            invoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(submission.Xml))
        });
        var archive = new ZatcaSubmissionArchive
        {
            ZatcaDocumentEnvelopeId = envelope.Id,
            AttemptNumber = attempt,
            Route = submission.Route,
            EndpointPath = submission.Route == ZatcaSubmissionRoute.Clearance
                ? "/invoices/clearance/single" : "/invoices/reporting/single",
            IdempotencyKey = result.IdempotencyKey,
            RequestPayloadJson = requestPayload,
            RequestSha256Base64 = Hash(requestPayload),
            SubmittedXml = submission.Xml,
            ResponseBody = result.ResponseBody,
            ResponseSha256Base64 = Hash(result.ResponseBody),
            HttpStatusCode = result.HttpStatusCode,
            Disposition = result.Disposition,
            AuthorityStatus = result.AuthorityStatus,
            IsAccepted = result.IsAccepted,
            IsRetryable = result.IsRetryable,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            DurationMilliseconds = checked((long)(completedAtUtc - startedAtUtc).TotalMilliseconds),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            Reason = reason.Trim()
        };
        outbox.AttemptCount = attempt;
        outbox.UpdatedAtUtc = completedAtUtc;
        ApplyOutcome(outbox, unit, evidence.InvoiceHashBase64, result, completedAtUtc);
        _context.ZatcaSubmissionArchives.Add(archive);
        _context.SaveChanges();
        transaction.Commit();
        return archive;
    }

    private static void ValidateIdentity(ZatcaDocumentEnvelope envelope,
        ZatcaCanonicalizationEvidence evidence, ZatcaApiSubmission submission, ZatcaApiResult result)
    {
        if (Guid.Parse(envelope.Uuid) != submission.Uuid)
            throw new InvalidOperationException("Submission UUID does not match the archived envelope.");
        if (envelope.SubmissionRoute != submission.Route)
            throw new InvalidOperationException("Submission route does not match the archived envelope.");
        if (!string.Equals(evidence.InvoiceHashBase64, submission.InvoiceHashBase64, StringComparison.Ordinal))
            throw new InvalidOperationException("Submission hash does not match canonicalization evidence.");
        var recomputed = new ZatcaInvoiceHashService().Compute(submission.Xml).Base64;
        if (!string.Equals(recomputed, submission.InvoiceHashBase64, StringComparison.Ordinal))
            throw new InvalidOperationException("Submitted XML does not match invoiceHash.");
        var expectedKey = ZatcaSubmissionIdentity.Create(submission.Route, submission.Uuid,
            submission.InvoiceHashBase64);
        if (!string.Equals(expectedKey, result.IdempotencyKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Submission result idempotency key is inconsistent.");
    }

    private static void ApplyOutcome(ZatcaOutboxMessage outbox, ZatcaEgsUnit unit,
        string acceptedHash, ZatcaApiResult result, DateTime completedAtUtc)
    {
        if (result.IsAccepted)
        {
            outbox.Status = ZatcaEnvelopeState.Submitted;
            outbox.NextAttemptAtUtc = DateTime.MaxValue;
            unit.PreviousInvoiceHashBase64 = acceptedHash;
            unit.HasOpenEnvelope = false;
            return;
        }
        if (result.IsRetryable)
        {
            outbox.Status = ZatcaEnvelopeState.ReadyForSubmission;
            var seconds = Math.Min(1800, 30 * Math.Pow(2, Math.Min(outbox.AttemptCount - 1, 6)));
            outbox.NextAttemptAtUtc = completedAtUtc.AddSeconds(seconds);
            return;
        }
        if (result.Disposition is ZatcaSubmissionDisposition.Rejected or ZatcaSubmissionDisposition.RouteMismatch)
        {
            outbox.Status = ZatcaEnvelopeState.Rejected;
            outbox.NextAttemptAtUtc = DateTime.MaxValue;
            return;
        }
        outbox.Status = ZatcaEnvelopeState.SubmissionBlocked;
        outbox.NextAttemptAtUtc = DateTime.MaxValue;
    }

    private static string Hash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));
}
