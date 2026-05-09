namespace MaxEnt.Web.Models;

public enum ExperimentRunStatus { Incomplete, Completed }

public class ExperimentLog
{
    public const int MaxContentLength = 55_000;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Source { get; init; } = "";       // e.g. "CartPole", "AntMaxEnt"
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Summary { get; set; } = "";      // last line of the log
    public string Content { get; set; } = "";      // plain text, max 55k chars

    /// <summary>Completed only when the training loop finishes without cancellation.</summary>
    public ExperimentRunStatus Status { get; set; } = ExperimentRunStatus.Incomplete;

    public static ExperimentLog Create(string source, string fullText)
    {
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
