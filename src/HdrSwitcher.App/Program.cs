namespace HdrSwitcher.App;

internal static class Program
{
    private const string MutexName = @"Local\HdrSwitcher.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        ApplicationConfiguration.Initialize();

        using var context = new TrayApp();
        Application.Run(context);
    }
}
