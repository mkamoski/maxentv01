namespace MaxEnt.Web.Services;

public class MaxEntArtifactManager : IMaxEntArtifactManager
{
    private const int MaxLogFiles = 10;
    private static readonly string ArtifactDir = Path.GetTempPath();

    public async Task SaveLogAsync(string content)
    {
        PruneOldestLogs();
        var path = Path.Combine(ArtifactDir, $"log_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.txt");
        await File.WriteAllTextAsync(path, content);
    }

    public async Task SaveGraphAsync(byte[] pngData)
    {
        var path = Path.Combine(ArtifactDir, "graph.png");
        await File.WriteAllBytesAsync(path, pngData);
    }

    private static void PruneOldestLogs()
    {
        var logs = Directory.GetFiles(ArtifactDir, "log_*.txt")
                            .OrderBy(File.GetCreationTimeUtc)
                            .ToArray();

        foreach (var file in logs[..Math.Max(0, logs.Length - MaxLogFiles + 1)])
            File.Delete(file);
    }
}
