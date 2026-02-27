using Microsoft.Win32;

namespace VolumeCapper;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _onSave;

    private TrackBar _slider = null!;
    private Label _percentLabel = null!;
    private CheckBox _enabledCheck = null!;
    private CheckBox _startupCheck = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public SettingsForm(AppSettings settings, Action<AppSettings> onSave)
    {
        _settings = settings;
        _onSave = onSave;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Volume Capper – Settings";
        Size = new Size(360, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        // ── Max Volume ─────────────────────────────────────────────────────
        var capLabel = new Label
        {
            Text = "Maximum Volume Cap",
            Location = new Point(16, 16),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _slider = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = (int)(_settings.MaxVolume * 100),
            TickFrequency = 10,
            LargeChange = 10,
            Location = new Point(12, 36),
            Width = 230,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        _slider.ValueChanged += (_, _) => _percentLabel.Text = $"{_slider.Value}%";

        _percentLabel = new Label
        {
            Text = $"{_slider.Value}%",
            Location = new Point(250, 44),
            AutoSize = true,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.SkyBlue
        };

        // ── Checkboxes ─────────────────────────────────────────────────────
        _enabledCheck = new CheckBox
        {
            Text = "Enable volume cap",
            Checked = _settings.CapEnabled,
            Location = new Point(16, 115),
            Width = 300,
            AutoSize = true
        };

        _startupCheck = new CheckBox
        {
            Text = "Run at Windows startup",
            Checked = _settings.RunAtStartup,
            Location = new Point(16, 150),
            Width = 300,
            AutoSize = true
        };

        // ── Buttons ────────────────────────────────────────────────────────
        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(160, 205),
            Size = new Size(85, 32),
            BackColor = Color.SteelBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        _saveButton.Click += OnSave;

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(250, 205),
            Size = new Size(85, 32),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[] {
            capLabel, _slider, _percentLabel,
            _enabledCheck, _startupCheck,
            _saveButton, _cancelButton
        });
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.MaxVolume = _slider.Value / 100f;
        _settings.CapEnabled = _enabledCheck.Checked;
        _settings.RunAtStartup = _startupCheck.Checked;
        _settings.Save();
        SetStartup(_settings.RunAtStartup);
        _onSave(_settings);
    }

    private static void SetStartup(bool enable)
    {
        const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string appName = "VolumeCapper";

        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key == null) return;

        if (enable)
            key.SetValue(appName, Application.ExecutablePath);
        else
            key.DeleteValue(appName, throwOnMissingValue: false);
    }
}