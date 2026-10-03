using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MvcApp.Web.Storage;

/// <summary>
/// Stores submitted verification documents OUTSIDE the web root and resolves them by name.
///
/// Why: anything under wwwroot is served by the static-file middleware with no authorization, which
/// is how identity documents were reachable anonymously — the URL was the only thing protecting
/// them (audit 2.3). Keeping the bytes off the web root means the only way to read one is through
/// an action that carries an authorization attribute.
/// </summary>
public sealed class VerificationDocumentStore
{
    private readonly string _root;

    public VerificationDocumentStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration.GetValue<string>("Storage:VerificationPath");

        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "Verifications")
            : configured);
    }

    public string Root => _root;

    /// <summary>Creates the directory if needed and returns the absolute path to write to.</summary>
    public string CreatePath(string ownerKey, string fileName)
    {
        var directory = Path.Combine(_root, SafeSegment(ownerKey));
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, SafeSegment(fileName));
    }

    /// <summary>
    /// Absolute path for an existing document, or null when it is missing or would resolve outside
    /// the root. Defence in depth: a stored name is never allowed to escape.
    /// </summary>
    public string? TryResolve(string ownerKey, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(_root, SafeSegment(ownerKey), SafeSegment(fileName)));

        if (!candidate.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return File.Exists(candidate) ? candidate : null;
    }

    // Strips any directory component, so a hostile or accidental value cannot traverse.
    private static string SafeSegment(string value) => Path.GetFileName(value);
}
