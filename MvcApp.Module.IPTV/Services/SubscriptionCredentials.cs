namespace MvcApp.Module.IPTV.Services;

/// <summary>
/// Generates the panel credentials handed to a customer when a subscription is activated.
///
/// Extracted so there is exactly one implementation: activation happens from two places now — the
/// customer's own claim path and the administrator's confirmation — and two copies of a credential
/// generator is how they silently diverge.
/// </summary>
public static class SubscriptionCredentials
{
    public static string GenerateUserCode() => Guid.NewGuid().ToString();

    public static string GeneratePassword() => Guid.NewGuid().ToString("N")[..8];
}
