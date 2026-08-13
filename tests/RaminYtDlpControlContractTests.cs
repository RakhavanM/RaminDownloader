namespace RaminDownloader.Tests;

public sealed class RaminYtDlpControlContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void CompanionProjectUsesApprovedNameAndNoLogo()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminYtDlpControl", "RaminYtDlpControl.csproj"));
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminYtDlpControl", "MainWindow.xaml"));

        Assert.Contains("RaminYtDlpControl", project);
        Assert.Contains("TabControl", xaml);
        Assert.DoesNotContain("AppLogo", xaml);
        Assert.DoesNotContain("RaminDownloader.ico", xaml);
    }

    [Fact]
    public void CompanionGuiContainsApprovedTabsAndRunControls()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminYtDlpControl", "MainWindow.xaml"));

        foreach (var tab in new[] { "General", "Format", "Post-processing", "Subtitles & Metadata", "Playlist", "Network", "Advanced" })
        {
            Assert.Contains(tab, xaml);
        }

        Assert.Contains("RunButton", xaml);
        Assert.Contains("CancelButton", xaml);
        Assert.Contains("AdditionalArgumentsTextBox", xaml);
    }

    [Fact]
    public void PackagingIncludesCompanionExecutable()
    {
        var package = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));

        Assert.Contains("RaminDownloader.exe", package);
        Assert.Contains("RaminYtDlpControl.exe", package);
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

