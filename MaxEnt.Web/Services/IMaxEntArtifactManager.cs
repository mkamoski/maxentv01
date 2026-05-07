namespace MaxEnt.Web.Services;

public interface IMaxEntArtifactManager
{
    Task SaveLogAsync(string content);
    Task SaveGraphAsync(byte[] pngData);
}
