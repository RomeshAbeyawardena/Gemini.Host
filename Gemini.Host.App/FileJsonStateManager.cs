using Gemini.Host.App.Models;
using System.Text;
using System.Text.Json;

namespace Gemini.Host.App;

internal class FileJsonStateManager : JsonStateManager
{
    // A semaphore ensures that if two tabs finish navigating at the exact same millisecond,
    // they line up nicely instead of throwing a file-in-use exception.
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private const string AppSettingsKey = "appSettings";

    private readonly Lazy<ApplicationSettings> _applicationSettings;

    public FileJsonStateManager(string fileName)
    {
        StoreFileName = fileName;
        _applicationSettings = new(() => {

            if (TryGetState(AppSettingsKey, out var settings) && settings is JsonElement element)
            {
                var appSettings = JsonSerializer.Deserialize<ApplicationSettingsDto>(element) ?? new();
                return new ApplicationSettings(appSettings);
            }

            return new();
        });
    }

    public string StoreFileName { get; }

    public ApplicationSettings Settings => _applicationSettings.Value;

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
        SetState(AppSettingsKey, Settings);

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