namespace Gemini.Host.App.Models;

public class ApplicationSettingsDto
{
    public Dictionary<string, TabState> Tabs { get; set; } = [];
}

public class ApplicationSettings
{
    public ApplicationSettings()
    {
        
    }

    public ApplicationSettings(ApplicationSettingsDto applicationSettings)
    {
        foreach(var (key, value) in applicationSettings.Tabs)
        {
            Tabs.Set(key, value);
        }
    }

    public TabCollection Tabs { get; } = [];
    //Add additional settings
}