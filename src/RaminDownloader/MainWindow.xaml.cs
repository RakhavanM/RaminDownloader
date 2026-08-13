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
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        var type = TypeComboBox.SelectedIndex == 1 ? DownloadType.AudioMp3 : DownloadType.Video;
        var quality = QualityComboBox.SelectedIndex switch
        {
            1 => DownloadQuality.Medium,
            2 => DownloadQuality.Lowest,
            _ => DownloadQuality.Highest
        };

        if (!DownloadOptions.TryCreate(UrlTextBox.Text, type, quality, useFirefoxCookies: true, out var options) || options is null)
        {
            StatusTextBlock.Text = "Please enter a complete http:// or https:// URL.";
            return;
        }

        SetBusy(true);
        DownloadProgressBar.Value = 0;
        LogTextBox.Clear();
        _downloadCancellation = new CancellationTokenSource();

        try
        {
            var progress = new Progress<DownloadProgress>(update =>
            {
                if (update.Percent is { } percent)
                {
                    DownloadProgressBar.Value = percent;
                    StatusTextBlock.Text = $"{percent:0.0}%";
                }
                LogTextBox.AppendText(update.Message + Environment.NewLine);
                LogTextBox.ScrollToEnd();
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
                StatusTextBlock.Text = $"Finished. Files are in {downloadDirectory}";
            }
            else
            {
                StatusTextBlock.Text = $"yt-dlp ended with exit code {exitCode}. See the log for details.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
            LogTextBox.AppendText(ex + Environment.NewLine);
        }
        finally
        {
            _downloadCancellation.Dispose();
            _downloadCancellation = null;
            SetBusy(false);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _downloadCancellation?.Cancel();
    }

    private void SetBusy(bool busy)
    {
        DownloadButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        UrlTextBox.IsEnabled = !busy;
        TypeComboBox.IsEnabled = !busy;
        QualityComboBox.IsEnabled = !busy;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _downloadCancellation?.Cancel();
        base.OnClosing(e);
    }
}
