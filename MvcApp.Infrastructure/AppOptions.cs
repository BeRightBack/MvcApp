namespace MvcApp.Infrastructure;

// Configuration option records bound in ServiceCollectionExtensions.AddInfrastructure
public record SmtpSettings(string From, string Host, int Port, string Username, string Password);
public record IpGeolocationOptions(string ApiKey);
public record DeepLOptions(string AuthKey, string SourceLang, string TargetLangEn);
