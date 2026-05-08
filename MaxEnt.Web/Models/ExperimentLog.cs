namespace MaxEnt.Web.Models;

public class ExperimentLog
{
    public const int MaxContentLength = 55_000;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Source { get; init; } = "";       // e.g. "CartPole", "AntMaxEnt"
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Summary { get; init; } = "";      // last line of the log
    public string Content { get; init; } = "";      // plain text, max 55k chars

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
        };
    }
}
