namespace MaxEnt.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppItem> AppItems => Set<AppItem>();
}
