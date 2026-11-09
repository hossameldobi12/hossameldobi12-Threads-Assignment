using FileStorageDesign.Helpers;
using FileStorageDesign.Storage;
using System.Text.Json;
namespace OrderProcessingSystem.Storage;

public sealed class JsonFileStore
{
    private readonly string _dataDirectory;
    private readonly SemaphoreDic _semaphore = new();


    public JsonFileStore()
    {
        _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
    }


    public async Task<List<T>> ReadAsync<T>(string fileName, CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphore.GetFileLock(fileName);
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            return await ReadCoreAsync<T>(fileName, cancellationToken);

        }
        finally
        {
            semaphore.Release();

        }
    }
    public List<T> Read<T>(string fileName)
    {
        var semaphore = _semaphore.GetFileLock(fileName);
        semaphore.Wait();

        try
        {
            return ReadCore<T>(fileName);
        }
        finally
        {
            semaphore.Release();
        }
    }


    public async Task WriteAsync<T>(string fileName, List<T> data, CancellationToken ct)
    {
        var semaphore = _semaphore.GetFileLock(fileName);
        await semaphore.WaitAsync(ct);
        try
        {
            await WriteCoreAsync(fileName, data, ct);
        }

        finally
        {
            semaphore.Release();
        }

    }
    public void Write<T>(string fileName, List<T> data)
    {
        var semaphore = _semaphore.GetFileLock(fileName);
        semaphore.Wait();

        try
        {
            WriteCore(fileName, data);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task UpdateAsync<T>(string fileName, Action<List<T>> updateFunc, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(updateFunc);
        SemaphoreSlim semaphore = _semaphore.GetFileLock(fileName);
        await semaphore.WaitAsync(ct);
        try
        {
            var data = await ReadCoreAsync<T>(fileName, ct);
            ct.ThrowIfCancellationRequested();
            updateFunc(data);
            await WriteCoreAsync<T>(fileName, data, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }
    public void Update<T>(string fileName, Action<List<T>> updateFunc)
    {
        ArgumentNullException.ThrowIfNull(updateFunc);

        var semaphore = _semaphore.GetFileLock(fileName);
        semaphore.Wait();

        try
        {
            var data = ReadCore<T>(fileName);

            updateFunc(data);

            WriteCore(fileName, data);
        }
        finally
        {
            semaphore.Release();
        }
    }
    private async Task<List<T>> ReadCoreAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(filePath))
        {
            return new List<T>();
        }

        try
        {
            await using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions.Default, cancellationToken) ?? new List<T>();
            }
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException($"Error reading data from file {fileName}: {ex.Message}", ex);
        }
    }
    private List<T> ReadCore<T>(string fileName)
    {
        var filePath = Path.Combine(_dataDirectory, fileName);

        if (!File.Exists(filePath))
        {
            return new List<T>();
        }

        try
        {
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                return JsonSerializer.Deserialize<List<T>>(
                    stream,
                    JsonOptions.Default) ?? new List<T>();
            }
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException(
                $"Error reading data from file {fileName}: {ex.Message}", ex);
        }
    }

    private async Task WriteCoreAsync<T>(string fileName, List<T> data, CancellationToken ct)
    {

        Directory.CreateDirectory(_dataDirectory);

        string filePath = Path.Combine(_dataDirectory, fileName);

        string tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";


        try
        {
            await using (FileStream stream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, data, JsonOptions.Default, ct);
                await stream.FlushAsync(ct);
            }



            ct.ThrowIfCancellationRequested();

            File.Move(tempPath, filePath, overwrite: true);
        }

        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
    private void WriteCore<T>(string fileName, List<T> data)
    {
        Directory.CreateDirectory(_dataDirectory);

        string filePath = Path.Combine(_dataDirectory, fileName);
        string tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (FileStream stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, JsonOptions.Default);
                stream.Flush();
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

}
