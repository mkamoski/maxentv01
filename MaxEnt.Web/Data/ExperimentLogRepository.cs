namespace MaxEnt.Web.Data;

public class ExperimentLogRepository(AppDbContext db) : IExperimentLogRepository
{
    private const int MaxRows = 10;

    public Task<List<ExperimentLogSummary>> GetSummariesAsync() =>
        db.ExperimentLogs
          .OrderByDescending(l => l.CreatedAt)
          .Select(l => new ExperimentLogSummary(l.Id, l.Source, l.CreatedAt, l.Summary, l.Status))
          .ToListAsync();

    public Task<ExperimentLog?> GetByIdAsync(Guid id) =>
        db.ExperimentLogs.FindAsync(id).AsTask();

    public async Task AddAsync(ExperimentLog log)
    {
        await PruneAsync();
        db.ExperimentLogs.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task MarkCompletedAsync(Guid id)
    {
        var log = await db.ExperimentLogs.FindAsync(id);
        if (log is null) return;
        log.Status = ExperimentRunStatus.Completed;
        await db.SaveChangesAsync();
    }

    public async Task UpdateContentAsync(Guid id, string fullText)
    {
        var log = await db.ExperimentLogs.FindAsync(id);
        if (log is null) return;

        var truncated = fullText.Length > ExperimentLog.MaxContentLength
            ? fullText[^ExperimentLog.MaxContentLength..]
            : fullText;

        log.Content = truncated;
        log.Summary = truncated.TrimEnd()
                               .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                               .LastOrDefault()?.Trim() ?? "";

        await db.SaveChangesAsync();
    }

    private async Task PruneAsync()
    {
        var count = await db.ExperimentLogs.CountAsync();
        var toDelete = count - MaxRows + 1;
        if (toDelete <= 0) return;

        var excess = await db.ExperimentLogs
            .OrderBy(l => l.CreatedAt)
            .Take(toDelete)
            .ToListAsync();

        db.ExperimentLogs.RemoveRange(excess);
        await db.SaveChangesAsync();
    }
}
