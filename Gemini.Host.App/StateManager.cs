using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gemini.Host.App;

internal class JsonStateManager
{
    // Changed value to string since URLs are strings, avoiding JsonElement conversion issues
    private Dictionary<string, string> stateDictionary = [];

    public bool TryGetState(string key, out string? value)
    {
        return stateDictionary.TryGetValue(key, out value);
    }

    public void SetState(string key, string value)
    {
        if (value is null)
        {
            return;
        }

        stateDictionary[key] = value;
    }

    public async Task LoadAsync(string serialisedPayload)
    {
        using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(serialisedPayload));
        stateDictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(memoryStream) ?? [];
    }

    public virtual async Task<string> SaveAsync()
    {
        using MemoryStream memoryStream = new();
        await JsonSerializer.SerializeAsync(memoryStream, stateDictionary);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}

internal class FileJsonStateManager(string fileName) : JsonStateManager
{
    public string StoreFileName { get; } = fileName;

    // A semaphore ensures that if two tabs finish navigating at the exact same millisecond,
    // they line up nicely instead of throwing a file-in-use exception.
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public async Task LoadAsync()
    {
        if (!File.Exists(StoreFileName))
        {
            return;
        }

        await _fileLock.WaitAsync();
        try
        {
            var serialisedPayload = await File.ReadAllTextAsync(StoreFileName, Encoding.UTF8);
            await LoadAsync(serialisedPayload);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public override async Task<string> SaveAsync()
    {
        var serialisedPayload = await base.SaveAsync();

        await _fileLock.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(StoreFileName, serialisedPayload, Encoding.UTF8);
        }
        finally
        {
            _fileLock.Release();
        }

        return serialisedPayload;
    }
}