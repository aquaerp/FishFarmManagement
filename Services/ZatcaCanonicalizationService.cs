using System.Data;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed class ZatcaCanonicalizationService
{
    private readonly FishFarmContext _context;
    private readonly ZatcaInvoiceHashService _hashService = new();

    public ZatcaCanonicalizationService(FishFarmContext context) => _context = context;

    public ZatcaCanonicalizationEvidence CanonicalizeAndHash(long envelopeId, string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var envelope = _context.ZatcaDocumentEnvelopes.AsNoTracking()
            .SingleOrDefault(value => value.Id == envelopeId)
            ?? throw new InvalidOperationException("The ZATCA envelope does not exist.");
        var outbox = _context.ZatcaOutboxMessages.Single(value => value.ZatcaDocumentEnvelopeId == envelopeId);
        if (outbox.Status != ZatcaEnvelopeState.AwaitingCanonicalization)
            throw new InvalidOperationException("Only an envelope awaiting canonicalization can be hashed.");
        if (_context.ZatcaCanonicalizationEvidence.Any(value => value.ZatcaDocumentEnvelopeId == envelopeId))
            throw new InvalidOperationException("Canonicalization evidence already exists for this envelope.");
        var result = _hashService.Compute(envelope.UnsignedXml);
        var evidence = new ZatcaCanonicalizationEvidence
        {
            ZatcaDocumentEnvelopeId = envelope.Id,
            CanonicalizationAlgorithm = ZatcaInvoiceHashService.CanonicalizationAlgorithm,
            DigestAlgorithm = ZatcaInvoiceHashService.DigestAlgorithm,
            InvoiceHashHex = result.Hex,
            InvoiceHashBase64 = result.Base64,
            CanonicalXmlSha256Base64 = result.Base64,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            Reason = reason.Trim()
        };
        outbox.Status = ZatcaEnvelopeState.AwaitingSignature;
        outbox.UpdatedAtUtc = DateTime.UtcNow;
        _context.ZatcaCanonicalizationEvidence.Add(evidence);
        _context.SaveChanges();
        transaction.Commit();
        return evidence;
    }
}
