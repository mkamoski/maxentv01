using Microsoft.JSInterop;

namespace MaxEnt.Web.Services;

public class BlazorFileDownloadService(IJSRuntime js) : IFileDownloadService
{
    public async Task DownloadVfsFileAsync(string vfsFilePath, string outputFileName)
    {
        await using var stream = File.OpenRead(vfsFilePath);
        using var streamRef = new DotNetStreamReference(stream);
        var module = await js.InvokeAsync<IJSObjectReference>("import", "./file-download.js");
        await module.InvokeVoidAsync("downloadFileFromStream", outputFileName, streamRef);
    }

    public async Task DownloadTextAsync(string content, string outputFileName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        using var streamRef = new DotNetStreamReference(stream);
        var module = await js.InvokeAsync<IJSObjectReference>("import", "./file-download.js");
        await module.InvokeVoidAsync("downloadFileFromStream", outputFileName, streamRef);
    }
}
