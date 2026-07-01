using Gemini.Host.App.Models;
using Microsoft.Web.WebView2.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gemini.Host.App;

internal partial class MainForm : Form
{
    private const string SubFolder = "gemini.host";
    private const string Filename = "gemini.host.settings.json";

    private readonly FileJsonStateManager stateManager;
    private ApplicationSettings settings = new();

    private const string Title = "Gemini App";

    public MainForm()
    {
        InitializeComponent();

        // 1. Establish file paths safely
        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SubFolder);
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        string fullSettingsPath = Path.Combine(appDataPath, Filename);
        stateManager = new FileJsonStateManager(fullSettingsPath);

        // 2. Perform UI thread safe synchronous initialization load block
        LoadApplicationSettingsSynchronously();

        // 3. Build UI layouts
        InitialiseControls();

        this.Text = Title;
    }

    private void LoadApplicationSettingsSynchronously()
    {
        try
        {
            // Block cleanly on task thread using modern pattern without deadlocking UI hooks
            var loadTask = Task.Run(async () => await stateManager.LoadAsync());
            loadTask.Wait();

            // Bind parsed states directly or default to a pristine application state context
            if (stateManager.Settings is ApplicationSettings savedSettings)
            {
                settings = savedSettings;
            }
        }
        catch (Exception)
        {
            // Fail safely to blank instantiation if settings payload file is corrupted
            settings = new ApplicationSettings();
        }
    }

    private void InitialiseControls()
    {
        tableLayoutPanel = new()
        {
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        tableLayoutPanel.ColumnStyles.Clear();
        tableLayoutPanel.RowStyles.Clear();

        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        browserTabControl = new()
        {
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        browserTabControl.SelectedIndexChanged += BrowserTabControl_SelectedIndexChanged;

        defaultTabPage = new("Add new tab")
        {
            Name = "add_new_tab"
        };

        tableLayoutPanel.Controls.Add(browserTabControl, 0, 0);

        browserPanel = new()
        {
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        tableLayoutPanel.Controls.Add(browserPanel, 0, 1);
        Controls.Add(tableLayoutPanel);

        // Load pre-existing historical views or spawn a clean homepage session
        LoadOrCreateTabs();
    }

    private void LoadOrCreateTabs()
    {
        if (settings.Tabs != null && settings.Tabs.Count > 0)
        {
            foreach (var kvp in settings.Tabs)
            {
                RestoreTabFromState(kvp.Value);
            }
        }
        else
        {
            SpawnNewTab("Current tab");
        }

        // Always push placeholder row tab layout button down to final index location
        browserTabControl.TabPages.Add(defaultTabPage);
    }

    private void RestoreTabFromState(TabState tabState)
    {
        // Rebuild active visual tabs out of your saved domain models
        TabPage tabPage = new(tabState.Title ?? tabState.Name ?? "Gemini Chat");

        BrowserTabComponent browserTabComponent = new(tabState)
        {
            Dock = DockStyle.Fill
        };

        browserTabComponent.TitleChanged += BrowserTabComponent_TitleChanged;
        browserTabComponent.TabStateUpdated += BrowserTabComponent_TabStateUpdated;

        tabPage.Tag = browserTabComponent;
        browserTabControl.TabPages.Add(tabPage);
    }

    private void SpawnNewTab(string title)
    {
        TabState tabState = new()
        {
            Key = Guid.NewGuid().ToString("N"),
            Name = title,
            Title = title
        };

        settings.Tabs.Set(tabState.Key, tabState);

        TabPage tabPage = new(title);
        BrowserTabComponent browserTabComponent = new(tabState)
        {
            Dock = DockStyle.Fill
        };

        browserTabComponent.TitleChanged += BrowserTabComponent_TitleChanged;
        browserTabComponent.TabStateUpdated += BrowserTabComponent_TabStateUpdated;

        tabPage.Tag = browserTabComponent;

        int insertIndex = Math.Max(0, browserTabControl.TabPages.Count - 1);
        browserTabControl.TabPages.Insert(insertIndex, tabPage);
    }

    private void BrowserTabComponent_TitleChanged(object? sender, string e)
    {
        if (sender is BrowserTabComponent component)
        {
            // Trace back across pages structurally to match the precise sender reference
            foreach (TabPage page in browserTabControl.TabPages)
            {
                if (page.Tag == component)
                {
                    page.Text = e;
                    break;
                }
            }
        }
    }

    private async void BrowserTabComponent_TabStateUpdated(object? sender, TabState e)
    {
        // Commit changes automatically whenever a component alerts us its URL/Title altered
        settings.Tabs.Set(e.Key, e);
        await stateManager.SaveAsync();
    }

    private void BrowserTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var currentIndex = browserTabControl.SelectedIndex;
        if (currentIndex == -1) return;

        if (browserTabControl.TabPages[currentIndex] == defaultTabPage)
        {
            SpawnNewTab("Newly spawned tab");
            browserTabControl.SelectedIndex = browserTabControl.TabPages.Count - 2;
            return;
        }

        browserPanel.Controls.Clear();
        if (browserTabControl.TabPages[currentIndex].Tag is BrowserTabComponent component)
        {
            browserPanel.Controls.Add(component);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var loadTask = Task.Run(async () => await stateManager.SaveAsync());
        loadTask.Wait();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (browserTabControl != null)
            {
                browserTabControl.SelectedIndexChanged -= BrowserTabControl_SelectedIndexChanged;

                foreach (TabPage page in browserTabControl.TabPages)
                {
                    if (page.Tag is BrowserTabComponent component)
                    {
                        component.TitleChanged -= BrowserTabComponent_TitleChanged;
                        component.TabStateUpdated -= BrowserTabComponent_TabStateUpdated;
                        component.Dispose();
                    }
                }
            }
        }

        base.Dispose(disposing);
    }
}