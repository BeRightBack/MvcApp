using System.Net;

namespace MvcApp.Tests.Host;

/// <summary>
/// Readiness is deliberately not instant: seeding runs off the startup path, so the endpoint is
/// Unhealthy until it completes. Tests poll rather than assume, mirroring how a real deploy gate
/// or load balancer probe would behave.
/// </summary>
internal static class HealthProbe
{
    public static async Task<string> WaitForHealthyAsync(
        HttpClient client,
        string path,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        var body = string.Empty;

        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync(path);
            body = (await response.Content.ReadAsStringAsync()).Trim();

            if (response.StatusCode == HttpStatusCode.OK && body == "Healthy")
            {
                return body;
            }

            await Task.Delay(250);
        }

        return body;
    }
}
