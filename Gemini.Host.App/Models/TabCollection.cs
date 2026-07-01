using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Gemini.Host.App.Models;

public class TabCollection : IReadOnlyDictionary<string, TabState>
{
    private readonly ConcurrentDictionary<string, TabState> tabs = [];
    public TabState this[string key] => tabs[key];

    public void Set(string key, TabState state)
    {
        tabs.AddOrUpdate(key, state, (existingKey, oldState) => state);
    }

    public IEnumerable<string> Keys => tabs.Keys;
    public IEnumerable<TabState> Values => tabs.Values;
    public int Count => tabs.Count;

    public bool ContainsKey(string key)
    {
        return tabs.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<string, TabState>> GetEnumerator()
    {
        return tabs.GetEnumerator();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TabState value)
    {
        return tabs.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
