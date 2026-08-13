namespace RaminDownloader.Tests;

public sealed class UiPackagingContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void MainWindowContainsPasteButtonRadioSelectorsAndScrollableLog()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml"));

        Assert.Contains("PasteButton", xaml);
        Assert.Contains("RadioButton", xaml);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", xaml);
    }

    [Fact]
    public void MainWindowUsesClipboardPasteAndLogAutoScrollHandlers()
    {
        var code = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml.cs"));

        Assert.Contains("Clipboard.GetText", code);
        Assert.Contains("LogTextBox.ScrollToEnd", code);
        Assert.Contains("PasteButton_Click", code);
    }

    [Fact]
    public void ProjectPublishesAsSingleFileWithoutBundlingExternalToolsInsideIt()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "RaminDownloader.csproj"));
        var package = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));

        Assert.Contains("PublishSingleFile", project);
        Assert.Contains("IncludeNativeLibrariesForSelfExtract", project);
        Assert.Contains("tools\\win-x64", package);
        Assert.Contains("RaminDownloader.exe", package);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RaminDownloader.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
