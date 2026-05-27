using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace StayAwake;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private readonly StayAwakeWorker _worker;
    private readonly Dispatcher _dispatcher;

    private string _idleSecondsText = "";
    private string _movementPixelsText = "";
    private string _sessionDurationHoursText = "";

    public MainViewModel(AppSettings settings, StayAwakeWorker worker)
    {
        _settings = settings;
        _worker = worker;
        _dispatcher = System.Windows.Application.Current.Dispatcher;

        _idleSecondsText = _settings.IdleSeconds.ToString(CultureInfo.InvariantCulture);
        _movementPixelsText = _settings.MovementPixels.ToString(CultureInfo.InvariantCulture);
        _sessionDurationHoursText = _settings.SessionDurationHours.ToString(CultureInfo.InvariantCulture);

        _worker.StatusChanged += OnWorkerStatusChanged;
        _worker.SessionCompleted += OnSessionCompleted;

        ResetSettingsCommand = new RelayCommand(ResetToDefaults, () => CanEditSettings);

        if (_settings.Enabled)
        {
            var duration = _settings.SessionDurationHours > 0
                ? TimeSpan.FromHours(_settings.SessionDurationHours)
                : (TimeSpan?)null;
            _worker.StartSession(duration);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ResetSettingsCommand { get; }

    public string AppVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v is null ? "1.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public string FooterVersionText => $"StayAwake v{AppVersion}";

    public bool Enabled
    {
        get => _settings.Enabled;
        set
        {
            if (_settings.Enabled == value)
                return;

            if (value)
                StartSession(SessionDurationFromSettings());
            else
                StopSession();
        }
    }

    public void StartSession(TimeSpan? duration)
    {
        _worker.StartSession(duration);

        if (duration is { TotalHours: >= 1 } d)
            _settings.SessionDurationHours = (int)d.TotalHours;
        else if (duration is null)
            _settings.SessionDurationHours = 0;

        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(SessionDurationHours));
        SyncSessionDurationHoursText(_settings.SessionDurationHours);
        Save();
        RefreshAll();
    }

    public void StopSession()
    {
        _worker.StopSession();
        OnPropertyChanged(nameof(Enabled));
        Save();
        RefreshAll();
    }

    private TimeSpan? SessionDurationFromSettings() =>
        _settings.SessionDurationHours > 0
            ? TimeSpan.FromHours(_settings.SessionDurationHours)
            : null;

    public string IdleSecondsText
    {
        get => _idleSecondsText;
        set
        {
            if (_idleSecondsText == value)
                return;

            _idleSecondsText = value;
            OnPropertyChanged();

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return;

            ApplyIdleSeconds(parsed);
        }
    }

    private void ApplyIdleSeconds(int value)
    {
        var clamped = Math.Clamp(value, 10, 3600);
        if (_settings.IdleSeconds == clamped)
        {
            SyncIdleSecondsText(clamped);
            return;
        }

        _settings.IdleSeconds = clamped;
        SyncIdleSecondsText(clamped);
        OnPropertyChanged(nameof(IdleSeconds));
        Save();
    }

    private void SyncIdleSecondsText(int value) =>
        SetField(ref _idleSecondsText, value.ToString(CultureInfo.InvariantCulture), nameof(IdleSecondsText));

    public int IdleSeconds => _settings.IdleSeconds;

    public string MovementPixelsText
    {
        get => _movementPixelsText;
        set
        {
            if (_movementPixelsText == value)
                return;

            _movementPixelsText = value;
            OnPropertyChanged();

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return;

            ApplyMovementPixels(parsed);
        }
    }

    private void ApplyMovementPixels(int value)
    {
        var clamped = Math.Clamp(value, 1, 10);
        if (_settings.MovementPixels == clamped)
        {
            SyncMovementPixelsText(clamped);
            return;
        }

        _settings.MovementPixels = clamped;
        SyncMovementPixelsText(clamped);
        OnPropertyChanged(nameof(MovementPixels));
        Save();
    }

    private void SyncMovementPixelsText(int value) =>
        SetField(ref _movementPixelsText, value.ToString(CultureInfo.InvariantCulture), nameof(MovementPixelsText));

    public int MovementPixels => _settings.MovementPixels;

    public string SessionDurationHoursText
    {
        get => _sessionDurationHoursText;
        set
        {
            if (_sessionDurationHoursText == value)
                return;

            _sessionDurationHoursText = value;
            OnPropertyChanged();

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return;

            ApplySessionDurationHours(parsed);
        }
    }

    private void ApplySessionDurationHours(int value)
    {
        var clamped = Math.Clamp(value, 0, 99);
        if (_settings.SessionDurationHours == clamped)
        {
            SyncSessionDurationHoursText(clamped);
            return;
        }

        _settings.SessionDurationHours = clamped;
        SyncSessionDurationHoursText(clamped);
        OnPropertyChanged(nameof(SessionDurationHours));
        OnPropertyChanged(nameof(RemainingTimeValue));
        OnPropertyChanged(nameof(RemainingDisplayText));
        Save();
    }

    private void SyncSessionDurationHoursText(int value) =>
        SetField(ref _sessionDurationHoursText, value.ToString(CultureInfo.InvariantCulture), nameof(SessionDurationHoursText));

    public int SessionDurationHours => _settings.SessionDurationHours;

    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set
        {
            if (_settings.MinimizeToTray == value)
                return;

            _settings.MinimizeToTray = value;
            OnPropertyChanged();
            Save();
        }
    }

    public MovementMode MovementMode
    {
        get => _settings.MovementMode;
        set
        {
            if (_settings.MovementMode == value)
                return;

            _settings.MovementMode = value;
            OnPropertyChanged();
            Save();
        }
    }

    public MovementMode[] MovementModes { get; } = Enum.GetValues<MovementMode>();

    public AppStatus Status => _worker.Status;

    /// <summary>False while running (Active); user must disable to change settings.</summary>
    public bool CanEditSettings => Status != AppStatus.Active;

    public string StatusPillText => Status switch
    {
        AppStatus.Active => "Active",
        AppStatus.Disabled => "Disabled",
        AppStatus.SessionCompleted => "Session completed",
        _ => "Unknown"
    };

    public string RemainingTimeValue
    {
        get
        {
            var remaining = _worker.RemainingTime;
            if (remaining is null || remaining.Value <= TimeSpan.Zero)
                return string.Empty;

            return SessionDisplay.FormatRemaining(remaining.Value);
        }
    }

    public string RemainingDisplayText
    {
        get
        {
            if (Status == AppStatus.Disabled)
                return "—";

            if (Status == AppStatus.SessionCompleted)
                return "—";

            if (_worker.IsUnlimitedSession)
                return "Unlimited";

            var value = RemainingTimeValue;
            return string.IsNullOrEmpty(value) ? "—" : value;
        }
    }

    public string LastMovementValue =>
        _worker.LastMoved is null
            ? "Never"
            : _worker.LastMoved.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(Status));
        NotifyStatusProperties();
        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(MovementMode));
        OnPropertyChanged(nameof(MinimizeToTray));
    }

    public void RevertInvalidNumericFields()
    {
        SyncIdleSecondsText(_settings.IdleSeconds);
        SyncMovementPixelsText(_settings.MovementPixels);
        SyncSessionDurationHoursText(_settings.SessionDurationHours);
    }

    private void ResetToDefaults()
    {
        var defaults = new AppSettings();
        _settings.Enabled = defaults.Enabled;
        _settings.MovementPixels = defaults.MovementPixels;
        _settings.IdleSeconds = defaults.IdleSeconds;
        _settings.MinimizeToTray = defaults.MinimizeToTray;
        _settings.MovementMode = defaults.MovementMode;
        _settings.SessionDurationHours = defaults.SessionDurationHours;

        if (_settings.Enabled)
            _worker.StartSession(SessionDurationFromSettings());
        else
            _worker.StopSession();

        SettingsStore.Save(_settings);
        RevertInvalidNumericFields();
        RefreshAll();
    }

    private void OnWorkerStatusChanged() => _dispatcher.Invoke(NotifyStatusProperties);

    private void OnSessionCompleted() => _dispatcher.Invoke(() =>
    {
        OnPropertyChanged(nameof(Enabled));
        NotifyStatusProperties();
    });

    private void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusPillText));
        OnPropertyChanged(nameof(RemainingTimeValue));
        OnPropertyChanged(nameof(RemainingDisplayText));
        OnPropertyChanged(nameof(LastMovementValue));
        OnPropertyChanged(nameof(CanEditSettings));
        CommandManager.InvalidateRequerySuggested();
    }

    private void Save() => SettingsStore.Save(_settings);

    private void SetField(ref string field, string value, string propertyName)
    {
        if (field == value)
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
