using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using RaminDownloader.Models;
using RaminDownloader.Services;

namespace RaminDownloader;

public partial class MainWindow : Window
{
    private readonly YtDlpRunner _runner = new();
    private readonly ToolManager _toolManager;
    private readonly AppUpdateManager _appUpdateManager;
    private CancellationTokenSource? _downloadCancellation;
    private CancellationTokenSource? _toolUpdateCancellation;

    public MainWindow()
    {
        InitializeComponent();
        _toolManager = new ToolManager(AppContext.BaseDirectory);
        _appUpdateManager = new AppUpdateManager(AppContext.BaseDirectory);
        ApplyBranding();
        SourceInitialized += (_, _) =>
        {
            if (Application.Current?.MainWindow is null)
            {
                Application.Current!.MainWindow = this;
            }
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await EnsureToolsAsync();
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateAsync();
        await CheckForAppUpdateAsync();
    }

    private async Task EnsureToolsAsync()
    {
        if (ToolLocator.TryLocate(AppContext.BaseDirectory, out _)) return;
        await UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        SetBusy(true);
        LatestOutputTextBlock.Text = "Checking and installing required tools...";
        _toolUpdateCancellation = new CancellationTokenSource();
        try
        {
            var progress = new Progress<string>(message => LatestOutputTextBlock.Text = message);
            await _toolManager.UpdateAsync(progress, _toolUpdateCancellation.Token);
            StatusTextBlock.Text = "Tools are up to date.";
            LatestOutputTextBlock.Text = "yt-dlp, FFmpeg, FFprobe, and Deno are ready.";
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = "Tool update failed.";
            LatestOutputTextBlock.Text = exception.Message;
            MessageBox.Show(this, exception.ToString(), "Tool update failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _toolUpdateCancellation?.Dispose();
            _toolUpdateCancellation = null;
            SetBusy(false);
        }
    }

    private async Task CheckForAppUpdateAsync()
    {
        try
        {
            var update = await _appUpdateManager.CheckAsync();
            if (update is null) return;
            var answer = MessageBox.Show(this, $"RaminDownloader {update.LatestVersion} is available. Update now?", "Application update", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
            {
                await _appUpdateManager.DownloadAndScheduleAsync(update, "RaminDownloader.exe", new Progress<string>(message => LatestOutputTextBlock.Text = message));
                Application.Current.Shutdown();
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Application update check failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyBranding()
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ramindownloader-logo.jpg");
        if (File.Exists(logoPath))
        {
            LogoImage.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "RaminDownloader.ico");
        if (File.Exists(iconPath))
        {
            Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }
    }

    private void PasteButton_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            UrlTextBox.Text = Clipboard.GetText();
            UrlTextBox.Focus();
            UrlTextBox.CaretIndex = UrlTextBox.Text.Length;
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        var type = AudioRadioButton.IsChecked == true ? DownloadType.AudioMp3 : DownloadType.Video;
        var quality = MediumQualityRadioButton.IsChecked == true
            ? DownloadQuality.Medium
            : LowestQualityRadioButton.IsChecked == true
                ? DownloadQuality.Lowest
                : DownloadQuality.Highest;

        if (!DownloadOptions.TryCreate(UrlTextBox.Text, type, quality, useFirefoxCookies: true, out var options) || options is null)
        {
            StatusTextBlock.Text = "Please enter a complete http:// or https:// URL.";
            return;
        }

        SetBusy(true);
        DownloadProgressBar.Value = 0;
        LatestOutputTextBlock.Text = "Starting yt-dlp...";
        _downloadCancellation = new CancellationTokenSource();

        try
        {
            var progress = new Progress<DownloadProgress>(update =>
            {
                LatestOutputTextBlock.Text = update.Message;
                if (update.Percent is { } percent)
                {
                    DownloadProgressBar.Value = percent;
                    StatusTextBlock.Text = $"{percent:0.0}%";
                }
            });

            var downloadDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "RaminDownloader");
            var exitCode = await _runner.RunAsync(
                options,
                AppContext.BaseDirectory,
                downloadDirectory,
                progress,
                _downloadCancellation.Token);

            if (exitCode == 0)
            {
                DownloadProgressBar.Value = 100;
                StatusTextBlock.Text = "Download complete.";
                MessageBox.Show(
                    this,
                    $"Your download is complete.\n\nSaved to:\n{downloadDirectory}",
                    "Download complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                ResetForm();
            }
            else
            {
                StatusTextBlock.Text = $"yt-dlp ended with exit code {exitCode}.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Download cancelled.";
            LatestOutputTextBlock.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
            LatestOutputTextBlock.Text = ex.Message;
        }
        finally
        {
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
            SetBusy(false);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _downloadCancellation?.Cancel();
    }

    private void ResetForm()
    {
        UrlTextBox.Clear();
        VideoRadioButton.IsChecked = true;
        HighestQualityRadioButton.IsChecked = true;
        DownloadProgressBar.Value = 0;
        StatusTextBlock.Text = "Ready";
        LatestOutputTextBlock.Text = "Ready for the next download.";
    }

    private void SetBusy(bool busy)
    {
        DownloadButton.IsEnabled = !busy;
        UpdateButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        PasteButton.IsEnabled = !busy;
        UrlTextBox.IsEnabled = !busy;
        VideoRadioButton.IsEnabled = !busy;
        AudioRadioButton.IsEnabled = !busy;
        HighestQualityRadioButton.IsEnabled = !busy;
        MediumQualityRadioButton.IsEnabled = !busy;
        LowestQualityRadioButton.IsEnabled = !busy;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _downloadCancellation?.Cancel();
        base.OnClosing(e);
    }
}
