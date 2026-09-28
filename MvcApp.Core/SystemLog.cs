namespace MvcApp.Core;

public class SystemLogEntry
{
    public int Id { get; set; }

    /// <summary>Row insert time as stored by MySQL (the log server runs UTC).</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Timestamp rendered by the sink, e.g. 2026-09-28 08:31:37.272-04:00.</summary>
    public string? Timestamp { get; set; }

    public string? Level { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }

    /// <summary>Serilog properties as a JSON object.</summary>
    public string? Properties { get; set; }

    public DateTime TimestampLocal => TimestampUtc.Kind == DateTimeKind.Utc
        ? TimestampUtc.ToLocalTime()
        : DateTime.SpecifyKind(TimestampUtc, DateTimeKind.Utc).ToLocalTime();

    /// <summary>
    /// Local time taken from the sink's own rendered timestamp, which keeps
    /// millisecond precision. The _ts column only stores whole seconds, so this
    /// is what the UI shows; falls back to TimestampLocal when unparseable.
    /// </summary>
    public DateTime TimestampExactLocal =>
        DateTime.TryParseExact(Timestamp, "yyyy-MM-dd HH:mm:ss.fffzzz",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var exact)
            ? exact
            : TimestampLocal;
}

public class SystemLogQuery
{
    public string? Level { get; set; }
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SystemLogPage
{
    public List<SystemLogEntry> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
    public Dictionary<string, int> LevelCounts { get; set; } = new();
    public long DatabaseBytes { get; set; }
    public int RetentionDays { get; set; }
    public bool Available { get; set; } = true;
    public string? UnavailableReason { get; set; }
}
