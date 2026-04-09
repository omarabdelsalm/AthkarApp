using System.Text.Json;

namespace AthkarApp.Services;

public interface IFileStorageService
{
    Task SaveJsonAsync<T>(string fileName, T data);
    Task<T?> LoadJsonAsync<T>(string fileName);
    Task SaveBinaryAsync(string fileName, byte[] data);
    Task<byte[]> LoadBinaryAsync(string fileName);
    bool Exists(string fileName);
    string GetFilePath(string fileName);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath = FileSystem.AppDataDirectory;

    public async Task SaveJsonAsync<T>(string fileName, T data)
    {
        var path = Path.Combine(_basePath, fileName);
        var json = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<T?> LoadJsonAsync<T>(string fileName)
    {
        var path = Path.Combine(_basePath, fileName);
        if (!File.Exists(path)) return default;
        
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SaveBinaryAsync(string fileName, byte[] data)
    {
        var path = Path.Combine(_basePath, fileName);
        await File.WriteAllBytesAsync(path, data);
    }

    public async Task<byte[]> LoadBinaryAsync(string fileName)
    {
        var path = Path.Combine(_basePath, fileName);
        if (!File.Exists(path)) return Array.Empty<byte>();
        
        return await File.ReadAllBytesAsync(path);
    }

    public bool Exists(string fileName)
    {
        return File.Exists(Path.Combine(_basePath, fileName));
    }

    public string GetFilePath(string fileName)
    {
        return Path.Combine(_basePath, fileName);
    }
}
