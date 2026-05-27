namespace StayAwake;

public sealed class StayAwakeWorker : IDisposable
{
    private readonly AppSettings _settings;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public StayAwakeWorker(AppSettings settings)
    {
        _settings = settings;
    }

    public AppStatus Status { get; private set; } = AppStatus.Disabled;
    public DateTime? SessionStartedAt { get; private set; }
    public DateTime? SessionEndsAt { get; private set; }
    public DateTime? LastMoved { get; private set; }

    public event Action? StatusChanged;
    public event Action? SessionCompleted;

    public bool IsUnlimitedSession =>
        _settings.Enabled && SessionStartedAt is not null && SessionEndsAt is null;

    public TimeSpan? RemainingTime
    {
        get
        {
            if (!_settings.Enabled || SessionStartedAt is null || SessionEndsAt is null)
                return null;

            var remaining = SessionEndsAt.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public void Start()
    {
        if (_timer is not null)
            return;

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        UpdateStatus();
        _loopTask = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        NativeMethods.SetKeepAwake(false);
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _cts?.Dispose();
        _cts = null;
    }

    public void StartSession(TimeSpan? duration)
    {
        SessionStartedAt = DateTime.UtcNow;
        SessionEndsAt = duration is null ? null : DateTime.UtcNow.Add(duration.Value);
        _settings.Enabled = true;
        LastMoved = null;
        Status = AppStatus.Active;
        SyncKeepAwake();
        StatusChanged?.Invoke();
    }

    public void StopSession()
    {
        _settings.Enabled = false;
        SessionStartedAt = null;
        SessionEndsAt = null;
        LastMoved = null;
        Status = AppStatus.Disabled;
        SyncKeepAwake();
        StatusChanged?.Invoke();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (_timer is not null && await _timer.WaitForNextTickAsync(ct))
        {
            if (IsSessionExpired())
            {
                CompleteSession();
                continue;
            }

            UpdateStatus();
            StatusChanged?.Invoke();

            if (!_settings.Enabled || Status != AppStatus.Active)
                continue;

            var idleSeconds = NativeMethods.GetIdleSeconds();
            if (idleSeconds < _settings.IdleSeconds)
                continue;

            var secondsSinceLastJiggle = LastMoved is null
                ? double.MaxValue
                : (DateTime.Now - LastMoved.Value).TotalSeconds;

            if (secondsSinceLastJiggle < _settings.IdleSeconds)
                continue;

            NativeMethods.JiggleMouse(_settings.MovementPixels, _settings.MovementMode);
            LastMoved = DateTime.Now;
            StatusChanged?.Invoke();
        }
    }

    private bool IsSessionExpired() =>
        _settings.Enabled
        && SessionEndsAt is not null
        && DateTime.UtcNow >= SessionEndsAt.Value;

    private void CompleteSession()
    {
        _settings.Enabled = false;
        SessionStartedAt = null;
        SessionEndsAt = null;
        LastMoved = null;
        Status = AppStatus.SessionCompleted;
        SyncKeepAwake();
        SessionCompleted?.Invoke();
    }

    private void UpdateStatus()
    {
        if (Status == AppStatus.SessionCompleted)
        {
            SyncKeepAwake();
            return;
        }

        if (!_settings.Enabled)
        {
            Status = AppStatus.Disabled;
            SyncKeepAwake();
            return;
        }

        Status = AppStatus.Active;
        SyncKeepAwake();
    }

    private void SyncKeepAwake() => NativeMethods.SetKeepAwake(Status == AppStatus.Active);

    public void Dispose() => Stop();
}
