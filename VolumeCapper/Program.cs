using System.Runtime.InteropServices;

namespace VolumeCapper;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Prevent multiple instances
        using var mutex = new System.Threading.Mutex(true, "VolumeCapper_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Volume Capper is already running.\nCheck the system tray.", "Already Running",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApp());
    }
}

/// <summary>
/// Invisible background form that owns the tray icon and the capper service.
/// </summary>
public class TrayApp : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly VolumeCapperService _service;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _capLevelItem;

    // Balloon tip cooldown so we don't spam the user
    private DateTime _lastBalloon = DateTime.MinValue;

    public TrayApp()
    {
        _settings = AppSettings.Load();
        _service = new VolumeCapperService(_settings);

        // Build context menu
        _toggleItem = new ToolStripMenuItem("Cap Enabled", null, OnToggleCap)
        {
            Checked = _settings.CapEnabled,
            CheckOnClick = true
        };

        _capLevelItem = new ToolStripMenuItem($"Cap: {(int)(_settings.MaxVolume * 100)}%")
        {
            Enabled = false
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Volume Capper") { Enabled = false, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_toggleItem);
        menu.Items.Add(_capLevelItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Settings\u2026", null, OnOpenSettings));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, OnExit));

        // Tray icon
        _trayIcon = new NotifyIcon
        {
            Text = "Volume Capper",
            Icon = CreateTrayIcon(_settings.CapEnabled),
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += OnOpenSettings;

        _service.VolumeCapped += OnVolumeCapped;
        _service.Start();
    }

    private void OnToggleCap(object? sender, EventArgs e)
    {
        _settings.CapEnabled = _toggleItem.Checked;
        _settings.Save();
        _service.UpdateSettings(_settings);
        _trayIcon.Icon = CreateTrayIcon(_settings.CapEnabled);
    }

    private void OnVolumeCapped(float cappedAt)
    {
        if ((DateTime.Now - _lastBalloon).TotalSeconds < 5) return;
        _lastBalloon = DateTime.Now;

        _trayIcon.ShowBalloonTip(2000, "Volume Capped",
            $"Volume was reduced to {(int)(cappedAt * 100)}%", ToolTipIcon.Info);
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings, updatedSettings =>
        {
            _service.UpdateSettings(updatedSettings);
            _toggleItem.Checked = updatedSettings.CapEnabled;
            _capLevelItem.Text = $"Cap: {(int)(updatedSettings.MaxVolume * 100)}%";
            _trayIcon.Icon = CreateTrayIcon(updatedSettings.CapEnabled);
        });
        form.ShowDialog();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _service.Dispose();
        Application.Exit();
    }

    /// <summary>Draw a speaker tray icon in code — no .ico file needed.</summary>
    private static Icon CreateTrayIcon(bool enabled)
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        var speakerColor = enabled ? Color.SkyBlue : Color.Gray;
        using var brush = new SolidBrush(speakerColor);

        // Speaker body
        g.FillRectangle(brush, 2, 5, 4, 6);
        // Speaker cone
        Point[] cone = { new(6, 5), new(10, 2), new(10, 14), new(6, 11) };
        g.FillPolygon(brush, cone);

        if (enabled)
        {
            using var pen = new Pen(speakerColor, 1.5f);
            g.DrawArc(pen, 10, 4, 4, 8, -60, 120);
        }
        else
        {
            // Red X when disabled
            using var redPen = new Pen(Color.Tomato, 1.5f);
            g.DrawLine(redPen, 11, 5, 14, 11);
            g.DrawLine(redPen, 14, 5, 11, 11);
        }

        // Clone to avoid GDI handle leak from GetHicon()
        IntPtr hicon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hicon).Clone();
        DestroyIcon(hicon);
        return icon;
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
