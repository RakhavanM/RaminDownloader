using System.ComponentModel;
using System.IO;
using System.Windows;
using RaminDownloader.Models;
using RaminDownloader.Services;

namespace RaminDownloader;

public partial class MainWindow : Window
{
    private readonly YtDlpRunner _runner = new();
    private CancellationTokenSource? _downloadCancellation;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            if (Application.Current?.MainWindow is null)
            {
                Application.Current!.MainWindow = this;
            }
        };
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
