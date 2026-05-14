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
    public string Summary { get; set; } = "";      // last line of the log
    public string Content { get; set; } = "";      // plain text, max 55k chars

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

    public static ExperimentLog Create(string source, string fullText)
    {
        fullText = TrimRows(fullText);

        var truncated = fullText.Length > MaxContentLength
            ? fullText[^MaxContentLength..]
            : fullText;

        var lastLine = truncated.TrimEnd()
                                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                .LastOrDefault() ?? "";

        return new ExperimentLog
        {
            Source = source,
            Summary = lastLine.Trim(),
            Content = truncated
            // Status defaults to Incomplete; caller must set Completed explicitly
        };
    }
}
