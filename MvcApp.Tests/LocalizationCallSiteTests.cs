using System.Text.RegularExpressions;
using Xunit;

namespace MvcApp.Tests;

/// <summary>
/// Guards the localization call sites against the four regressions found in the 2026-09-29 audit.
/// </summary>
/// <remarks>
/// <para>
/// These rules exist because every one of them was actually broken at some point, and none of them
/// was caught by comparing English output before and after: English renders the translation key, so
/// a call in the wrong position looks identical in English and only misbehaves in another language
/// or when a value is null. An English A/B cannot find this class of bug; these tests can.
/// </para>
/// <para>
/// They read the view sources from the repository rather than testing runtime behaviour, so they
/// assert on how a string is being localized, not on what it renders.
/// </para>
/// </remarks>
public class LocalizationCallSiteTests
{
    private static readonly string[] ViewExtensions = { ".cshtml", ".razor" };

    /// <summary>Locates the repository root by walking up to the solution file.</summary>
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.EnumerateFiles("MvcApp.slnx").Any() || dir.EnumerateFiles("*.sln").Any())
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Every view in the live source tree. The stale copies under MvcApp.Template are excluded:
    /// they are a protected v1.0.0-era snapshot that is deliberately not kept in sync, and build
    /// output is excluded because it is generated.
    /// </summary>
    private static IEnumerable<(string Path, string[] Lines)> SourceViews()
    {
        var root = RepositoryRoot();

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (!ViewExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/bin/") || normalized.Contains("/obj/") || normalized.Contains("/MvcApp.Template/"))
            {
                continue;
            }

            yield return (normalized, File.ReadAllLines(path));
        }
    }

    /// <summary>
    /// Database content must never be used as a translation key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>@Localizer[@plan.Description]</c> made the seeded VIP plan copy into translation keys and
    /// machine-translated the user's content into four languages, silently desynchronising the public
    /// page from the value an admin edits. Content has no translation workflow, so it must not be
    /// routed through the localizer at all.
    /// </para>
    /// <para>
    /// The rule targets member access, which is how content arrives. A bare identifier is allowed,
    /// because the legitimate form of this is a local holding hardcoded UI copy - for example
    /// <c>@Localizer[_unit]</c> where <c>_unit</c> is assigned the literal "Month(s)". A static
    /// analyser cannot tell a hardcoded local from a model property assigned to a local, so a
    /// reviewer has to confirm the identifier is a literal, not a value read from the database.
    /// </para>
    /// </remarks>
    [Fact]
    public void LocalizerIsNeverCalledOnModelOrDatabaseContent()
    {
        // Not a string literal, and reaches a member: Localizer[@plan.Description],
        // Localizer[plan.Description], Localizer[item.Label], Localizer[Model.Thing].
        var memberAccess = new Regex(@"Localizer\[\s*(?![""'\s])[^\]]*\.[A-Za-z_]", RegexOptions.Compiled);

        var offenders = new List<string>();
        foreach (var (path, lines) in SourceViews())
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (memberAccess.IsMatch(lines[i]))
                {
                    offenders.Add($"{path.Substring(RepositoryRoot().Length + 1)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Do not pass model or database values to the localizer - that translates user content and "
            + "desynchronises it from what an admin edits. Render the value directly:\n"
            + string.Join("\n", offenders));
    }    /// <summary>
    /// Localized text must never appear inside a JavaScript string literal.
    /// </summary>
    /// <remarks>
    /// Razor's JavaScript encoder escapes an apostrophe, so <c>'@Localizer["..."]'</c> happens to work
    /// today. It stops working the moment a translation contains a quote: the literal terminates, the
    /// whole script block fails to parse, and every handler in it dies silently. Use
    /// <c>JsonSerializer.Serialize</c> or read the text from a data attribute.
    /// </remarks>
    [Fact]
    public void LocalizerIsNeverEmbeddedInAScriptStringLiteral()
    {
        // A localizer call that sits inside a quoted run of characters, e.g. '... @Localizer["X"] ...'
        // or "prefix @Localizer["X"] suffix". Deliberately does not match @Localizer at the start of a
        // statement, which is the safe, intended form.
        var inLiteral = new Regex(@"""[^""\r\n]*Localizer\[|'[^'\r\n]*Localizer\[", RegexOptions.Compiled);

        var offenders = new List<string>();
        foreach (var (path, lines) in SourceViews())
        {
            var inScript = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("<script", StringComparison.OrdinalIgnoreCase)) inScript = true;

                if (inScript && inLiteral.IsMatch(line))
                {
                    offenders.Add($"{path.Substring(RepositoryRoot().Length + 1)}:{i + 1}  {line.Trim()}");
                }

                if (line.Contains("</script>", StringComparison.OrdinalIgnoreCase)) inScript = false;
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Localized text inside a JavaScript string literal breaks the script as soon as a "
            + "translation contains an apostrophe. Emit it with JsonSerializer.Serialize, or read it "
            + "from a data attribute:\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// Localized text must never appear inside an inline event-handler attribute.
    /// </summary>
    /// <remarks>
    /// <c>onsubmit="return confirm('@Localizer["..."]')"</c> is the same hazard as a script literal:
    /// one apostrophe in a translation silently disables the handler. The admin translate button had
    /// exactly this. Read the text from a <c>data-</c> attribute instead.
    /// </remarks>
    [Fact]
    public void LocalizerIsNeverEmbeddedInAnEventHandlerAttribute()
    {
        var handler = new Regex(@"\son[a-z]+\s*=\s*""[^""]*Localizer\[", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var offenders = new List<string>();
        foreach (var (path, lines) in SourceViews())
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (handler.IsMatch(lines[i]))
                {
                    offenders.Add($"{path.Substring(RepositoryRoot().Length + 1)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Localized text inside an on* handler attribute breaks the handler as soon as a "
            + "translation contains an apostrophe. Put the text in a data- attribute and read it from "
            + "JavaScript:\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// A translation key must not contain an HTML entity.
    /// </summary>
    /// <remarks>
    /// Razor HTML-encodes the value the localizer returns, so a key written as
    /// <c>"Language &amp; Currency"</c> renders a literal "&amp;" on the page. Decode entities to
    /// their characters in the key.
    /// </remarks>
    [Fact]
    public void TranslationKeysDoNotContainHtmlEntities()
    {
        var entity = new Regex(@"Localizer\[[""'][^""']*&(?:amp|lt|gt|quot|apos|nbsp|mdash|ndash|hellip|rsquo|lsquo|ldquo|rdquo|#\d+);", RegexOptions.Compiled);

        var offenders = new List<string>();
        foreach (var (path, lines) in SourceViews())
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (entity.IsMatch(lines[i]))
                {
                    offenders.Add($"{path.Substring(RepositoryRoot().Length + 1)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Razor HTML-encodes what the localizer returns, so an entity inside the key is encoded a "
            + "second time and displays literally. Use the decoded character:\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// Every boxicon class used in a view must exist in the shipped icon font.
    /// </summary>
    /// <remarks>
    /// A missing glyph renders as an empty box and is indistinguishable from a missing icon by eye.
    /// <c>bxs-translate</c>, <c>bx-grip-vertical</c>, <c>bx-puzzle</c>, <c>bx-tshirt</c> and
    /// <c>bx-wand</c> were all referenced by views and all absent from the bundled boxicons, so this
    /// had already produced five blank icons across the admin area and two templates.
    /// </remarks>
    [Fact]
    public void EveryBoxiconClassExistsInTheBundledStylesheet()
    {
        var cssPath = Path.Combine(RepositoryRoot(), "MvcApp.Web", "wwwroot", "lib", "boxicons", "css", "boxicons.min.css");
        Assert.True(File.Exists(cssPath), "Bundled boxicons stylesheet not found at " + cssPath);

        var css = File.ReadAllText(cssPath);

        var used = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (path, lines) in SourceViews())
        {
            foreach (var line in lines)
            {
                foreach (Match match in Regex.Matches(line, @"\b(bx-[a-z0-9-]+)\b"))
                {
                    used.Add(match.Groups[1].Value);
                }
            }
        }

        var missing = used.Where(icon => !css.Contains("." + icon, StringComparison.Ordinal)).ToList();

        Assert.True(
            missing.Count == 0,
            "These boxicon classes are used in views but do not exist in the bundled stylesheet, so "
            + "they render as blank boxes. Pick a class that exists in "
            + "MvcApp.Web/wwwroot/lib/boxicons/css/boxicons.min.css:\n  " + string.Join("\n  ", missing));
    }
}
