namespace MaxEnt.Web.Models;

public enum ExperimentRunStatus { Incomplete, Completed }

public class ExperimentLog
{
    public const int MaxContentLength = 55_000;

    /// <summary>Maximum number of log lines retained per log entry.</summary>
    public const int MaxRows = 2_000;

    /// <summary>Number of lines kept when trimming back from MaxRows.</summary>
    public const int TrimToRows = 1_000;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Source { get; init; } = "";       // e.g. "CartPole", "AntMaxEnt"
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Summary { get; set; } = "";      // first data row Time field
    public string Content { get; set; } = "";      // plain text, max 55k chars

    /// <summary>UTC timestamp of the first data row (experiment start).</summary>
    public string StartedTime { get; set; } = "";

    /// <summary>UTC timestamp of the last data row (experiment finish).</summary>
    public string FinishedTime { get; set; } = "";

    /// <summary>Number of CSV data rows (excluding the header).</summary>
    public int RowCount { get; set; }

    /// <summary>Completed only when the training loop finishes without cancellation.</summary>
    public ExperimentRunStatus Status { get; set; } = ExperimentRunStatus.Incomplete;

    /// <summary>
    /// Trims <paramref name="text"/> so it contains at most <see cref="MaxRows"/> lines.
    /// When over the limit the oldest lines are dropped, keeping the last <see cref="TrimToRows"/>.
    /// </summary>
    public static string TrimRows(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length <= MaxRows)
            return text;

        var kept = lines[^TrimToRows..];
        return string.Join('\n', kept);
    }

    /// <summary>
    /// Extracts the Time field (column index 2) from the first non-header CSV data row.
    /// Returns an empty string if no data row exists yet.
    /// CSV column order: ExperimentRunId,ExperimentRowId,Time,...
    /// </summary>
    public static string ExtractFirstRowTime(string text)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("ExperimentRunId,", StringComparison.Ordinal))
                continue;
            var parts = trimmed.Split(',');
            if (parts.Length > 2)
                return parts[2];
        }
        return "";
    }

    /// <summary>
    /// Extracts the Time field (column index 2) from the last non-header CSV data row.
    /// Returns an empty string if no data row exists yet.
    /// </summary>
    public static string ExtractLastRowTime(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("ExperimentRunId,", StringComparison.Ordinal))
                continue;
            var parts = trimmed.Split(',');
            if (parts.Length > 2)
                return parts[2];
        }
        return "";
    }

    /// <summary>Counts non-header CSV data rows in <paramref name="text"/>.</summary>
    public static int CountDataRows(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(l => !l.TrimStart().StartsWith("ExperimentRunId,", StringComparison.Ordinal) && l.Trim().Length > 0);

    public static ExperimentLog Create(string source, string fullText)
    {
        fullText = TrimRows(fullText);

        var truncated = fullText.Length > MaxContentLength
            ? fullText[^MaxContentLength..]
            : fullText;

        return new ExperimentLog
        {
            Source = source,
            Summary = ExtractFirstRowTime(truncated),
            StartedTime = ExtractFirstRowTime(truncated),
            FinishedTime = ExtractLastRowTime(truncated),
            RowCount = CountDataRows(truncated),
            Content = truncated
            // Status defaults to Incomplete; caller must set Completed explicitly
        };
    }
}
