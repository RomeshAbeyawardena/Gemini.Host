using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace Gemini.Host.App;

internal partial class BrowserTabComponent : UserControl
{

    private FileJsonStateManager _stateManager;
    private IntPtr? hIcon;
    private const string StartUrl = "https://gemini.google.com";
    private const string LastVisitedUrlStateKey = "lastVisitedUrl";

    // Add this P/Invoke import at the top of your class
    [LibraryImport("user32.dll", EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(IntPtr handle);
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<Icon>? IconChanged;

    public BrowserTabComponent(FileJsonStateManager stateManager)
    {
        _stateManager = stateManager;

        browser = new()
        {
            Source = new Uri(GetStoredOrDefaultUrl),
            Dock = DockStyle.Fill
        };

        browser.NavigationCompleted += Browser_NavigationCompleted;
        Controls.Add(browser);
    }

    private string GetStoredOrDefaultUrl
    {
        get
        {
            var url = StartUrl;

            if (_stateManager.TryGetState(LastVisitedUrlStateKey, out var _url))
            {
                url = _url?.ToString() ?? StartUrl;
            }

            return url;
        }
    }

    private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        var core = browser.CoreWebView2;
        browser.CoreWebView2.SourceChanged += Source_Changed;
        TitleChanged?.Invoke(this, core.DocumentTitle);
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
            IconChanged?.Invoke(this, Icon.FromHandle(hIcon.Value));
        }

        var currentUrl = browser.Source.OriginalString;
        if (currentUrl != GetStoredOrDefaultUrl)
        {
            _stateManager.SetState(LastVisitedUrlStateKey, currentUrl);
            await _stateManager.SaveAsync();
        }
    }
    private async void Source_Changed(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        var core = browser.CoreWebView2;
        TitleChanged?.Invoke(this, core.DocumentTitle);

        var currentUrl = browser.Source.OriginalString;
        if (currentUrl != StartUrl)
        {
            _stateManager.SetState(LastVisitedUrlStateKey, currentUrl);
            await _stateManager.SaveAsync();
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
