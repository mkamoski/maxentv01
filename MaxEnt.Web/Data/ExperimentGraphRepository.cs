namespace MaxEnt.Web.Data;

public class ExperimentGraphRepository(AppDbContext db) : IExperimentGraphRepository
{
    private const int MaxRows = 50;

    public Task<List<ExperimentGraph>> GetAllAsync() =>
        db.ExperimentGraphs
          .OrderByDescending(g => g.CreatedAt)
          .ToListAsync();

    public async Task AddAsync(ExperimentGraph graph)
    {
        await PruneAsync();
        db.ExperimentGraphs.Add(graph);
        await db.SaveChangesAsync();
    }

    private async Task PruneAsync()
    {
        var count = await db.ExperimentGraphs.CountAsync();
        var toDelete = count - MaxRows + 1;
        if (toDelete <= 0) return;

        var excess = await db.ExperimentGraphs
            .OrderBy(g => g.CreatedAt)
            .Take(toDelete)
            .ToListAsync();

        db.ExperimentGraphs.RemoveRange(excess);
        await db.SaveChangesAsync();
    }
}
