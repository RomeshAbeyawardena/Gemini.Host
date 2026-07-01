using Microsoft.Web.WebView2.WinForms;

namespace Gemini.Host.App;

internal partial class MainForm
{
    private System.ComponentModel.Container components;
    private WebView2 browser;

    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(64, 64, 64);
        ClientSize = new Size(1216, 722);
        Name = "MainForm";
        ResumeLayout(false);
    }

}
