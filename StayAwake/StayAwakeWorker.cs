using System.Diagnostics;

namespace StayAwake;

public sealed class StayAwakeWorker : IDisposable
{
    private readonly AppSettings _settings;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private AppStatus _lastNotifiedStatus = AppStatus.Disabled;
    private DateTime? _lastNotifiedLastMoved;
    private int? _lastNotifiedRemainingSeconds;

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
        ResetNotificationSnapshot();
        NotifyStatusChanged();
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

    /// <summary>Starts a session; updates <see cref="AppSettings.Enabled"/> for persistence.</summary>
    public void StartSession(TimeSpan? duration)
    {
        SessionStartedAt = DateTime.UtcNow;
        SessionEndsAt = duration is null ? null : DateTime.UtcNow.Add(duration.Value);
        _settings.Enabled = true;
        LastMoved = null;
        Status = AppStatus.Active;
        SyncKeepAwake();
        NotifyStatusChanged();
    }

    /// <summary>Stops a session; clears runtime timestamps and <see cref="AppSettings.Enabled"/>.</summary>
    public void StopSession()
    {
        _settings.Enabled = false;
        SessionStartedAt = null;
        SessionEndsAt = null;
        LastMoved = null;
        Status = AppStatus.Disabled;
        SyncKeepAwake();
        NotifyStatusChanged();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (_timer is not null)
        {
            try
            {
                if (!await _timer.WaitForNextTickAsync(ct))
                    break;

                if (IsSessionExpired())
                {
                    CompleteSession();
                    continue;
                }

                UpdateStatus();
                NotifyStatusChangedIfNeeded();

                if (!_settings.Enabled || Status != AppStatus.Active)
                    continue;

                var idleSeconds = NativeMethods.GetIdleSeconds();
                if (idleSeconds < _settings.IdleSeconds)
                    continue;

                var secondsSinceLastJiggle = LastMoved is null
                    ? double.MaxValue
                    : (DateTime.UtcNow - LastMoved.Value).TotalSeconds;

                if (secondsSinceLastJiggle < _settings.IdleSeconds)
                    continue;

                NativeMethods.JiggleMouse(_settings.MovementPixels, _settings.MovementMode);
                LastMoved = DateTime.UtcNow;
                NotifyStatusChanged();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StayAwake worker tick failed: {ex}");
            }
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
        NotifyStatusChanged();
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

    private static int? GetRemainingSecondsKey(TimeSpan? remaining) =>
        remaining is null ? null : Math.Max(0, (int)remaining.Value.TotalSeconds);

    private int? CurrentRemainingSecondsKey() => GetRemainingSecondsKey(RemainingTime);

    private void ResetNotificationSnapshot()
    {
        _lastNotifiedStatus = Status;
        _lastNotifiedLastMoved = LastMoved;
        _lastNotifiedRemainingSeconds = CurrentRemainingSecondsKey();
    }

    private void NotifyStatusChanged()
    {
        ResetNotificationSnapshot();
        StatusChanged?.Invoke();
    }

    private void NotifyStatusChangedIfNeeded()
    {
        var remainingKey = CurrentRemainingSecondsKey();
        if (Status == _lastNotifiedStatus
            && LastMoved == _lastNotifiedLastMoved
            && remainingKey == _lastNotifiedRemainingSeconds)
        {
            return;
        }

        NotifyStatusChanged();
    }

    public void Dispose() => Stop();
}
