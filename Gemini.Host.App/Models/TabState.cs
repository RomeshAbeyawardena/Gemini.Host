namespace Gemini.Host.App.Models;

public class TabState
{
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Title { get; set; } //stores the last retrieved title if Name was out of sync during the save operation
    public string? LastVisitedUrl { get; set; }
    public DateTimeOffset LastUpdatedTimestampUtc { get; set; }
}
