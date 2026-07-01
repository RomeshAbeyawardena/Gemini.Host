using System.Text;
using System.Text.Json;

namespace Gemini.Host.App;

internal class JsonStateManager
{
    private Dictionary<string, object> stateDictionary = [];

    public bool TryGetState(string key, out object? value)
    {
        value = null;

        return stateDictionary.TryGetValue(key, out value);
    }

    public void SetState(string key, object value)
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

        stateDictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(memoryStream) ?? [];
    }

    public async virtual Task<string> SaveAsync()
    {
        using MemoryStream memoryStream = new();

        await JsonSerializer.SerializeAsync(memoryStream, stateDictionary);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}

internal class FileJsonStateManager(string fileName) : JsonStateManager
{
    public string StoreFileName { get; } = fileName;

    public async Task LoadAsync()
    {
        if (!File.Exists(StoreFileName))
        {
            return;
        }

        var serialisedPayload = File.ReadAllText(StoreFileName);

        await LoadAsync(serialisedPayload);
    }

    public async override Task<string> SaveAsync()
    {
        var serialisedPayload = await base.SaveAsync();
        
        File.WriteAllText(StoreFileName, serialisedPayload, Encoding.UTF8);

        return serialisedPayload;
    }
}
