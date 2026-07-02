using Gemini.Host.App.Models;
using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace Gemini.Host.App;

internal partial class BrowserTabComponent : UserControl
{
    private readonly TabState _tabState;
    private IntPtr? hIcon;
    private const string StartUrl = "https://gemini.google.com";

    [LibraryImport("user32.dll", EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(IntPtr handle);

    public event EventHandler<string>? TitleChanged;
    public event EventHandler<TabState>? TabStateUpdated;
    public event EventHandler<Icon>? IconChanged;

    public BrowserTabComponent(TabState tabState)
    {
        InitializeComponent();
        _tabState = tabState;

        browser = new()
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(browser);

        // Safely kick off asynchronous engine spin-up
        InitializeWebViewAsync();
    }

    private async void InitializeWebViewAsync()
    {
        // 1. Await engine initialization to guarantee CoreWebView2 is NOT null
        await browser.EnsureCoreWebView2Async();

        // 2. Safely bind events exactly ONCE right here
        browser.CoreWebView2.SourceChanged += Source_Changed;
        browser.CoreWebView2.NavigationCompleted += Browser_NavigationCompleted;
        browser.NavigationCompleted += Browser_NavigationCompleted;

        // 3. Initiate the initial route navigation safely
        browser.Source = new Uri(_tabState.LastVisitedUrl ?? StartUrl);
    }

    private void NotifyStateUpdated(string currentUrl, string documentTitle)
    {
        _tabState.LastVisitedUrl = currentUrl;
        _tabState.Title = documentTitle;

        // Safely marshal the state event update back onto the main UI thread 
        // to prevent cross-thread WinForms invalidation crashes
        if (InvokeRequired)
        {
            BeginInvoke(() => TabStateUpdated?.Invoke(this, _tabState));
        }
        else
        {
            TabStateUpdated?.Invoke(this, _tabState);
        }
    }

    private async Task GetIcon(CoreWebView2 core)
    {
        try
        {
            using var iconStream = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (iconStream != null && iconStream.Length > 0)
            {
                using var bitmap = new Bitmap(iconStream);

                if (hIcon.HasValue)
                {
                    DestroyIcon(hIcon.Value);
                }

                hIcon = bitmap.GetHicon();
                IconChanged?.Invoke(this, Icon.FromHandle(hIcon.Value));
            }
        }
        catch (Exception)
        {
            // Fail silently if favicon streaming drops due to a network glitch
        }
    }

    private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (browser.CoreWebView2 == null)
        {
            return;
        }

        var core = browser.CoreWebView2;
        await GetIcon(core);
        TitleChanged?.Invoke(this, core.DocumentTitle);

        NotifyStateUpdated(browser.Source.OriginalString, core.DocumentTitle);
    }

    private async void Source_Changed(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (browser.CoreWebView2 == null) return;

        var core = browser.CoreWebView2;
        TitleChanged?.Invoke(this, core.DocumentTitle);
        await GetIcon(core);
        NotifyStateUpdated(browser.Source.OriginalString, core.DocumentTitle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            browser.NavigationCompleted -= Browser_NavigationCompleted;
            browser.CoreWebView2.NavigationCompleted -= Browser_NavigationCompleted;
            browser.CoreWebView2?.SourceChanged -= Source_Changed;

            if (hIcon.HasValue)
            {
                DestroyIcon(hIcon.Value);
            }

            browser.Dispose();
        }
        base.Dispose(disposing);
    }
}