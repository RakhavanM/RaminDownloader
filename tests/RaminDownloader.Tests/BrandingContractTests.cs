namespace RaminDownloader.Tests;

public sealed class BrandingContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void BrandingImagesAreNotUsedOrPackaged()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "RaminDownloader.csproj"));
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml.cs"));
        var packaging = File.ReadAllText(Path.Combine(RepositoryRoot, "packaging", "package.ps1"));

        Assert.DoesNotContain("ApplicationIcon", project);
        Assert.DoesNotContain("RaminDownloader.ico", project);
        Assert.DoesNotContain("ramindownloader-logo.jpg", project);
        Assert.DoesNotContain("LogoImage", xaml);
        Assert.DoesNotContain("ApplyBranding", code);
        Assert.DoesNotContain("BitmapImage", code);
        Assert.DoesNotContain("RaminDownloader.ico", packaging);
        Assert.DoesNotContain("ramindownloader-logo.jpg", packaging);
    }

    [Fact]
    public void MainWindowUsesTextOnlyBranding()
    {
        var xaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "MainWindow.xaml"));

        Assert.Contains("RAMIN", xaml);
        Assert.Contains("DOWNLOADER", xaml);
        Assert.DoesNotContain("<Image", xaml);
        Assert.DoesNotContain("Icon=", xaml);
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
