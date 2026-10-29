using System.ComponentModel;
using System.Runtime.InteropServices;
using static HdrSwitcher.Core.NativeMethods;

namespace HdrSwitcher.Core;

/// <summary>
/// Lê os monitores ativos e liga/desliga HDR usando a API DisplayConfig do Windows.
/// Não exige privilégios de administrador nem driver adicional.
/// </summary>
public sealed class MonitorService
{
    private const int MaxQueryAttempts = 4;

    // sizeof(DISPLAYCONFIG_MODE_INFO) no Windows atual. Os modos não são usados,
    // mas a API exige um buffer com o tamanho pedido por GetDisplayConfigBufferSizes.
    private const int ModeInfoSize = 64;

    /// <summary>Lista os monitores ativos no momento, com nome, conexão e estado de HDR.</summary>
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        PathInfo[] paths = QueryActivePaths();
        var monitors = new List<MonitorInfo>(paths.Length);

        foreach (PathInfo path in paths)
        {
            PathTargetInfo target = path.TargetInfo;
            TargetDeviceName nameInfo = GetTargetName(target);
            AdvancedColorInfo color = GetAdvancedColorInfo(target);

            string name = string.IsNullOrWhiteSpace(nameInfo.MonitorFriendlyDeviceName)
                ? $"Monitor {target.Id}"
                : nameInfo.MonitorFriendlyDeviceName.Trim();

            string devicePath = nameInfo.MonitorDevicePath ?? string.Empty;
            string id = devicePath.Length > 0
                ? devicePath
                : $"{target.AdapterId.HighPart}:{target.AdapterId.LowPart}:{target.Id}";

            monitors.Add(new MonitorInfo(
                id,
                name,
                devicePath,
                target.AdapterId,
                target.Id,
                color.AdvancedColorSupported,
                color.AdvancedColorEnabled,
                color.BitsPerColorChannel,
                (VideoOutputTechnology)target.OutputTechnology));
        }

