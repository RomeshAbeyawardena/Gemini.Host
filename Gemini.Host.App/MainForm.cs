using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gemini.Host.App;

internal partial class MainForm : Form
{
    private const string SubFolder = "gemini.host";
    private const string Filename = "gemini.host.settings.json";

    private readonly FileJsonStateManager stateManager = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        SubFolder,
        Filename));

    private const string Title = "Gemini App";

    public MainForm()
    {
        InitializeComponent();

        InitialiseControls();

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SubFolder);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        Task.Run(async () => await stateManager.LoadAsync())
            .ConfigureAwait(true).GetAwaiter().GetResult();

        this.Text = Title;
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

        // Make the column stretch to fill the width
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Configure the rows: Fixed height first, remainder second
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        browserTabControl = new()
        {
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        // Wire up the index change event
        browserTabControl.SelectedIndexChanged += BrowserTabControl_SelectedIndexChanged;

        defaultTabPage = new("Add new tab")
        {
            Name = "add_new_tab"
        };

        // Set up the structural layout hierarchy
        tableLayoutPanel.Controls.Add(browserTabControl, 0, 0);

        browserPanel = new()
        {
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        tableLayoutPanel.Controls.Add(browserPanel, 0, 1);
        Controls.Add(tableLayoutPanel);

        // Spawn your initial default tab, then attach the helper "plus" tab right after it
        SpawnNewTab("Current tab");
        browserTabControl.TabPages.Add(defaultTabPage);
    }

    private void SpawnNewTab(string title)
    {
        TabPage tabPage = new(title);

        // Create the individual tab components dynamically
        BrowserTabComponent browserTabComponent = new(stateManager)
        {
            Dock = DockStyle.Fill
        };

        browserTabComponent.TitleChanged += BrowserTabComponent_TitleChanged;

        // Store the component reference straight into the TabPage's metadata
        tabPage.Tag = browserTabComponent;

        // Safely wedge the new tab directly before our "Add new tab" target row
        int insertIndex = Math.Max(0, browserTabControl.TabPages.Count - 1);
        browserTabControl.TabPages.Insert(insertIndex, tabPage);
    }

    private void BrowserTabComponent_TitleChanged(object? sender, string e)
    {
        browserTabControl.TabPages[browserTabControl.SelectedIndex].Text = e;
    }

    private void BrowserTabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var currentIndex = browserTabControl.SelectedIndex;
        if (currentIndex == -1) return;

        // Check if the user selected the placeholder row button
        if (browserTabControl.TabPages[currentIndex] == defaultTabPage)
        {
            SpawnNewTab("Newly spawned tab");

            // Instantly transition the active view focus onto the freshly created tab
            browserTabControl.SelectedIndex = browserTabControl.TabPages.Count - 2;
            return;
        }

        // Clean out the active view container and mount the active tab content
        browserPanel.Controls.Clear();
        if (browserTabControl.TabPages[currentIndex].Tag is BrowserTabComponent component)
        {
            browserPanel.Controls.Add(component);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe using the identical event signature to prevent memory leaks
            if (browserTabControl != null)
            {
                browserTabControl.SelectedIndexChanged -= BrowserTabControl_SelectedIndexChanged;

                // Deep-clean your underlying controls directly via their lifecycle owners
                foreach (TabPage page in browserTabControl.TabPages)
                {
                    if (page.Tag is IDisposable disposableComponent)
                    {
                        disposableComponent.Dispose();
                    }
                }
            }
        }

        base.Dispose(disposing);
    }
}