using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RaminDownloader.Models;

namespace RaminDownloader.Desktop;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _active;
    private string? _outputFolder;
    private bool _choosingOutput;

    public MainWindow()
    {
        InitializeComponent();
        DownloadFolder.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Ramin Downloader");
        FormatBox.ItemsSource = MediaJobs.Formats;
        FormatBox.SelectedIndex = 0;
        Operation.SelectionChanged += (_, _) => UpdateMediaForm();
        FileCard.AddHandler(DragDrop.DragOverEvent, (_, e) =>
            e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None);
        FileCard.AddHandler(DragDrop.DropEvent, DropFile);
        UpdateMediaForm();
        Closing += (_, _) => _active?.Cancel();
    }

    private void CookiesChanged(object? sender, RoutedEventArgs e)
    {
        if (ProfileRow is not null)
            ProfileRow.IsVisible = Cookies.IsChecked == true;
    }

    private async void PasteLink(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is { } clipboard)
            Url.Text = await clipboard.TryGetTextAsync() ?? "";
        Url.Focus();
    }

    private async void ChooseProfile(object? sender, RoutedEventArgs e) =>
        FirefoxProfile.Text = await PickFolderAsync("Choose the Firefox profile folder") ?? FirefoxProfile.Text;

    private async void ChooseDownloadFolder(object? sender, RoutedEventArgs e) =>
        DownloadFolder.Text = await PickFolderAsync("Choose a download folder") ?? DownloadFolder.Text;

    private async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = title });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    private async void ChooseInput(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new() { Title = "Choose audio or video" });
        SelectInput(files.FirstOrDefault());
    }

    private void DropFile(object? sender, DragEventArgs e) =>
        SelectInput(e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>().FirstOrDefault());

    private void SelectInput(IStorageFile? file)
    {
        if (_active is null && file?.TryGetLocalPath() is { } path)
            InputFile.Text = path;
    }

    private void UpdateMediaForm()
    {
        ResizePanel.IsVisible = Operation.SelectedIndex == 0;
        CompressPanel.IsVisible = Operation.SelectedIndex == 1;
        ConvertPanel.IsVisible = Operation.SelectedIndex == 2;
        MediaHint.Text = Operation.SelectedIndex switch
        {
            0 => "Fits within the selected height while keeping the original proportions. Output: MP4.",
            1 => "Keeps the frame size and re-encodes to MP4. Final size depends on the source.",
            _ => "Choose an audio format to extract the first audio track from a video."
        };
    }

    private async void StartDownload(object? sender, RoutedEventArgs e)
    {
        if (!DownloadOptions.TryCreate(Url.Text, (DownloadType)DownloadTypeBox.SelectedIndex,
            (DownloadQuality)DownloadQualityBox.SelectedIndex, Cookies.IsChecked == true, out var options))
        {
            Status.Text = "Enter a valid HTTP or HTTPS link.";
            return;
        }

        var profile = FirefoxProfile.Text?.Trim();
        var folder = DownloadFolder.Text!;
        await RunJobAsync("Downloading…", "Download complete.", folder,
            (progress, token) => MediaJobs.DownloadAsync(options!, profile, folder, progress, token));
    }

    private async void StartMedia(object? sender, RoutedEventArgs e)
    {
        if (_choosingOutput || _active is not null)
            return;
        if (InputFile.Text is not { } input || !File.Exists(input))
        {
            Status.Text = "Choose an audio or video file first.";
            return;
        }
        var action = (MediaAction)Operation.SelectedIndex;
        var format = action == MediaAction.Convert ? (string)FormatBox.SelectedItem! : "MP4";
        var height = (int)(HeightBox.Value ?? 720);
        var crf = Compression.SelectedIndex switch { 0 => 22, 2 => 32, _ => 27 };
        var options = new MediaOptions(action, format, height, crf);
        var extension = format.ToLowerInvariant();

        _choosingOutput = true;
        string? output;
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new()
            {
                Title = "Save output as",
                DefaultExtension = extension,
                SuggestedFileName = $"{Path.GetFileNameWithoutExtension(input)}-{action.ToString().ToLowerInvariant()}.{extension}",
                FileTypeChoices = [new FilePickerFileType(format) { Patterns = [$"*.{extension}"] }]
            });
            output = file?.TryGetLocalPath();
        }
        finally
        {
            _choosingOutput = false;
        }

        if (output is null)
            return;
        if (!Path.GetExtension(output).Equals($".{extension}", StringComparison.OrdinalIgnoreCase))
            output += $".{extension}";

        var destination = output;
        await RunJobAsync("Processing media…", "File saved.", Path.GetDirectoryName(destination)!,
            (progress, token) => MediaJobs.ProcessAsync(input, destination, options, progress, token));
    }

    private async Task RunJobAsync(string running, string complete, string folder,
        Func<IProgress<double>, CancellationToken, Task> job)
    {
        if (_active is not null)
            return;

        using var cancellation = new CancellationTokenSource();
        _active = cancellation;
        Workspace.IsEnabled = false;
        OpenOutput.IsVisible = false;
        Cancel.IsEnabled = true;
        Progress.Value = 0;
        Progress.IsIndeterminate = true;
        ToolTip.SetTip(Status, null);
        Status.Text = running;
        var progress = new Progress<double>(value =>
        {
            if (!ReferenceEquals(_active, cancellation) || cancellation.IsCancellationRequested)
                return;
            Progress.IsIndeterminate = false;
            Progress.Value = value;
        });

        try
        {
            await job(progress, cancellation.Token);
            Progress.Value = 100;
            Status.Text = complete;
            _outputFolder = folder;
            OpenOutput.IsVisible = true;
        }
        catch (OperationCanceledException)
        {
            Progress.Value = 0;
            Status.Text = "Cancelled.";
        }
        catch (Exception exception)
        {
            Progress.Value = 0;
            Status.Text = exception.Message;
            ToolTip.SetTip(Status, exception.Message);
        }
        finally
        {
            _active = null;
            Progress.IsIndeterminate = false;
            Workspace.IsEnabled = true;
            Cancel.IsEnabled = false;
        }
    }

    private void CancelJob(object? sender, RoutedEventArgs e)
    {
        _active?.Cancel();
        Cancel.IsEnabled = false;
    }

    private void OpenFolder(object? sender, RoutedEventArgs e)
    {
        if (_outputFolder is null)
            return;
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(_outputFolder) { UseShellExecute = true })?.Dispose();
            else
            {
                var start = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
                start.ArgumentList.Add(_outputFolder);
                Process.Start(start)?.Dispose();
            }
        }
        catch (Exception)
        {
            Status.Text = $"Output folder: {_outputFolder}";
        }
    }
}
