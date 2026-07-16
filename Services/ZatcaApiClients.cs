using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public enum ZatcaApiEnvironment { Disabled, Simulation, Production }
public enum ZatcaSubmissionDisposition
{
    Accepted,
    AcceptedWithWarnings,
    Rejected,
    AuthenticationFailed,
    RouteMismatch,
    DuplicateOrPreviouslyProcessed,
    TransientFailure,
    ProtocolFailure
}

public sealed record ZatcaApiOptions(
    bool Enabled,
    ZatcaApiEnvironment Environment,
    Uri? BaseUri,
    TimeSpan Timeout,
    string AcceptVersion = "v2");

public sealed record ZatcaApiAuthentication(string Csid, string Secret);

public sealed record ZatcaApiSubmission(
    Guid Uuid,
    string InvoiceHashBase64,
    string Xml,
    ZatcaSubmissionRoute Route,
    ZatcaApiAuthentication Authentication);

public sealed record ZatcaApiResult(
    ZatcaSubmissionDisposition Disposition,
    int? HttpStatusCode,
    string AuthorityStatus,
    string ResponseBody,
    string IdempotencyKey,
    bool FromLocalDeduplication)
{
    public bool IsAccepted => Disposition is ZatcaSubmissionDisposition.Accepted
        or ZatcaSubmissionDisposition.AcceptedWithWarnings;
    public bool IsRetryable => Disposition == ZatcaSubmissionDisposition.TransientFailure;
}

public interface IZatcaSubmissionLedger
{
    bool TryGet(string idempotencyKey, out ZatcaApiResult result);
    void Store(string idempotencyKey, ZatcaApiResult result);
}

public sealed class InMemoryZatcaSubmissionLedger : IZatcaSubmissionLedger
{
    private readonly ConcurrentDictionary<string, ZatcaApiResult> _results = new(StringComparer.Ordinal);
    public bool TryGet(string idempotencyKey, out ZatcaApiResult result) =>
        _results.TryGetValue(idempotencyKey, out result!);
    public void Store(string idempotencyKey, ZatcaApiResult result) => _results[idempotencyKey] = result;
}

public interface IZatcaApiClient
{
    ZatcaSubmissionRoute Route { get; }
    Task<ZatcaApiResult> SubmitAsync(ZatcaApiSubmission submission, CancellationToken cancellationToken = default);
}

public sealed class ZatcaClearanceClient : ZatcaApiClientBase
{
    public ZatcaClearanceClient(HttpClient httpClient, ZatcaApiOptions options, IZatcaSubmissionLedger ledger)
        : base(httpClient, options, ledger) { }
    public override ZatcaSubmissionRoute Route => ZatcaSubmissionRoute.Clearance;
    protected override string RelativeEndpoint => "invoices/clearance/single";
}

public sealed class ZatcaReportingClient : ZatcaApiClientBase
{
    public ZatcaReportingClient(HttpClient httpClient, ZatcaApiOptions options, IZatcaSubmissionLedger ledger)
        : base(httpClient, options, ledger) { }
    public override ZatcaSubmissionRoute Route => ZatcaSubmissionRoute.Reporting;
    protected override string RelativeEndpoint => "invoices/reporting/single";
}

public abstract class ZatcaApiClientBase : IZatcaApiClient
{
    private const int MaxResponseCharacters = 64 * 1024;
    private readonly HttpClient _httpClient;
    private readonly ZatcaApiOptions _options;
    private readonly IZatcaSubmissionLedger _ledger;
    private readonly ConcurrentDictionary<string, Lazy<Task<ZatcaApiResult>>> _inFlight = new(StringComparer.Ordinal);

