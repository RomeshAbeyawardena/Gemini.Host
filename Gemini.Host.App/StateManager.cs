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
