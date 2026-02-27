using NAudio.CoreAudioApi;

namespace VolumeCapper;

/// <summary>
/// Monitors the default audio endpoint and enforces a maximum volume level.
/// Uses a lightweight timer to re-attach when the default device changes
/// (avoids NAudio's internal IMMNotificationClient interface).
/// </summary>
public class VolumeCapperService : IDisposable
{
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _device;
    private AppSettings _settings;
    private System.Threading.Timer? _deviceWatchTimer;
    private string? _lastDeviceId = null;

    // Guard flag to prevent re-entrancy (setting vol triggers another notification)
    private bool _isSetting = false;

    public event Action<float>? VolumeCapped;   // Fired when a cap was enforced
    public event Action<float>? VolumeChanged;  // Fired on any volume change

    public VolumeCapperService(AppSettings settings)
    {
        _settings = settings;
    }

    public void Start()
    {
        _enumerator = new MMDeviceEnumerator();
        AttachToDefaultDevice();

        // Poll every 2 seconds to re-attach if the default audio device changes
        // (e.g. user plugs in / unplugs headphones)
        _deviceWatchTimer = new System.Threading.Timer(_ => RefreshDeviceIfChanged(),
            null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    private void AttachToDefaultDevice()
    {
        try
        {
            if (_device != null)
            {
                _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
                _device.Dispose();
                _device = null;
            }

            _device = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _lastDeviceId = _device.ID;
            _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }
        catch { /* No audio device available */ }
    }

    private void RefreshDeviceIfChanged()
    {
        try
        {
            using var temp = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (temp.ID != _lastDeviceId)
                AttachToDefaultDevice();
        }
        catch { /* Device may be temporarily unavailable */ }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        if (_isSetting) return;

        float current = data.MasterVolume;
        VolumeChanged?.Invoke(current);

        if (_settings.CapEnabled && current > _settings.MaxVolume)
        {
            _isSetting = true;
            try
            {
                _device!.AudioEndpointVolume.MasterVolumeLevelScalar = _settings.MaxVolume;
                VolumeCapped?.Invoke(_settings.MaxVolume);
            }
            finally
            {
                _isSetting = false;
            }
        }
    }

    /// <summary>Update settings without restarting the service.</summary>
    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;

        // Immediately enforce if current volume exceeds new cap
        if (_device != null && _settings.CapEnabled)
        {
            float current = _device.AudioEndpointVolume.MasterVolumeLevelScalar;
            if (current > _settings.MaxVolume)
            {
                _isSetting = true;
                try { _device.AudioEndpointVolume.MasterVolumeLevelScalar = _settings.MaxVolume; }
                finally { _isSetting = false; }
            }
        }
    }

    public void Dispose()
    {
        _deviceWatchTimer?.Dispose();
        if (_device != null)
        {
            _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
            _device.Dispose();
        }
        _enumerator?.Dispose();
    }
}
