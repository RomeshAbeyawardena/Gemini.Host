using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gemini.Host.App;

internal partial class BrowserTabComponent
{
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
        ResumeLayout(false);
    }
}
