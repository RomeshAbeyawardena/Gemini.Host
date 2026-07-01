using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace Gemini.Host.App;

internal partial class MainForm : Form
{
    // Add this P/Invoke import at the top of your class
    [LibraryImport("user32.dll", EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(IntPtr handle);
    private const string SubFolder = "gemini.host";
    private const string Filename = "gemini.host.settings.json";
    private const string StartUrl = "https://gemini.google.com";
    private const string LastVisitedUrlStateKey = "lastVisitedUrl";
    private readonly FileJsonStateManager stateManager = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        SubFolder,
        Filename));

    private IntPtr? hIcon;
    private const string Title = "Gemini App";

    private string GetStoredOrDefaultUrl
    {
        get
        {
            var url = StartUrl;

            if (stateManager.TryGetState(LastVisitedUrlStateKey, out var _url))
            {
                url = _url?.ToString() ?? StartUrl;
            }

            return url;
        }
    }

    public MainForm()
    {
        InitializeComponent();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            SubFolder);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        Task.Run(async () => await stateManager.LoadAsync())
            .ConfigureAwait(true).GetAwaiter().GetResult();

        this.Text = Title;

        browser = new()
        {
            Source = new Uri(GetStoredOrDefaultUrl),
            Dock = DockStyle.Fill
        };

        browser.NavigationCompleted += Browser_NavigationCompleted;
        Controls.Add(browser);
    }

    
    private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        var core = browser.CoreWebView2;
        browser.CoreWebView2.SourceChanged += Source_Changed;
        this.Text = $"{Title} - {core.DocumentTitle}";
        using var iconStream = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);

        if (iconStream != null && iconStream.Length > 0)
        {
            // 1. Load the PNG stream into a standard Bitmap
            using var bitmap = new Bitmap(iconStream);

            if (hIcon.HasValue)
            {
                DestroyIcon(hIcon.Value);
            }

            // 2. Get the native Windows handle for the icon
            hIcon = bitmap.GetHicon();

            // 3. Create the Icon object from the handle
            this.Icon = Icon.FromHandle(hIcon.Value);
        }

        var currentUrl = browser.Source.OriginalString;
        if (currentUrl != GetStoredOrDefaultUrl)
        {
            stateManager.SetState(LastVisitedUrlStateKey, currentUrl);
            await stateManager.SaveAsync();
        }
    }

    private async void Source_Changed(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        var currentUrl = browser.Source.OriginalString;
        if (currentUrl != StartUrl)
        {
            stateManager.SetState(LastVisitedUrlStateKey, currentUrl);
            await stateManager.SaveAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            browser.NavigationCompleted -= Browser_NavigationCompleted;
            browser.CoreWebView2.SourceChanged -= Source_Changed;

            if (hIcon.HasValue)
            {
                DestroyIcon(hIcon.Value);
            }
        }
        base.Dispose(disposing);
    }
}
