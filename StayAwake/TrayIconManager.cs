using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace StayAwake;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _window;
    private readonly StayAwakeWorker _worker;
    private readonly MainViewModel _viewModel;
    private readonly ContextMenuStrip _menu;
    private readonly Dispatcher _dispatcher;
    private readonly Icon _iconDisabled;
    private readonly Icon _iconActive;
    private readonly Icon _iconCompleted;
    private readonly Icon? _fallbackIcon;

    private static readonly TimeSpan RunningInTrayBalloonCooldown = TimeSpan.FromSeconds(15);
    private DateTime _lastRunningInTrayBalloonUtc = DateTime.MinValue;

    private const string RunningInTrayBalloonBody =
        "Still running in the system tray. Click here or double-click the tray icon to open settings.";

    public TrayIconManager(Window window, StayAwakeWorker worker, MainViewModel viewModel)
    {
        _window = window;
        _worker = worker;
        _viewModel = viewModel;
        _dispatcher = window.Dispatcher;

        _fallbackIcon = LoadTrayIcon("Assets/app.ico");
        _iconDisabled = LoadTrayIcon("Assets/app-tray-disabled.ico") ?? _fallbackIcon!;
        _iconActive = LoadTrayIcon("Assets/app-tray-active.ico") ?? _fallbackIcon!;
        _iconCompleted = LoadTrayIcon("Assets/app-tray-completed.ico") ?? _fallbackIcon!;

        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new NotifyIcon
        {
            Icon = IconForStatus(_worker.Status),
            Text = "StayAwake",
            Visible = true,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += OnNotifyIconOpenWindow;
        _notifyIcon.BalloonTipClicked += OnNotifyIconOpenWindow;
        _worker.StatusChanged += OnWorkerStatusChanged;
        UpdateTrayAppearance();
    }

    private void OnWorkerStatusChanged()
    {
        if (_dispatcher.CheckAccess())
            UpdateTrayAppearance();
        else
            _dispatcher.BeginInvoke(UpdateTrayAppearance);
    }

    public void Show() => _notifyIcon.Visible = true;

    public void ShowSessionCompletedBalloon()
    {
        if (_dispatcher.CheckAccess())
            ShowSessionCompletedBalloonOnUiThread();
        else
            _dispatcher.BeginInvoke(ShowSessionCompletedBalloonOnUiThread);
    }

    public void ShowRunningInTrayBalloon()
    {
        if (_dispatcher.CheckAccess())
            ShowRunningInTrayBalloonOnUiThread();
        else
            _dispatcher.BeginInvoke(ShowRunningInTrayBalloonOnUiThread);
    }

    private void ShowSessionCompletedBalloonOnUiThread() =>
        _notifyIcon.ShowBalloonTip(3000, "StayAwake", "Session completed", ToolTipIcon.Info);

    private void ShowRunningInTrayBalloonOnUiThread()
    {
        var now = DateTime.UtcNow;
        if (now - _lastRunningInTrayBalloonUtc < RunningInTrayBalloonCooldown)
            return;

        _lastRunningInTrayBalloonUtc = now;
        _notifyIcon.ShowBalloonTip(3000, "StayAwake", RunningInTrayBalloonBody, ToolTipIcon.Info);
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        _menu.Items.Add(CreateStatusItem());
        _menu.Items.Add("Open settings", null, (_, _) => ShowWindow());
        _menu.Items.Add(new ToolStripSeparator());

        var isActive = _worker.Status == AppStatus.Active;
        AddSessionItem("Start 30 minutes", TimeSpan.FromMinutes(30), enabled: !isActive);
        AddSessionItem("Start 1 hour", TimeSpan.FromHours(1), enabled: !isActive);
        AddSessionItem("Start 3 hours", TimeSpan.FromHours(3), enabled: !isActive);
        AddSessionItem("Start indefinitely", null, enabled: !isActive);

        _menu.Items.Add(new ToolStripSeparator());

        var stopItem = new ToolStripMenuItem("Stop session", null, (_, _) => _viewModel.StopSession())
        {
            Enabled = isActive
        };
        _menu.Items.Add(stopItem);

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());
    }

    private ToolStripMenuItem CreateStatusItem()
    {
        var label = _worker.Status switch
        {
            AppStatus.Active => "● Active",
            AppStatus.SessionCompleted => "● Session completed",
            _ => "○ Inactive"
        };

        return new ToolStripMenuItem(label) { Enabled = false };
    }

    private void AddSessionItem(string text, TimeSpan? duration, bool enabled)
    {
        var item = new ToolStripMenuItem(text, null, (_, _) => _viewModel.StartSession(duration))
        {
            Enabled = enabled
        };
        _menu.Items.Add(item);
    }

    private void UpdateTrayAppearance()
    {
        _notifyIcon.Icon = IconForStatus(_worker.Status);
        UpdateTooltip();
    }

    private Icon IconForStatus(AppStatus status) => status switch
    {
        AppStatus.Active => _iconActive,
        AppStatus.SessionCompleted => _iconCompleted,
        _ => _iconDisabled
    };

    private void UpdateTooltip()
    {
        var text = BuildTooltipText();
        if (text.Length > 63)
            text = text[..63];

        _notifyIcon.Text = text;
    }

    private string BuildTooltipText()
    {
        return _worker.Status switch
        {
            AppStatus.Active when _worker.IsUnlimitedSession => "StayAwake — Active (no limit)",
            AppStatus.Active when _worker.RemainingTime is { } remaining && remaining > TimeSpan.Zero =>
                $"StayAwake — Active ({SessionDisplay.FormatRemaining(remaining)})",
            AppStatus.Active => "StayAwake — Active",
            AppStatus.SessionCompleted => "StayAwake — Session completed",
            _ => "StayAwake — Inactive"
        };
    }

    private void OnNotifyIconOpenWindow(object? sender, EventArgs e) => ShowWindow();

    private void ShowWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private static Icon? LoadTrayIcon(string packPath)
    {
        var stream = System.Windows.Application.GetResourceStream(
            new Uri($"pack://application:,,,/{packPath}", UriKind.Absolute))?.Stream;
        if (stream == null)
            return null;

        using (stream)
        using (var temp = new Icon(stream))
            return (Icon)temp.Clone();
    }

    public void Dispose()
    {
        _worker.StatusChanged -= OnWorkerStatusChanged;
        _notifyIcon.DoubleClick -= OnNotifyIconOpenWindow;
        _notifyIcon.BalloonTipClicked -= OnNotifyIconOpenWindow;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();

        DisposeUniqueIcons();
    }

    private void DisposeUniqueIcons()
    {
        var seen = new HashSet<Icon>();
        foreach (var icon in new[] { _iconDisabled, _iconActive, _iconCompleted, _fallbackIcon })
        {
            if (icon is null || !seen.Add(icon))
                continue;

            icon.Dispose();
        }
    }
}
