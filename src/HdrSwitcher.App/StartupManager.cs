using Microsoft.Win32;

namespace HdrSwitcher.App;

/// <summary>Liga/desliga a inicialização automática via HKCU\...\Run (sem admin).</summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "HdrSwitcher";

    public static bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string value && value.Length > 0;
        }
    }

    public static void Set(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Não foi possível abrir a chave de inicialização do Windows.");

        if (enabled)
        {
            string executable = Environment.ProcessPath
                ?? throw new InvalidOperationException("Não foi possível descobrir o caminho do executável.");

            key.SetValue(ValueName, $"\"{executable}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
