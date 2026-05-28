using System.ComponentModel;
using System.Windows;

namespace StayAwake;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.MinimizeToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Minimized
            && DataContext is MainViewModel vm
            && vm.MinimizeToTray)
        {
            HideToTray();
        }
    }

    private void HideToTray()
    {
        Hide();

        if (DataContext is MainViewModel vm
            && vm.MinimizeToTray
            && System.Windows.Application.Current is App app)
        {
            app.Tray.ShowRunningInTrayBalloon();
        }
    }

    private void NumericField_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        Dispatcher.BeginInvoke(() => vm.RevertInvalidNumericFields());
    }
}
