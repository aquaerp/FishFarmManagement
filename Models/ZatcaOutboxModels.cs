namespace FishFarmManager.Models;

public enum ZatcaEnvelopeState { AwaitingCanonicalization, AwaitingSignature, ReadyForSubmission, Submitted, Rejected }

public sealed class ZatcaEgsUnit
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public long LastReservedInvoiceCounterValue { get; set; }
    public string PreviousInvoiceHashBase64 { get; set; } = string.Empty;
    public bool HasOpenEnvelope { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ICollection<ZatcaDocumentEnvelope> Envelopes { get; set; } = new List<ZatcaDocumentEnvelope>();
}

public sealed class ZatcaDocumentEnvelope
{
    public long Id { get; set; }
    public int ZatcaEgsUnitId { get; set; }
    public ZatcaEgsUnit ZatcaEgsUnit { get; set; } = null!;
    public string SourceEntityType { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public long InvoiceCounterValue { get; set; }
    public string PreviousInvoiceHashBase64 { get; set; } = string.Empty;
    public string UnsignedXml { get; set; } = string.Empty;
    public string LocalPayloadSha256Base64 { get; set; } = string.Empty;
    public ZatcaInvoiceProfile Profile { get; set; }
    public ZatcaDocumentKind Kind { get; set; }
    public ZatcaSubmissionRoute SubmissionRoute { get; set; }
    public ZatcaEnvelopeState State { get; set; } = ZatcaEnvelopeState.AwaitingCanonicalization;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public ZatcaOutboxMessage OutboxMessage { get; set; } = null!;
    public ZatcaCanonicalizationEvidence? CanonicalizationEvidence { get; set; }
}

public sealed class ZatcaCanonicalizationEvidence
{
    public long Id { get; set; }
    public long ZatcaDocumentEnvelopeId { get; set; }
    public ZatcaDocumentEnvelope ZatcaDocumentEnvelope { get; set; } = null!;
    public string CanonicalizationAlgorithm { get; set; } = string.Empty;
    public string DigestAlgorithm { get; set; } = string.Empty;
    public string InvoiceHashHex { get; set; } = string.Empty;
    public string InvoiceHashBase64 { get; set; } = string.Empty;
    public string CanonicalXmlSha256Base64 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ZatcaOutboxMessage
{
    public long Id { get; set; }
    public long ZatcaDocumentEnvelopeId { get; set; }
    public ZatcaDocumentEnvelope ZatcaDocumentEnvelope { get; set; } = null!;
    public ZatcaEnvelopeState Status { get; set; } = ZatcaEnvelopeState.AwaitingCanonicalization;
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
