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
        Assert.Contains("ramindownloader-logo.jpg", project);
        Assert.DoesNotContain("ramindownloader-logo.jpg", app);
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "Assets", "RaminDownloader.ico")));
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "Assets", "ramindownloader-logo.jpg")));
        var packaging = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));
        Assert.Contains("$packageAssets", packaging);
        Assert.Contains("RaminDownloader.ico", packaging);
    }

    [Fact]
    public void MainWindowUsesRuntimeBrandingWithoutFragileIconUri()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml.cs"));

        Assert.Contains("LogoImage", xaml);
        Assert.DoesNotContain("Icon=", xaml);
        Assert.Contains("LogoImage.Source", code);
        Assert.Contains("Icon =", code);
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