        return monitors;
    }

    /// <summary>Liga ou desliga HDR no monitor informado.</summary>
    public HdrChangeResult SetHdr(MonitorInfo monitor, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        if (!monitor.HdrSupported)
            return HdrChangeResult.CreateNotSupported($"{monitor.Name} não suporta HDR.");

        if (monitor.HdrEnabled == enabled)
            return HdrChangeResult.CreateOk(monitor, $"{monitor.Name}: HDR já estava {(enabled ? "ligado" : "desligado")}.", changed: false);

        var state = new SetAdvancedColorState
        {
            Header = new DeviceInfoHeader
            {
                Type = (uint)NativeMethods.DisplayConfigDeviceInfoType.SetAdvancedColorState,
                Size = (uint)Marshal.SizeOf<SetAdvancedColorState>(),
                AdapterId = monitor.AdapterId,
                Id = monitor.TargetId,
            },
            Value = enabled ? 1u : 0u,
        };

        int error = NativeMethods.DisplayConfigSetDeviceInfo(ref state);
        if (error != 0)
            return HdrChangeResult.CreateError(monitor, $"{monitor.Name}: falha ao {(enabled ? "ligar" : "desligar")} HDR — {DescribeError(error)}.");

        return HdrChangeResult.CreateOk(monitor, $"{monitor.Name}: HDR {(enabled ? "ligado" : "desligado")}.", changed: true);
    }

    /// <summary>Liga ou desliga HDR no monitor cujo <see cref="MonitorInfo.Id"/> for informado.</summary>
    public HdrChangeResult SetHdr(string monitorId, bool enabled)
    {
        MonitorInfo? monitor = FindMonitor(monitorId);
        return monitor is null
            ? HdrChangeResult.CreateNotSupported($"Monitor \"{monitorId}\" não encontrado.")
            : SetHdr(monitor, enabled);
    }

    /// <summary>Aplica o mesmo estado a todos os monitores com suporte a HDR.</summary>
    public IReadOnlyList<HdrChangeResult> SetAll(bool enabled)
    {
        List<MonitorInfo> supported = GetMonitors().Where(m => m.HdrSupported).ToList();
        return supported.Select(m => SetHdr(m, enabled)).ToList();
    }

    /// <summary>
    /// Alterna todos os monitores com suporte: se algum estiver com HDR desligado,
    /// liga em todos; caso contrário, desliga em todos.
    /// </summary>
    public IReadOnlyList<HdrChangeResult> ToggleAll()
    {
        List<MonitorInfo> supported = GetMonitors().Where(m => m.HdrSupported).ToList();
        if (supported.Count == 0)
            return Array.Empty<HdrChangeResult>();

        bool turnOn = supported.Any(m => !m.HdrEnabled);
        return supported.Select(m => SetHdr(m, turnOn)).ToList();
    }

    private MonitorInfo? FindMonitor(string monitorId) =>
        GetMonitors().FirstOrDefault(m => string.Equals(m.Id, monitorId, StringComparison.OrdinalIgnoreCase));

    private static PathInfo[] QueryActivePaths()
    {
        for (int attempt = 0; attempt < MaxQueryAttempts; attempt++)
        {
            int error = NativeMethods.GetDisplayConfigBufferSizes(
                NativeMethods.QdcOnlyActivePaths,
                out uint pathCount,
                out uint modeCount);

            if (error != 0)
                throw new Win32Exception(error, "Não foi possível consultar a configuração de vídeo.");

            int pathSize = Marshal.SizeOf<PathInfo>();
            IntPtr pathsPtr = Marshal.AllocHGlobal(Math.Max(1, (int)pathCount * pathSize));
            IntPtr modesPtr = Marshal.AllocHGlobal(Math.Max(1, (int)modeCount * ModeInfoSize));

            try
            {
                uint pathsUsed = pathCount;
                uint modesUsed = modeCount;

                error = NativeMethods.QueryDisplayConfig(
                    NativeMethods.QdcOnlyActivePaths,
                    ref pathsUsed,
                    pathsPtr,
                    ref modesUsed,
                    modesPtr,
                    IntPtr.Zero);

                // A topologia mudou entre as duas chamadas: tenta de novo.
                if (error == NativeMethods.ErrorInsufficientBuffer)
                    continue;

                if (error != 0)
                    throw new Win32Exception(error, "Não foi possível enumerar os monitores.");

                var paths = new PathInfo[pathsUsed];
                for (int i = 0; i < pathsUsed; i++)
                    paths[i] = Marshal.PtrToStructure<PathInfo>(IntPtr.Add(pathsPtr, i * pathSize));

                return paths;
            }
            finally
            {
                Marshal.FreeHGlobal(pathsPtr);
                Marshal.FreeHGlobal(modesPtr);
            }
        }

        throw new InvalidOperationException("A configuração de vídeo mudou demais durante a leitura dos monitores.");
    }

    private static TargetDeviceName GetTargetName(PathTargetInfo target)
    {
        var info = new TargetDeviceName
        {
            Header = CreateHeader(
                NativeMethods.DisplayConfigDeviceInfoType.GetTargetName,
                Marshal.SizeOf<TargetDeviceName>(),
                target.AdapterId,
                target.Id),
            MonitorFriendlyDeviceName = string.Empty,
            MonitorDevicePath = string.Empty,
        };

        return NativeMethods.DisplayConfigGetDeviceInfo(ref info) == 0 ? info : default;
    }

    private static AdvancedColorInfo GetAdvancedColorInfo(PathTargetInfo target)
    {
        var info = new AdvancedColorInfo
        {
            Header = CreateHeader(
                NativeMethods.DisplayConfigDeviceInfoType.GetAdvancedColorInfo,
                Marshal.SizeOf<AdvancedColorInfo>(),
                target.AdapterId,
                target.Id),
        };

        // Se o monitor não expõe a informação, devolve tudo zerado (sem HDR).
        return NativeMethods.DisplayConfigGetDeviceInfo(ref info) == 0 ? info : default;
    }

    private static DeviceInfoHeader CreateHeader(
        NativeMethods.DisplayConfigDeviceInfoType type,
        int size,
        NativeMethods.Luid adapterId,
        uint targetId) =>
        new()
        {
            Type = (uint)type,
            Size = (uint)size,
            AdapterId = adapterId,
            Id = targetId,
        };

    private static string DescribeError(int error) => error switch
    {
        NativeMethods.ErrorNotSupported => "o monitor não suporta HDR no modo atual",
        NativeMethods.ErrorInvalidParameter => "parâmetro inválido (a resolução/modo atual pode não suportar HDR)",
        NativeMethods.ErrorNotFound => "monitor não encontrado (a topologia mudou?)",
        _ => $"{new Win32Exception(error).Message} (código {error})",
    };
}
