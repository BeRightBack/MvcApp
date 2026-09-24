namespace MvcApp.Core.Abstractions;

public interface IIpGeolocationService
{
    Task<string> LookupCountryAsync(string ip);
}
