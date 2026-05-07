namespace MaxEnt.Web.Data;

public class AppItemRepository(AppDbContext db) : IAppItemRepository
{
    public Task<List<AppItem>> GetAllAsync() =>
        db.AppItems.ToListAsync();

    public async Task AddAsync(AppItem item)
    {
        db.AppItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        if (await db.AppItems.FindAsync(id) is { } item)
        {
            db.AppItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
