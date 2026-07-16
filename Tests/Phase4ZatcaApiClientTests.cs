using System.Net;
using System.Text;
using System.Text.Json;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaApiClientTests
{
    [Fact]
    public async Task Reporting_UsesV2BasicAuthAndDeduplicatesAcceptedSubmission()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var calls = 0;
        var handler = new StubHandler(async request =>
        {
            calls++;
            captured = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK,
                "{\"reportingStatus\":\"REPORTED\",\"validationResults\":{\"warningMessages\":[]}}");
        });
        var client = new ZatcaReportingClient(new HttpClient(handler), Simulation(),
            new InMemoryZatcaSubmissionLedger());

        var first = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting));
        var second = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting));

        Assert.Equal(ZatcaSubmissionDisposition.Accepted, first.Disposition);
        Assert.True(second.FromLocalDeduplication);
        Assert.Equal(1, calls);
        Assert.Equal("v2", captured!.Headers.GetValues("accept-version").Single());
        Assert.Equal("0", captured.Headers.GetValues("Clearance-Status").Single());
        Assert.Equal("Basic", captured.Headers.Authorization!.Scheme);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("test-csid:test-secret")),
            captured.Headers.Authorization.Parameter);
        using var body = JsonDocument.Parse(capturedBody!);
        Assert.Equal("ArWKybHgkEDwNFzxarLEOeg3y8qacz6/g98zAuK+ClA=",
            body.RootElement.GetProperty("invoiceHash").GetString());
        Assert.Equal("<Invoice />", Encoding.UTF8.GetString(Convert.FromBase64String(
            body.RootElement.GetProperty("invoice").GetString()!)));
    }

    [Fact]
    public async Task Clearance_IsSeparateAndClassifiesWarningsAndTransientFailures()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            Json(HttpStatusCode.Accepted, "{\"clearanceStatus\":\"CLEARED\",\"warnings\":[{\"code\":\"W1\"}]}"),
            Json(HttpStatusCode.TooManyRequests, "{\"status\":\"RETRY\"}")
        });
        var handler = new StubHandler(request =>
        {
            Assert.EndsWith("/invoices/clearance/single", request.RequestUri!.AbsolutePath);
            Assert.Equal("1", request.Headers.GetValues("Clearance-Status").Single());
            return Task.FromResult(responses.Dequeue());
        });
        var client = new ZatcaClearanceClient(new HttpClient(handler), Simulation(),
            new InMemoryZatcaSubmissionLedger());
        var accepted = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Clearance));
        var retry = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Clearance) with { Uuid = Guid.NewGuid() });

        Assert.Equal(ZatcaSubmissionDisposition.AcceptedWithWarnings, accepted.Disposition);
        Assert.False(accepted.IsRetryable);
        Assert.Equal(ZatcaSubmissionDisposition.TransientFailure, retry.Disposition);
        Assert.True(retry.IsRetryable);
    }

    [Theory]
    [InlineData(HttpStatusCode.SeeOther, ZatcaSubmissionDisposition.RouteMismatch)]
    [InlineData(HttpStatusCode.BadRequest, ZatcaSubmissionDisposition.Rejected)]
    [InlineData(HttpStatusCode.Unauthorized, ZatcaSubmissionDisposition.AuthenticationFailed)]
    [InlineData(HttpStatusCode.Conflict, ZatcaSubmissionDisposition.DuplicateOrPreviouslyProcessed)]
    public async Task ResponseCodes_AreGoverned(HttpStatusCode code, ZatcaSubmissionDisposition expected)
    {
        var client = new ZatcaReportingClient(new HttpClient(new StubHandler(_ =>
            Task.FromResult(Json(code, "{\"status\":\"TEST\"}")))), Simulation(),
            new InMemoryZatcaSubmissionLedger());
        var result = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting));
        Assert.Equal(expected, result.Disposition);
        Assert.False(result.IsRetryable);
    }

    [Fact]
    public async Task Http200_WithAuthorityErrorsIsRejected()
    {
        var body = "{\"reportingStatus\":\"NOT_REPORTED\",\"validationResults\":{\"errorMessages\":[{\"code\":\"BR-KSA-01\"}]}}";
        var client = new ZatcaReportingClient(new HttpClient(new StubHandler(_ =>
            Task.FromResult(Json(HttpStatusCode.OK, body)))), Simulation(),
            new InMemoryZatcaSubmissionLedger());
        var result = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting));
        Assert.Equal(ZatcaSubmissionDisposition.Rejected, result.Disposition);
        Assert.Equal("NOT_REPORTED", result.AuthorityStatus);
    }

    [Fact]
    public async Task Network_IsDisabledByDefaultAndRejectsWrongRouteOrHost()
    {
        var never = new StubHandler(_ => throw new InvalidOperationException("Network must not run."));
        var disabled = new ZatcaReportingClient(new HttpClient(never),
            new ZatcaApiOptions(false, ZatcaApiEnvironment.Disabled, null, TimeSpan.FromSeconds(30)),
            new InMemoryZatcaSubmissionLedger());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            disabled.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting)));

        var wrongHost = new ZatcaReportingClient(new HttpClient(never),
            Simulation() with { BaseUri = new Uri("https://example.com/e-invoicing/simulation/") },
            new InMemoryZatcaSubmissionLedger());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrongHost.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting)));

        var reporting = new ZatcaReportingClient(new HttpClient(never), Simulation(),
            new InMemoryZatcaSubmissionLedger());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reporting.SubmitAsync(Submission(ZatcaSubmissionRoute.Clearance)));
    }

    [Fact]
    public async Task TransportOutage_IsRetryableAndDoesNotPoisonDeduplicationLedger()
    {
        var calls = 0;
        var client = new ZatcaReportingClient(new HttpClient(new StubHandler(_ =>
        {
            calls++;
            throw new HttpRequestException("offline");
        })), Simulation(), new InMemoryZatcaSubmissionLedger());

        var first = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting));
        var second = await client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting));

        Assert.Equal(ZatcaSubmissionDisposition.TransientFailure, first.Disposition);
        Assert.True(first.IsRetryable);
        Assert.False(second.FromLocalDeduplication);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ConcurrentDuplicate_IsCollapsedToOneAuthorityCall()
    {
        var calls = 0;
        var client = new ZatcaReportingClient(new HttpClient(new StubHandler(async _ =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(50);
            return Json(HttpStatusCode.OK, "{\"reportingStatus\":\"REPORTED\"}");
        })), Simulation(), new InMemoryZatcaSubmissionLedger());

        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => client.SubmitAsync(Submission(ZatcaSubmissionRoute.Reporting))));

        Assert.Equal(1, calls);
        Assert.All(results, result => Assert.True(result.IsAccepted));
    }

    [Fact]
    public async Task ExpiredOrNotYetValidCsid_BlocksBeforeNetwork()
    {
        var calls = 0;
        var client = new ZatcaReportingClient(new HttpClient(new StubHandler(_ =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        })), Simulation(), new InMemoryZatcaSubmissionLedger());
        var now = DateTimeOffset.UtcNow;
        var expired = Submission(ZatcaSubmissionRoute.Reporting) with
        {
            Authentication = new ZatcaApiAuthentication("expired", "secret", now.AddDays(-2), now.AddDays(-1))
        };
        var future = Submission(ZatcaSubmissionRoute.Reporting) with
        {
            Authentication = new ZatcaApiAuthentication("future", "secret", now.AddDays(1), now.AddDays(2))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SubmitAsync(expired));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SubmitAsync(future));
        Assert.Equal(0, calls);
    }

    private static ZatcaApiOptions Simulation() => new(true, ZatcaApiEnvironment.Simulation,
        new Uri("https://gw-fatoora.zatca.gov.sa/e-invoicing/simulation/"), TimeSpan.FromSeconds(30));

    private static ZatcaApiSubmission Submission(ZatcaSubmissionRoute route) => new(
        Guid.Parse("0c31a257-5088-40b5-9f48-bc2c84898d0a"),
        "ArWKybHgkEDwNFzxarLEOeg3y8qacz6/g98zAuK+ClA=", "<Invoice />", route,
        new ZatcaApiAuthentication("test-csid", "test-secret",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)));

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => handler(request);
    }
}
