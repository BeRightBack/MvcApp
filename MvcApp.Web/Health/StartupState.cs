namespace MvcApp.Web.Health;

/// <summary>
/// Records the outcome of startup seeding so readiness can report it. Seeding runs in the
/// background (so the listener comes up fast), which previously meant a failure was only visible
/// as a Fatal log line while the app happily served 500s to every request.
/// </summary>
public sealed class StartupState
{
    private volatile bool _seedingCompleted;
    private volatile string? _seedingError;

    public bool SeedingCompleted => _seedingCompleted;

    public string? SeedingError => _seedingError;

    public void MarkCompleted() => _seedingCompleted = true;

    public void MarkFailed(string error) => _seedingError = error;
}
