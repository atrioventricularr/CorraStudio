using Microsoft.Extensions.Logging;
using CorraStudio.Domain.Interfaces.Services;
using System.IO;

namespace CorraStudio.Infrastructure.Services;

public class FileStorageService : IStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _basePath;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CorraStudio");
        
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(byte[] fileData, string fileName, string container)
    {
        var containerPath = Path.Combine(_basePath, container);
        if (!Directory.Exists(containerPath))
            Directory.CreateDirectory(containerPath);

        var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(containerPath, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, fileData);
        _logger.LogInformation("File saved: {FilePath}", filePath);
        
        return filePath;
    }

    public async Task<byte[]> GetFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
        
        return await File.ReadAllBytesAsync(filePath);
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted: {FilePath}", filePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<string> GetFileUrlAsync(string filePath)
    {
        return Task.FromResult($"file://{filePath}");
    }
}
