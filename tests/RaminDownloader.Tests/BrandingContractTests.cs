namespace RaminDownloader.Tests;

public sealed class BrandingContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ProjectDeclaresAppIconAndLogoResource()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "RaminDownloader.csproj"));
        var app = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "App.xaml"));

        Assert.Contains("ApplicationIcon", project);
        Assert.Contains("RaminDownloader.ico", project);
        Assert.Contains("ramindownloader-logo.jpg", app);
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "Assets", "RaminDownloader.ico")));
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "Assets", "ramindownloader-logo.jpg")));
        var packaging = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));
        Assert.DoesNotContain("Copy-Item (Join-Path $root 'src\\RaminDownloader\\Assets\\ramindownloader-logo.jpg')", packaging);
    }

    [Fact]
    public void MainWindowDisplaysLogo()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml"));

        Assert.Contains("LogoImage", xaml);
        Assert.Contains("{StaticResource AppLogo}", xaml);
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
