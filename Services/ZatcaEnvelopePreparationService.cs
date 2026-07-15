using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record ZatcaEnvelopePreparationResult(
    ZatcaDocumentEnvelope Envelope,
    ZatcaOutboxMessage OutboxMessage);

public sealed class ZatcaEnvelopePreparationService
{
    private readonly FishFarmContext _context;
    private readonly ZatcaUblGenerator _generator = new();

    public ZatcaEnvelopePreparationService(FishFarmContext context) => _context = context;

    public ZatcaEgsUnit RegisterEgsUnit(string deviceId, string initialPreviousInvoiceHashBase64,
        string actor, string reason)
    {
        Require(actor, nameof(actor));
        Require(reason, nameof(reason));
        Require(deviceId, nameof(deviceId));
        ValidateBase64(initialPreviousInvoiceHashBase64, "Initial PIH");
        if (_context.ZatcaEgsUnits.Any(value => value.DeviceId == deviceId.Trim()))
            throw new InvalidOperationException("The EGS device is already registered.");
        var unit = new ZatcaEgsUnit
        {
            DeviceId = deviceId.Trim(),
            PreviousInvoiceHashBase64 = initialPreviousInvoiceHashBase64,
            LastReservedInvoiceCounterValue = 0,
            HasOpenEnvelope = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim()
        };
        _context.ZatcaEgsUnits.Add(unit);
        _context.SaveChanges();
        return unit;
    }

    public ZatcaEnvelopePreparationResult PrepareLocalDraft(int egsUnitId,
        string sourceEntityType, string sourceEntityId, ZatcaUblDocumentRequest template,
        string actor, string reason)
    {
        Require(actor, nameof(actor));
        Require(reason, nameof(reason));
        Require(sourceEntityType, nameof(sourceEntityType));
        Require(sourceEntityId, nameof(sourceEntityId));
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var unit = _context.ZatcaEgsUnits.SingleOrDefault(value => value.Id == egsUnitId)
            ?? throw new InvalidOperationException("The EGS unit does not exist.");
        if (unit.HasOpenEnvelope)
            throw new InvalidOperationException("This EGS unit has an unresolved envelope; canonicalize and sign it before reserving another ICV.");
        if (_context.ZatcaDocumentEnvelopes.Any(value => value.SourceEntityType == sourceEntityType.Trim()
            && value.SourceEntityId == sourceEntityId.Trim()))
            throw new InvalidOperationException("This source document already has a ZATCA envelope.");

        var icv = checked(unit.LastReservedInvoiceCounterValue + 1);
        var request = template with
        {
            Uuid = Guid.NewGuid(),
            InvoiceCounterValue = icv,
            PreviousInvoiceHashBase64 = unit.PreviousInvoiceHashBase64
        };
        var unsignedXml = _generator.Generate(request).ToString(SaveOptions.DisableFormatting);
        var localHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(unsignedXml)));
        var envelope = new ZatcaDocumentEnvelope
        {
            ZatcaEgsUnitId = unit.Id,
            SourceEntityType = sourceEntityType.Trim(),
            SourceEntityId = sourceEntityId.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            Uuid = request.Uuid.ToString(),
            InvoiceCounterValue = icv,
            PreviousInvoiceHashBase64 = unit.PreviousInvoiceHashBase64,
            UnsignedXml = unsignedXml,
            LocalPayloadSha256Base64 = localHash,
            Profile = request.Profile,
            Kind = request.Kind,
            SubmissionRoute = request.SubmissionRoute,
            State = ZatcaEnvelopeState.AwaitingCanonicalization,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            Reason = reason.Trim()
        };
        var outbox = new ZatcaOutboxMessage
        {
            ZatcaDocumentEnvelope = envelope,
            Status = ZatcaEnvelopeState.AwaitingCanonicalization,
            AttemptCount = 0,
            NextAttemptAtUtc = DateTime.MaxValue,
            CreatedAtUtc = DateTime.UtcNow
        };
        unit.LastReservedInvoiceCounterValue = icv;
        unit.HasOpenEnvelope = true;
        _context.AddRange(envelope, outbox);
        _context.SaveChanges();
        transaction.Commit();
        return new ZatcaEnvelopePreparationResult(envelope, outbox);
    }

    private static void ValidateBase64(string value, string name)
    {
        Require(value, name);
        try { _ = Convert.FromBase64String(value); }
        catch (FormatException) { throw new InvalidOperationException($"{name} must be valid Base64."); }
    }

    private static void Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
    }
}
