using Microsoft.Extensions.Caching.Memory;
using System.Net;
using MvcApp.Core;
using MvcApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MvcApp.Core.Abstractions;
using System.Net.Http.Json;

namespace MvcApp.Web.Middlewares
{
    public class VisitorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly string _ipGeolocationApiKey;
        private readonly ILogger<VisitorLoggingMiddleware> _logger;

        public VisitorLoggingMiddleware(RequestDelegate next, IMemoryCache cache, IConfiguration configuration, ILogger<VisitorLoggingMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _ipGeolocationApiKey = configuration["IpGeolocation:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "IpGeolocation:ApiKey is not configured.");
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UserDbContext dbContext, ISettingsService settings)
        {
            var trackingEnabled = await settings.GetAsync<bool>("VisitorTrackingEnabled") ?? true;
            if (!trackingEnabled)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value;

            if (path == "/" || path!.StartsWith("/Home") || path.StartsWith("/Subscription"))
            {
                var ipAddress = GetClientIpAddress(context);

                if (!string.IsNullOrEmpty(ipAddress))
                {
                    // Check if the IP address has been logged recently
                    if (!_cache.TryGetValue(ipAddress, out _))
                    {
                        var visitorLog = new VisitorLog
                        {
                            IpAddress = ipAddress,
                            VisitTime = DateTime.UtcNow
                        };

                        var location = await GetLocationAsync(ipAddress);
                        if (location != null)
                        {
                            visitorLog.Country = location.Country ?? "Unknown";
                            visitorLog.City = location.City ?? "Unknown";
                            visitorLog.Region = location.RegionName?? "Unknown";
                        }
                        else
                        {
                            visitorLog.Country = "Unknown";
                            visitorLog.City = "Unknown";
                            visitorLog.Region = "Unknown";
                        }

                        dbContext.VisitorLogs.Add(visitorLog);
                        await dbContext.SaveChangesAsync();

                        // Cache the IP address for a short period to avoid duplicate logging
                        _cache.Set(ipAddress, true, TimeSpan.FromMinutes(5));
                    }
                }
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress;

            // Check for X-Forwarded-For header
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = IPAddress.Parse(context.Request.Headers["X-Forwarded-For"].FirstOrDefault()!);
            }
            // Check for other common headers used by proxies
            else if (context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = IPAddress.Parse(context.Request.Headers["X-Real-IP"].FirstOrDefault()!);
            }

            // If the IP address is IPv6 loopback, use IPv4 loopback instead
            if (ipAddress != null && IPAddress.IsLoopback(ipAddress))
            {
                ipAddress = IPAddress.Loopback;
            }

            // If multiple IPs are present, take the first one
            var ipAddressString = ipAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddressString) && ipAddressString.Contains(","))
            {
                ipAddressString = ipAddressString.Split(',').First().Trim();
            }

            return ipAddressString!;
        }

        private async Task<Location?> GetLocationAsync(string ipAddress)
        {
            using var httpClient = new HttpClient();
            try
            {
                //var response = await httpClient.GetAsync($"https://api.ipgeolocation.io/ipgeo?apiKey={_ipGeolocationApiKey}&ip={ipAddress}");
                var response = await httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Location>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Locked)
            {
                // Handle 423 Locked status code
                _logger.LogWarning("IPGeolocation.io service is locked (423) for IP: {IpAddress}.", ipAddress);
                return null;
            }
            catch (HttpRequestException ex)
            {
                // Handle other HttpRequestExceptions
                _logger.LogError(ex, "HttpRequestException occurred while fetching location for IP: {IpAddress}.", ipAddress);
                return null;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                _logger.LogError(ex, "An unexpected error occurred while fetching location for IP: {IpAddress}.", ipAddress);
                return null;
            }
        }

        private class Location
        {
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? RegionName { get; set; }
        }
    }
}
