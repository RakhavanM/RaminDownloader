using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using RaminDownloader.Services;

namespace RaminYtDlpControl;

public partial class MainWindow : Window
{
    private Process? _terminalProcess;
    private string? _scriptPath;
    private readonly ToolManager _toolManager;
    private readonly AppUpdateManager _appUpdateManager;
    private CancellationTokenSource? _toolUpdateCancellation;

    public MainWindow()
    {
        InitializeComponent();
        _toolManager = new ToolManager(AppContext.BaseDirectory);
        _appUpdateManager = new AppUpdateManager(AppContext.BaseDirectory);
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
        UpdateButton.IsEnabled = false;
        RunButton.IsEnabled = false;
        CommandPreviewTextBox.Text = "Checking and installing required tools...";
        _toolUpdateCancellation = new CancellationTokenSource();
        try
        {
            var progress = new Progress<string>(message => CommandPreviewTextBox.Text = message);
            await _toolManager.UpdateAsync(progress, _toolUpdateCancellation.Token);
            CommandPreviewTextBox.Text = "yt-dlp, FFmpeg, FFprobe, and Deno are ready.";
        }
        catch (Exception exception)
        {
            CommandPreviewTextBox.Text = exception.Message;
            MessageBox.Show(this, exception.ToString(), "Tool update failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _toolUpdateCancellation?.Dispose();
            _toolUpdateCancellation = null;
            UpdateButton.IsEnabled = true;
            RunButton.IsEnabled = true;
        }
    }

    private async Task CheckForAppUpdateAsync()
    {
        try
        {
            var update = await _appUpdateManager.CheckAsync();
            if (update is null) return;
            var answer = MessageBox.Show(this, $"RaminYtDlpControl {update.LatestVersion} is available. Update both applications now?", "Application update", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
            {
                await _appUpdateManager.DownloadAndScheduleAsync(update, "RaminYtDlpControl.exe", new Progress<string>(message => CommandPreviewTextBox.Text = message));
                Application.Current.Shutdown();
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Application update check failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var urls = UrlsTextBox.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => Uri.TryCreate(line, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                .ToArray();

            if (urls.Length == 0)
            {
                MessageBox.Show(this, "Enter at least one valid http:// or https:// URL.", "Input required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var toolPaths = ToolLocator.Locate(AppContext.BaseDirectory);
            var arguments = BuildArguments(toolPaths.Directory, toolPaths.Deno, urls);
            CommandPreviewTextBox.Text = RenderPreview(toolPaths.YtDlp, arguments);
            LaunchVisibleTerminal(toolPaths.YtDlp, arguments);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.ToString(), "Could not run yt-dlp", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_terminalProcess is { HasExited: false })
            {
                _terminalProcess.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The terminal already closed.
        }
    }

    private List<string> BuildArguments(string toolsDirectory, string denoPath, IReadOnlyList<string> urls)
    {
        var arguments = new List<string>
        {
            "--newline",
            "--windows-filenames",
            "--ffmpeg-location", toolsDirectory,
            "--js-runtimes", $"deno:{denoPath}",
            "--output", Path.Combine(OutputFolderTextBox.Text.Trim(), OutputTemplateTextBox.Text.Trim()),
            "--merge-output-format", Mp4RadioButton.IsChecked == true ? "mp4" : "mkv"
        };

        if (FirefoxCookiesCheckBox.IsChecked == true)
        {
            arguments.Add("--cookies-from-browser");
            arguments.Add("firefox");
        }

        if (NoPlaylistCheckBox.IsChecked == true) arguments.Add("--no-playlist");
        if (ArchiveCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(ArchivePathTextBox.Text))
        {
            arguments.Add("--download-archive");
            arguments.Add(ArchivePathTextBox.Text.Trim());
        }

        var formatPreset = ((ComboBoxItem)FormatPresetComboBox.SelectedItem)?.Content?.ToString();
        var format = formatPreset switch
        {
            "Best MP4-compatible" => "bestvideo*[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best",
            "Audio only" => "bestaudio/best",
            "Custom format" => CustomFormatTextBox.Text.Trim(),
            _ => "bestvideo*+bestaudio/best"
        };
        if (!string.IsNullOrWhiteSpace(format))
        {
            arguments.Add("--format");
            arguments.Add(format);
        }

        if (ExtractAudioCheckBox.IsChecked == true)
        {
            arguments.Add("--extract-audio");
            arguments.Add("--audio-format");
            arguments.Add(((ComboBoxItem)AudioFormatComboBox.SelectedItem)?.Content?.ToString()?.ToLowerInvariant() ?? "mp3");
            arguments.Add("--audio-quality");
            arguments.Add(AudioQualityComboBox.SelectedIndex switch { 1 => "5", 2 => "9", _ => "0" });
            if (KeepVideoCheckBox.IsChecked != true) arguments.Add("--no-keep-video");
        }

        if (EmbedMetadataCheckBox.IsChecked == true) arguments.Add("--embed-metadata");
        if (EmbedThumbnailCheckBox.IsChecked == true) arguments.Add("--embed-thumbnail");
        if (WriteThumbnailCheckBox.IsChecked == true) arguments.Add("--write-thumbnail");
        if (WriteSubsCheckBox.IsChecked == true) arguments.Add("--write-subs");
        if (WriteAutoSubsCheckBox.IsChecked == true) arguments.Add("--write-auto-subs");
        if (WriteDescriptionCheckBox.IsChecked == true) arguments.Add("--write-description");
        if (WriteInfoJsonCheckBox.IsChecked == true) arguments.Add("--write-info-json");
        if (VerboseCheckBox.IsChecked == true) arguments.Add("--verbose");

        AddOptional(arguments, "--sub-langs", SubtitleLanguagesTextBox.Text);
        if (WriteSubsCheckBox.IsChecked == true || WriteAutoSubsCheckBox.IsChecked == true)
        {
            var subtitleFormat = ((ComboBoxItem)SubtitleFormatComboBox.SelectedItem)?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(subtitleFormat) && subtitleFormat != "Best")
            {
                AddOptional(arguments, "--sub-format", subtitleFormat.ToLowerInvariant());
            }
        }
        AddOptional(arguments, "--playlist-start", PlaylistStartTextBox.Text);
        AddOptional(arguments, "--playlist-end", PlaylistEndTextBox.Text);
        if (PlaylistReverseCheckBox.IsChecked == true) arguments.Add("--playlist-reverse");
        if (PlaylistRandomCheckBox.IsChecked == true) arguments.Add("--playlist-random");
        AddOptional(arguments, "--proxy", ProxyTextBox.Text);
        AddOptional(arguments, "--limit-rate", RateLimitTextBox.Text);
        AddOptional(arguments, "--retries", RetriesTextBox.Text);
        AddOptional(arguments, "--fragment-retries", FragmentRetriesTextBox.Text);

        arguments.AddRange(ParseAdditionalArguments(AdditionalArgumentsTextBox.Text));
        arguments.Add("--");
        arguments.AddRange(urls);
        return arguments;
    }

    private void LaunchVisibleTerminal(string ytDlpPath, IReadOnlyList<string> arguments)
    {
        var scriptLines = new List<string>
        {
            "$ErrorActionPreference = 'Continue'",
            $"& {PowerShellQuote(ytDlpPath)} {string.Join(" ", arguments.Select(PowerShellQuote))}",
            "$exitCode = $LASTEXITCODE",
            "if ($exitCode -ne 0) { Write-Host ''; Write-Host ('yt-dlp failed with exit code ' + $exitCode) -ForegroundColor Red; Read-Host 'Press Enter to close this error window' }"
        };

        _scriptPath = Path.Combine(Path.GetTempPath(), $"RaminYtDlpControl-{Guid.NewGuid():N}.ps1");
        File.WriteAllLines(_scriptPath, scriptLines, new UTF8Encoding(false));

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory,
            Arguments = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -File {PowerShellQuote(_scriptPath)}"
        };
        _terminalProcess = Process.Start(startInfo);
        if (_terminalProcess is null) throw new InvalidOperationException("PowerShell could not be started.");
        _terminalProcess.EnableRaisingEvents = true;
        _terminalProcess.Exited += (_, _) => Dispatcher.Invoke(() =>
        {
            CancelButton.IsEnabled = false;
            TryDeleteScript();
        });
        CancelButton.IsEnabled = true;
    }

    private static string RenderPreview(string executable, IReadOnlyList<string> arguments) =>
        $"{PowerShellQuote(executable)} {string.Join(" ", arguments.Select(PowerShellQuote))}";

    private static IEnumerable<string> ParseAdditionalArguments(string text)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(text ?? string.Empty, "(?:(?:\"([^\"]*)\")|([^\\s]+))");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            yield return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        }
    }

    private static void AddOptional(ICollection<string> arguments, string option, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            arguments.Add(option);
            arguments.Add(value.Trim());
        }
    }

    private static string PowerShellQuote(string value) =>
        "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private void TryDeleteScript()
    {
        if (_scriptPath is null) return;
        try { File.Delete(_scriptPath); } catch { /* best effort cleanup */ }
        _scriptPath = null;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        CancelButton_Click(this, new RoutedEventArgs());
        TryDeleteScript();
        base.OnClosing(e);
    }
}
