using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MvcApp.Common.Payments;
using Xunit;

namespace MvcApp.Tests.Unit;

/// <summary>
/// The platform PayPal rail. Two things are worth pinning down without touching the real endpoint:
/// the captured total must be SUMMED across purchase units (a rail that creates one unit per item
/// would otherwise understate a multi-item order and verify a partially paid cart), and a rail with
/// no credentials must fail loudly rather than look like one that works.
/// </summary>
public class PayPalGatewayTests
{
    private const string TokenJson = """{"access_token":"test-token"}""";

    private static PayPalGateway Build(string? clientId, string? clientSecret, Func<HttpRequestMessage, HttpResponseMessage> jsonResponder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayPal:ClientId"] = clientId,
                ["PayPal:ClientSecret"] = clientSecret
            })
            .Build();

        return new PayPalGateway(
            new StubHttpClientFactory(jsonResponder),
            configuration,
            NullLogger<PayPalGateway>.Instance);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static Func<HttpRequestMessage, HttpResponseMessage> CaptureResponder(string captureJson) =>
        request => request.RequestUri!.AbsolutePath.Contains("oauth2/token")
            ? Json(TokenJson)
            : Json(captureJson);

    [Fact]
    public async Task The_captured_total_is_summed_across_purchase_units()
    {
        // Two units, as a two-item cart produces. Reading only the first would report 10.00.
        const string captureJson = """
            {"status":"COMPLETED","purchase_units":[
              {"payments":{"captures":[{"amount":{"value":"10.00","currency_code":"CAD"}}]}},
              {"payments":{"captures":[{"amount":{"value":"20.00","currency_code":"CAD"}}]}}
            ]}
            """;

        var gateway = Build("id", "secret", CaptureResponder(captureJson));

        var verification = await gateway.CaptureAsync("order-1");

        Assert.Equal(PaymentStatus.Completed, verification.Status);
        Assert.Equal(30.00m, verification.Amount);
        Assert.Equal("CAD", verification.Currency);

        // The point of the sum: a 30.00 cart is not satisfied by a 10.00 payment.
        Assert.True(verification.Matches(30.00m, "CAD"));
        Assert.False(verification.Matches(10.00m, "CAD"));
    }

    [Fact]
    public async Task A_completed_capture_without_an_amount_cannot_satisfy_any_check()
    {
        var gateway = Build("id", "secret", CaptureResponder("""{"status":"COMPLETED"}"""));

        var verification = await gateway.CaptureAsync("order-1");

        Assert.Equal(PaymentStatus.Completed, verification.Status);
        Assert.Equal(0m, verification.Amount);
        Assert.Equal(string.Empty, verification.Currency);

        // Status alone must never be enough — that is the whole reason Matches exists.
        Assert.False(verification.Matches(1m, "CAD"));

        // Nor may a zero/blank expectation be trivially satisfiable: an empty capture must not be
        // able to satisfy a forgotten expectation. This fails closed.
        Assert.False(verification.Matches(0m, string.Empty));
        Assert.False(verification.Matches(0m, "CAD"));
    }

    [Fact]
    public async Task A_non_completed_capture_is_reported_as_pending()
    {
        var gateway = Build("id", "secret", CaptureResponder("""{"status":"APPROVED"}"""));

        var verification = await gateway.CaptureAsync("order-1");

        Assert.Equal(PaymentStatus.Pending, verification.Status);
        Assert.False(verification.Matches(10.00m, "CAD"));
    }

    [Fact]
    public async Task Missing_credentials_fail_loudly_instead_of_silently()
    {
        var gateway = Build(clientId: null, clientSecret: null, CaptureResponder("{}"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.InitiateAsync(new PaymentRequest(5m, "CAD", "1", "test")));

        Assert.Contains("PayPal:ClientId", ex.Message);
    }

    [Fact]
    public void The_rail_reports_itself_truthfully()
    {
        var gateway = Build("id", "secret", CaptureResponder("{}"));

        Assert.Equal(PaymentProviderKind.PayPal, gateway.Kind);

        // Recurring is not implemented on this rail, so it must not claim to support it.
        Assert.False(gateway.SupportsRecurring);
    }

    private sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(responder));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
