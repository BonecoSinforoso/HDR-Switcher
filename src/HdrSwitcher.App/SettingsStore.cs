using System.Text.Json;

namespace HdrSwitcher.App;

internal sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
}

internal static class SettingsStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HdrSwitcher");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            // Configuração corrompida não deve impedir o app de abrir.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch
        {
            // Falha ao salvar preferências não é motivo para travar o app.
        }
    }
}
