using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed record ControlledDocumentDraftRequest(string DocumentCode, string Version, string Title,
    string Category, string Owner, string StorageReference, string ContentSha256, string? ChangeSummary = null);

public sealed class DocumentControlService
{
    private readonly FishFarmContext _context;
    public DocumentControlService(FishFarmContext context) => _context = context;

    public ControlledDocument CreateDraft(ControlledDocumentDraftRequest request, string actor, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        Require(actor, 100, "Actor"); Require(reason, 500, "Reason"); Require(request.DocumentCode, 50, "Document code");
        Require(request.Version, 30, "Version"); Require(request.Title, 200, "Title"); Require(request.Category, 100, "Category");
        Require(request.Owner, 100, "Owner"); Require(request.StorageReference, 500, "Storage reference");
        if (string.IsNullOrWhiteSpace(request.ContentSha256) || request.ContentSha256.Trim().Length != 64)
            throw new InvalidOperationException("A SHA-256 content fingerprint is required.");
        if (_context.ControlledDocuments.Any(value => value.DocumentCode == request.DocumentCode.Trim() && value.Version == request.Version.Trim()))
            throw new InvalidOperationException("This document code and version already exist.");
        var document = new ControlledDocument
        {
            DocumentCode = request.DocumentCode.Trim(), Version = request.Version.Trim(), Title = request.Title.Trim(),
            Category = request.Category.Trim(), Owner = request.Owner.Trim(), StorageReference = request.StorageReference.Trim(),
            ContentSha256 = request.ContentSha256.Trim().ToUpperInvariant(), ChangeSummary = request.ChangeSummary?.Trim(),
            CreatedBy = actor.Trim(), CreatedAtUtc = DateTime.UtcNow
        };
        _context.ControlledDocuments.Add(document);
        _context.SaveChanges();
        Audit(document, "CreateDocumentDraft", actor, reason);
        _context.SaveChanges();
        return document;
    }

    public ControlledDocument Approve(int documentId, DateTime effectiveDate, string approver, string reason)
    {
        Require(approver, 100, "Approver"); Require(reason, 500, "Approval reason");
        var document = _context.ControlledDocuments.SingleOrDefault(value => value.Id == documentId)
            ?? throw new InvalidOperationException("The controlled document does not exist.");
        if (document.Status != ControlledDocumentStatus.Draft) throw new InvalidOperationException("Only a draft can be approved.");
        if (string.Equals(document.CreatedBy, approver, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A controlled document requires independent approval.");
        if (effectiveDate.Date < document.CreatedAtUtc.Date) throw new InvalidOperationException("The effective date cannot precede creation.");
        foreach (var previous in _context.ControlledDocuments.Where(value => value.DocumentCode == document.DocumentCode && value.Status == ControlledDocumentStatus.Approved))
        {
            previous.Status = ControlledDocumentStatus.Obsolete;
            previous.ObsoletedBy = approver.Trim(); previous.ObsoletedAtUtc = DateTime.UtcNow;
        }
        document.Status = ControlledDocumentStatus.Approved; document.EffectiveDate = effectiveDate.Date;
        document.ApprovedBy = approver.Trim(); document.ApprovedAtUtc = DateTime.UtcNow; document.ApprovalReason = reason.Trim();
        Audit(document, "ApproveControlledDocument", approver, reason);
        _context.SaveChanges();
        return document;
    }

    private void Audit(ControlledDocument document, string action, string actor, string reason) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow, EntityType = nameof(ControlledDocument), EntityId = document.Id.ToString(), Action = action,
            ActorUsername = actor.Trim(), Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(new { document.DocumentCode, document.Version, document.Status, document.ContentSha256 }),
            CorrelationId = Guid.NewGuid().ToString("N")
        });
    private static void Require(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength) throw new InvalidOperationException($"{field} is required.");
    }
}
