namespace MvcApp.Core;

public sealed class NavGroup
{
    public string? Label { get; set; }
    public List<NavItem> Items { get; } = new();
}

public static class NavGrouping
{
    private static readonly Dictionary<string, string> GroupByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["blog|index"] = "Community",
        ["forum|index"] = "Community",
        ["chat|rooms"] = "Community",
        ["video|index"] = "Community",
        ["store|index"] = "Shop",
        ["iptvstore|index"] = "Shop",
        ["iptvhome|index"] = "Shop",
        ["discover|index"] = "Discover",
        ["likes|index"] = "Discover",
        ["matches|index"] = "Discover",
        ["gamification|index"] = "Discover",
        ["home|faq"] = "Help",
        ["home|contact"] = "Help",
    };

    public static List<NavGroup> Group(IReadOnlyList<NavItem> items)
    {
        var result = new List<NavGroup>();
        var byLabel = new Dictionary<string, NavGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var key = $"{item.Controller}|{item.Action}";
            if (!GroupByKey.TryGetValue(key, out var label))
            {
                result.Add(new NavGroup { Items = { item } });
                continue;
            }
            if (byLabel.TryGetValue(label, out var existing))
            {
                existing.Items.Add(item);
                continue;
            }
            var group = new NavGroup { Label = label };
            group.Items.Add(item);
            byLabel[label] = group;
            result.Add(group);
        }

        foreach (var group in result)
        {
            if (group.Items.Count == 1)
            {
                group.Label = null;
            }
        }
        return result;
    }
}