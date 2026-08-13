namespace RaminDownloader.Tests;

public sealed class StartupContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ApplicationUsesExplicitStartupHandlerForVisibleStartupErrors()
    {
        var app = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "App.xaml"));
        var code = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "RaminDownloader", "App.xaml.cs"));

        Assert.DoesNotContain("StartupUri=", app);
        Assert.Contains("OnStartup", code);
        Assert.Contains("DispatcherUnhandledException", code);
        Assert.Contains("MessageBox.Show", code);
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
