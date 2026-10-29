using System.Text;
using HdrSwitcher.Core;

namespace HdrSwitcher.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var service = new MonitorService();

        try
        {
            if (args.Length == 0)
            {
                PrintMonitors(service.GetMonitors());
                Console.WriteLine();
                PrintUsage();
                return 0;
            }

            return args[0].ToLowerInvariant() switch
            {
                "list" or "ls" => List(service),
                "on" => Apply(service, args, enabled: true),
                "off" => Apply(service, args, enabled: false),
                "toggle" => Toggle(service, args),
                "all" => ApplyAll(service, args),
                "help" or "-h" or "--help" => Help(),
                _ => Unknown(args[0]),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"erro: {ex.Message}");
            return 1;
        }
    }

    private static int List(MonitorService service)
    {
        PrintMonitors(service.GetMonitors());
        return 0;
    }

    private static int Apply(MonitorService service, string[] args, bool enabled)
    {
        string filter = string.Join(' ', args.Skip(1)).Trim();
        if (filter.Length == 0)
        {
            Console.Error.WriteLine("informe o nome (ou parte do nome) do monitor.");
            return 2;
        }

        List<MonitorInfo> matches = Find(service, filter);
        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"nenhum monitor ativo corresponde a \"{filter}\".");
            return 2;
        }

        int failures = 0;
        foreach (MonitorInfo monitor in matches)
        {
            HdrChangeResult result = service.SetHdr(monitor, enabled);
            Console.WriteLine(result.Message);
            if (!result.Success)
                failures++;
        }

        return failures == 0 ? 0 : 1;
    }

    private static int Toggle(MonitorService service, string[] args)
    {
        string filter = string.Join(' ', args.Skip(1)).Trim();
        if (filter.Length == 0)
        {
            Console.Error.WriteLine("informe o nome (ou parte do nome) do monitor.");
            return 2;
        }

        List<MonitorInfo> matches = Find(service, filter);
        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"nenhum monitor ativo corresponde a \"{filter}\".");
            return 2;
        }

        int failures = 0;
        foreach (MonitorInfo monitor in matches)
        {
            HdrChangeResult result = service.SetHdr(monitor, !monitor.HdrEnabled);
            Console.WriteLine(result.Message);
            if (!result.Success)
                failures++;
        }

        return failures == 0 ? 0 : 1;
    }

    private static int ApplyAll(MonitorService service, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("uso: hdr all on|off|toggle");
            return 2;
        }

        IReadOnlyList<HdrChangeResult> results;
        switch (args[1].ToLowerInvariant())
        {
            case "on":
                results = service.SetAll(enabled: true);
                break;
            case "off":
                results = service.SetAll(enabled: false);
                break;
            case "toggle":
                results = service.ToggleAll();
                break;
            default:
                Console.Error.WriteLine("uso: hdr all on|off|toggle");
                return 2;
        }

        if (results.Count == 0)
        {
            Console.WriteLine("nenhum monitor com suporte a HDR encontrado.");
            return 0;
        }

        int failures = 0;
        foreach (HdrChangeResult result in results)
        {
            Console.WriteLine(result.Message);
            if (!result.Success)
                failures++;
        }

        return failures == 0 ? 0 : 1;
    }

    private static int Help()
    {
        PrintUsage();
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"comando desconhecido: {command}");
        PrintUsage();
        return 2;
    }

    private static List<MonitorInfo> Find(MonitorService service, string filter) =>
        service.GetMonitors()
            .Where(m => m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static void PrintMonitors(IReadOnlyList<MonitorInfo> monitors)
    {
        if (monitors.Count == 0)
        {
            Console.WriteLine("nenhum monitor ativo encontrado.");
            return;
        }

        Console.WriteLine($"{"MONITOR",-24} {"CONEXÃO",-14} {"HDR",-11} {"ESTADO",-10} BITS");
        Console.WriteLine(new string('-', 72));

        foreach (MonitorInfo monitor in monitors)
        {
            string supported = monitor.HdrSupported ? "suportado" : "não";
            string state = monitor.HdrSupported ? (monitor.HdrEnabled ? "ligado" : "desligado") : "-";

            Console.WriteLine(
                $"{Truncate(monitor.Name, 24),-24} {monitor.ConnectionName,-14} {supported,-11} {state,-10} {monitor.BitsPerColorChannel}");
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    private static void PrintUsage()
    {
        Console.WriteLine("""
            Uso:
              hdr list                  Lista monitores e o estado do HDR
              hdr on <nome>             Liga o HDR nos monitores que casarem com o nome
              hdr off <nome>            Desliga o HDR
              hdr toggle <nome>         Alterna o HDR
              hdr all on|off|toggle     Aplica a todos os monitores com HDR

            Exemplos:
              hdr on ultragear
              hdr off samsung
              hdr all toggle
            """);
    }
}