    protected ZatcaApiClientBase(HttpClient httpClient, ZatcaApiOptions options, IZatcaSubmissionLedger ledger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    public abstract ZatcaSubmissionRoute Route { get; }
    protected abstract string RelativeEndpoint { get; }

    public Task<ZatcaApiResult> SubmitAsync(ZatcaApiSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();
        ValidateSubmission(submission);
        var key = CreateIdempotencyKey(submission);
        if (_ledger.TryGet(key, out var existing))
            return Task.FromResult(existing with { FromLocalDeduplication = true });
        var operation = _inFlight.GetOrAdd(key, _ => new Lazy<Task<ZatcaApiResult>>(
            () => SendAndRecordAsync(submission, key, cancellationToken),
            LazyThreadSafetyMode.ExecutionAndPublication));
        return AwaitAndReleaseAsync(key, operation);
    }

    private async Task<ZatcaApiResult> AwaitAndReleaseAsync(
        string key, Lazy<Task<ZatcaApiResult>> operation)
    {
        try { return await operation.Value.ConfigureAwait(false); }
        finally { _inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<ZatcaApiResult>>>(key, operation)); }
    }

    private async Task<ZatcaApiResult> SendAndRecordAsync(
        ZatcaApiSubmission submission, string key, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);
        try
        {
            using var request = BuildRequest(submission);
            using var response = await _httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var body = await ReadBoundedAsync(response.Content, timeout.Token).ConfigureAwait(false);
            var result = Classify(response.StatusCode, body, key);
            if (ShouldPersist(result.Disposition)) _ledger.Store(key, result);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Transient(key, "ZATCA request timed out.");
        }
        catch (HttpRequestException exception)
        {
            return Transient(key, $"ZATCA transport failure: {exception.GetType().Name}");
        }
    }

    private HttpRequestMessage BuildRequest(ZatcaApiSubmission submission)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri!, RelativeEndpoint));
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{submission.Authentication.Csid}:{submission.Authentication.Secret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.TryAddWithoutValidation("accept-version", _options.AcceptVersion);
        request.Headers.TryAddWithoutValidation("accept-language", "en");
        request.Headers.TryAddWithoutValidation("Clearance-Status",
            Route == ZatcaSubmissionRoute.Clearance ? "1" : "0");
        var payload = JsonSerializer.Serialize(new
        {
            invoiceHash = submission.InvoiceHashBase64,
            uuid = submission.Uuid.ToString(),
            invoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(submission.Xml))
        });
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return request;
    }

    private ZatcaApiResult Classify(HttpStatusCode statusCode, string body, string key)
    {
        var code = (int)statusCode;
        var authorityStatus = ExtractAuthorityStatus(body);
        if (code == 200)
            return Result(HasErrors(body) || AuthorityRejected(authorityStatus)
                ? ZatcaSubmissionDisposition.Rejected
                : HasWarnings(body) ? ZatcaSubmissionDisposition.AcceptedWithWarnings
                : ZatcaSubmissionDisposition.Accepted, code, authorityStatus, body, key);
        if (code == 202)
            return Result(ZatcaSubmissionDisposition.AcceptedWithWarnings, code, authorityStatus, body, key);
        if (code == 303)
            return Result(ZatcaSubmissionDisposition.RouteMismatch, code, authorityStatus, body, key);
        if (code is 401 or 403)
            return Result(ZatcaSubmissionDisposition.AuthenticationFailed, code, authorityStatus, body, key);
        if (code == 409)
            return Result(ZatcaSubmissionDisposition.DuplicateOrPreviouslyProcessed, code, authorityStatus, body, key);
        if (code is 400 or 404 or 422)
            return Result(ZatcaSubmissionDisposition.Rejected, code, authorityStatus, body, key);
        if (code is 408 or 425 or 429 || code >= 500)
            return Result(ZatcaSubmissionDisposition.TransientFailure, code, authorityStatus, body, key);
        return Result(ZatcaSubmissionDisposition.ProtocolFailure, code, authorityStatus, body, key);
    }

    private void ValidateOptions()
    {
        if (!_options.Enabled || _options.Environment == ZatcaApiEnvironment.Disabled)
            throw new InvalidOperationException("ZATCA network integration is disabled by configuration.");
        if (_options.BaseUri is null || !_options.BaseUri.IsAbsoluteUri || _options.BaseUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("ZATCA BaseUri must be an absolute HTTPS URI.");
        if (!string.Equals(_options.BaseUri.Host, "gw-fatoora.zatca.gov.sa", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ZATCA BaseUri host is not allowed.");
        var expectedPath = _options.Environment == ZatcaApiEnvironment.Simulation
            ? "/e-invoicing/simulation/"
            : "/e-invoicing/core/";
        if (!_options.BaseUri.AbsolutePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"ZATCA BaseUri must end with {expectedPath} for this environment.");
        if (_options.Timeout <= TimeSpan.Zero || _options.Timeout > TimeSpan.FromMinutes(2))
            throw new InvalidOperationException("ZATCA timeout must be greater than zero and no more than two minutes.");
        if (_options.AcceptVersion != "v2")
            throw new InvalidOperationException("Only the governed ZATCA v2 API is supported.");
    }

    private void ValidateSubmission(ZatcaApiSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (submission.Route != Route)
            throw new InvalidOperationException($"A {submission.Route} document cannot be submitted through the {Route} client.");
        if (submission.Uuid == Guid.Empty) throw new InvalidOperationException("ZATCA UUID is required.");
        try
        {
            if (Convert.FromBase64String(submission.InvoiceHashBase64).Length != 32)
                throw new InvalidOperationException("ZATCA invoiceHash must be a Base64 SHA-256 value.");
        }
        catch (FormatException) { throw new InvalidOperationException("ZATCA invoiceHash must be valid Base64."); }
        if (string.IsNullOrWhiteSpace(submission.Xml)) throw new InvalidOperationException("ZATCA invoice XML is required.");
        if (string.IsNullOrWhiteSpace(submission.Authentication?.Csid)
            || string.IsNullOrWhiteSpace(submission.Authentication.Secret))
            throw new InvalidOperationException("ZATCA CSID and secret are required in memory.");
    }

    private string CreateIdempotencyKey(ZatcaApiSubmission submission)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{Route}\n{submission.Uuid:D}\n{submission.InvoiceHashBase64}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task<string> ReadBoundedAsync(HttpContent content, CancellationToken token)
    {
        var body = await content.ReadAsStringAsync(token).ConfigureAwait(false);
        return body.Length <= MaxResponseCharacters ? body : body[..MaxResponseCharacters];
    }

    private static string ExtractAuthorityStatus(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        try
        {
            using var json = JsonDocument.Parse(body);
            foreach (var name in new[] { "clearanceStatus", "reportingStatus", "status" })
                if (json.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
        }
        catch (JsonException) { }
        return string.Empty;
    }

    private static bool HasWarnings(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("warnings", out var warnings)
                && warnings.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                && !(warnings.ValueKind == JsonValueKind.Array && warnings.GetArrayLength() == 0)) return true;
            if (json.RootElement.TryGetProperty("validationResults", out var validation)
                && validation.TryGetProperty("warningMessages", out var messages)
                && messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0) return true;
        }
        catch (JsonException) { }
        return false;
    }

    private static bool HasErrors(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                && !(errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() == 0)) return true;
            if (json.RootElement.TryGetProperty("validationResults", out var validation)
                && validation.TryGetProperty("errorMessages", out var messages)
                && messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0) return true;
        }
        catch (JsonException) { }
        return false;
    }

    private static bool AuthorityRejected(string status) =>
        status.Contains("NOT_", StringComparison.OrdinalIgnoreCase)
        || status.Contains("REJECT", StringComparison.OrdinalIgnoreCase)
        || status.Contains("FAIL", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldPersist(ZatcaSubmissionDisposition disposition) => disposition is
        ZatcaSubmissionDisposition.Accepted or
        ZatcaSubmissionDisposition.AcceptedWithWarnings or
        ZatcaSubmissionDisposition.Rejected or
        ZatcaSubmissionDisposition.RouteMismatch or
        ZatcaSubmissionDisposition.DuplicateOrPreviouslyProcessed;

    private static ZatcaApiResult Result(ZatcaSubmissionDisposition disposition, int? code,
        string authorityStatus, string body, string key) =>
        new(disposition, code, authorityStatus, body, key, false);
    private static ZatcaApiResult Transient(string key, string message) =>
        Result(ZatcaSubmissionDisposition.TransientFailure, null, string.Empty, message, key);
}
