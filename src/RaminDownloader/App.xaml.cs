using System.Windows;
using System.Windows.Threading;

namespace RaminDownloader;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

        try
        {
            base.OnStartup(e);
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            ShowStartupError(exception);
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowStartupError(e.Exception);
        e.Handled = true;
        Shutdown(-1);
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ShowStartupError(exception);
        }
    }

    private static void ShowStartupError(Exception exception)
    {
        try
        {
            MessageBox.Show(
                $"RaminDownloader could not start.\n\n{exception.Message}\n\nDetails:\n{exception}",
                "RaminDownloader startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // There is no further UI fallback if Windows cannot create a message box.
        }
    }
}

