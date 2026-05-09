namespace MaxEnt.Web.Models;

/// <summary>Type tag so the UI can render appropriately.</summary>
public enum GraphKind { EntropyOverEpochs, StateOccupancyHeatmap, PolicyEntropyOverEpochs }

public class ExperimentGraph
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Source { get; init; } = "";        // "CartPole" | "AntMaxEnt"
    public GraphKind Kind { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Title { get; init; } = "";
    /// <summary>Complete SVG markup (UTF-8 text, no data-URI prefix).</summary>
    public string SvgContent { get; init; } = "";
}
