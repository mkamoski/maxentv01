namespace MaxEnt.Web.Data;

public interface IAppItemRepository
{
    Task<List<AppItem>> GetAllAsync();
    Task AddAsync(AppItem item);
    Task DeleteAsync(Guid id);
}
