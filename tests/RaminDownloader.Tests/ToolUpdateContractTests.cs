namespace RaminDownloader.Tests;

public sealed class ToolUpdateContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ToolManifestIsShippedWithoutToolBinaries()
    {
        var manifestPath = Path.Combine(RepositoryRoot, "src", "RaminDownloader", "Assets", "tools-manifest.json");
        var package = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));

        Assert.True(File.Exists(manifestPath));
        Assert.DoesNotContain("fetch-tools.ps1", package);
        Assert.DoesNotContain("Copy-Item (Join-Path $tools '*.exe')", package);
        Assert.Contains("tools-manifest.json", package);
    }

    [Fact]
    public void MainGuiHasUpdateButtonAndStartupDependencyCheck()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml.cs"));

        Assert.Contains("UpdateButton", xaml);
        Assert.Contains("UpdateButton_Click", xaml);
        Assert.Contains("Window_Loaded", xaml);
        Assert.Contains("EnsureToolsAsync", code);
        Assert.Contains("UpdateAsync", code);
    }

    [Fact]
    public void ControlGuiAlsoHasUpdateButtonAndDependencyCheck()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminYtDlpControl", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminYtDlpControl", "MainWindow.xaml.cs"));

        Assert.Contains("UpdateButton", xaml);
        Assert.Contains("Window_Loaded", xaml);
        Assert.Contains("EnsureToolsAsync", code);
        Assert.Contains("UpdateAsync", code);
    }

    [Fact]
    public void PackageContainsAssetsButNoToolsDirectory()
    {
        var package = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));

        Assert.Contains("$packageAssets", package);
        Assert.DoesNotContain("$packageTools", package);
        Assert.DoesNotContain("tools\\win-x64", package);
        Assert.Contains("tools-manifest.json", package);
        Assert.Contains("Assets", package);
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
