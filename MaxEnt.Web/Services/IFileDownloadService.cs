namespace MaxEnt.Web.Services;

public interface IFileDownloadService
{
    Task DownloadVfsFileAsync(string vfsFilePath, string outputFileName);
    Task DownloadTextAsync(string content, string outputFileName);
}
