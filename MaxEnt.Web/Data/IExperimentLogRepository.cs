namespace MaxEnt.Web.Data;

public interface IExperimentLogRepository
{
    /// <summary>Returns all logs ordered newest-first, without the Content column.</summary>
    Task<List<ExperimentLogSummary>> GetSummariesAsync();

    /// <summary>Returns the full log including Content.</summary>
    Task<ExperimentLog?> GetByIdAsync(Guid id);

    Task AddAsync(ExperimentLog log);

    /// <summary>Marks an existing log as Completed and persists the change.</summary>
    Task MarkCompletedAsync(Guid id);

    /// <summary>Writes the final log text into an existing log entry.</summary>
    Task UpdateContentAsync(Guid id, string fullText);
}

/// <summary>Lightweight projection used in list views — avoids loading Content blobs.</summary>
public record ExperimentLogSummary(Guid Id, string Source, DateTime CreatedAt, string Summary, ExperimentRunStatus Status);
