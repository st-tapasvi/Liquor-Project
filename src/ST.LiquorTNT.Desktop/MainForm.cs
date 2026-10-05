using System.Windows.Forms;

namespace ST.LiquorTNT.Desktop;

/// <summary>
/// Shell only. The line application (printing, scanning, aggregation, hardware) is built here later.
/// It will call the API over HTTP using the types from ST.LiquorTNT.Contracts.
/// </summary>
public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "ST LiquorTNT - Line Application";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label
        {
            Text = "Line application shell. Nothing wired yet.",
            AutoSize = true,
            Left = 24,
            Top = 24,
        });
    }
}
