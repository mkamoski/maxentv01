namespace MaxEnt.Web.Data;

public interface IExperimentGraphRepository
{
    Task<List<ExperimentGraph>> GetAllAsync();
    Task AddAsync(ExperimentGraph graph);
}
