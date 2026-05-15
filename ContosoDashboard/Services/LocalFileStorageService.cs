using Microsoft.AspNetCore.Hosting;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsPath;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _uploadsPath = Path.Combine(environment.ContentRootPath, "AppData", "uploads");
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        Directory.CreateDirectory(_uploadsPath);

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_uploadsPath, storedFileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return Path.Combine("AppData", "uploads", storedFileName);
    }

    public Task DeleteAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task<string> GetUrlAsync(string filePath, TimeSpan expiration)
    {
        return Task.FromResult(filePath);
    }

    private string GetFullPath(string relativePath)
    {
        // Navigate up from uploads path to content root, then combine with relative path
        var contentRoot = Directory.GetParent(_uploadsPath)!.Parent!.FullName;
        return Path.Combine(contentRoot, relativePath);
    }
}
