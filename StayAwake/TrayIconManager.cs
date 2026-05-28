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
    private readonly Icon _trayIcon;

    public TrayIconManager(Window window, StayAwakeWorker worker, MainViewModel viewModel)
    {
        _window = window;
        _worker = worker;
        _viewModel = viewModel;
        _dispatcher = window.Dispatcher;

        _trayIcon = LoadTrayIcon();

        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "StayAwake",
            Visible = true,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
        _worker.StatusChanged += OnWorkerStatusChanged;
        UpdateTooltip();
    }

    private void OnWorkerStatusChanged()
    {
        if (_dispatcher.CheckAccess())
            UpdateTooltip();
        else
            _dispatcher.BeginInvoke(UpdateTooltip);
    }

    public void Show() => _notifyIcon.Visible = true;

    public void ShowSessionCompletedBalloon()
    {
        if (_dispatcher.CheckAccess())
            ShowBalloonOnUiThread();
        else
            _dispatcher.BeginInvoke(ShowBalloonOnUiThread);
    }

    private void ShowBalloonOnUiThread() =>
        _notifyIcon.ShowBalloonTip(3000, "StayAwake", "Session completed", ToolTipIcon.Info);

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
            _ => "○ Disabled"
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
            _ => "StayAwake — Disabled"
        };
    }

    private void ShowWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private static Icon LoadTrayIcon()
    {
        var stream = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute))?.Stream;
        if (stream == null)
            return SystemIcons.Application;

        using (stream)
        using (var temp = new Icon(stream))
            return (Icon)temp.Clone();
    }

    public void Dispose()
    {
        _worker.StatusChanged -= OnWorkerStatusChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        if (!ReferenceEquals(_trayIcon, SystemIcons.Application))
            _trayIcon.Dispose();
    }
}
