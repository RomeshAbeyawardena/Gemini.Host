using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace Gemini.Host.App;

internal partial class MainForm : Form
{
    
    private const string SubFolder = "gemini.host";
    private const string Filename = "gemini.host.settings.json";
    private List<Control> browserTabControls;
    private readonly FileJsonStateManager stateManager = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        SubFolder,
        Filename));

    private const string Title = "Gemini App";

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

        // 3. Make the column stretch to fill the width
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // 4. Configure the rows: Fixed height first, remainder second
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // e.g., 50 pixels high
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Take

        browserTabControl = new()
        {
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        browserTabControl.SelectedIndexChanged += BrowserTabControl_TabIndexChanged;
        browserTabControl.ControlAdded += BrowserTabControl_ControlAdded;

        defaultTabPage = new("Add new tab")
        {
            Name = "add_new_tab"
        };

        browserTabControl.TabPages.Add("Current tab");

        SpawnNewTab();

        browserTabControl.TabPages.Add(defaultTabPage);
        tableLayoutPanel.Controls.Add(browserTabControl, 0, 0);

        browserPanel = new()
        {
            BackColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill
        };

        tableLayoutPanel.Controls.Add(browserPanel, 0, 1);
        Controls.Add(tableLayoutPanel);
    }

    public MainForm()
    {
        InitializeComponent();
        
        browserTabControls = [];

        InitialiseControls();
        
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            SubFolder);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        Task.Run(async () => await stateManager.LoadAsync())
            .ConfigureAwait(true).GetAwaiter().GetResult();

        this.Text = Title;
    }

    private void SpawnNewTab()
    {
        BrowserTabComponent browserTabComponent = new(stateManager)
        {
            Dock = DockStyle.Fill
        };

        browserTabControls.Add(browserTabComponent);
    }

    private void BrowserTabControl_TabIndexChanged(object? sender, EventArgs e)
    {
        var currentIndex = browserTabControl.SelectedIndex;
        if (currentIndex == browserTabControl.TabPages.Count - 1)
        {
            var tabPage = new TabPage("Newly spawned tab");

            SpawnNewTab();
            browserTabControl.TabPages.Add(tabPage);

            return;
        }

        browserPanel.Controls.Clear();
        browserPanel.Controls.Add(browserTabControls[currentIndex]);
    }

    private bool bypass = false;
    private void BrowserTabControl_ControlAdded(object? sender, ControlEventArgs e)
    {
        if (!bypass && browserTabControl.TabPages.Count > 2)
        {
            bypass = true;
            browserTabControl.TabPages.Remove(defaultTabPage);
            browserTabControl.TabPages.Add(defaultTabPage);
            bypass = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            browserTabControl.ControlAdded -= BrowserTabControl_ControlAdded;
            browserTabControl.TabIndexChanged -= BrowserTabControl_TabIndexChanged;
        }

        foreach(var control in browserTabControls)
        {
            control?.Dispose();
        }

        base.Dispose(disposing);
    }
}
