using System;
using System.IO;
using System.Threading.Tasks;

namespace DotDone.Data.Services;

public interface IFileStorageService
{
    string GetStorageDirectory();
    Task<(string storedFileName, string relativePath, long fileSizeBytes)> SaveFileAsync(string sourceFilePath);
    Task<(string storedFileName, string relativePath, long fileSizeBytes)> SaveStreamAsync(Stream stream, string originalFileName);
    bool DeleteFile(string relativePath);
    string GetFullPath(string relativePath);
    bool Exists(string relativePath);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _baseDirectory;

    public FileStorageService(string? customBaseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(customBaseDirectory))
        {
            _baseDirectory = customBaseDirectory;
        }
        else
        {
            // Portable location next to executable or AppContext.BaseDirectory
            _baseDirectory = Path.Combine(AppContext.BaseDirectory, "DotDone_Data", "Attachments");
        }

        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    public string GetStorageDirectory() => _baseDirectory;

    public async Task<(string storedFileName, string relativePath, long fileSizeBytes)> SaveFileAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source file not found", sourceFilePath);
        }

        var extension = Path.GetExtension(sourceFilePath);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_baseDirectory, storedFileName);

        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
        using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        var fileInfo = new FileInfo(destinationPath);
        var relativePath = Path.Combine("DotDone_Data", "Attachments", storedFileName);

        return (storedFileName, relativePath, fileInfo.Length);
    }

    public async Task<(string storedFileName, string relativePath, long fileSizeBytes)> SaveStreamAsync(Stream stream, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_baseDirectory, storedFileName);

        using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await stream.CopyToAsync(destinationStream);
        }

        var fileInfo = new FileInfo(destinationPath);
        var relativePath = Path.Combine("DotDone_Data", "Attachments", storedFileName);

        return (storedFileName, relativePath, fileInfo.Length);
    }

    public bool DeleteFile(string relativePath)
    {
        try
        {
            var fullPath = GetFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch
        {
            // Ignore deletion errors or log
        }
        return false;
    }

    public string GetFullPath(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        return Path.Combine(_baseDirectory, fileName);
    }

    public bool Exists(string relativePath)
    {
        return File.Exists(GetFullPath(relativePath));
    }
}
