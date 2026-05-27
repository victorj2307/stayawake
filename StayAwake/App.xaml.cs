using System.Threading;
using System.Windows;

namespace StayAwake;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "StayAwake.SingleInstance";
    private static Mutex? _singleInstanceMutex;

    private AppSettings? _settings;
    private StayAwakeWorker? _worker;
    private TrayIconManager? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryAcquireSingleInstance())
        {
            System.Windows.MessageBox.Show(
                "StayAwake is already running.",
                "StayAwake",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settings = SettingsStore.Load();
        _worker = new StayAwakeWorker(_settings);
        var viewModel = new MainViewModel(_settings, _worker);
        var window = new MainWindow { DataContext = viewModel };

        _tray = new TrayIconManager(window, _worker, viewModel);

        _worker.SessionCompleted += () =>
        {
            SettingsStore.Save(_settings);
            _tray.ShowSessionCompletedBalloon();
        };

        _worker.Start();

        if (string.Equals(Environment.GetEnvironmentVariable("STAYAWAKE_SCREENSHOT"), "session-completed", StringComparison.OrdinalIgnoreCase))
        {
            _worker.StartSession(TimeSpan.FromSeconds(1));
            Thread.Sleep(1500);
        }

        _tray.Show();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_settings is not null)
            SettingsStore.Save(_settings);

        _worker?.Dispose();
        _tray?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }

    private static bool TryAcquireSingleInstance()
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        return createdNew;
    }
}
