namespace HdrSwitcher.Core;

/// <summary>
/// Um monitor ativo no sistema, com o estado de HDR lido da API DisplayConfig.
/// </summary>
public sealed class MonitorInfo
{
    internal MonitorInfo(
        string id,
        string name,
        string devicePath,
        NativeMethods.Luid adapterId,
        uint targetId,
        bool hdrSupported,
        bool hdrEnabled,
        uint bitsPerColorChannel,
        VideoOutputTechnology outputTechnology)
    {
        Id = id;
        Name = name;
        DevicePath = devicePath;
        AdapterId = adapterId;
        TargetId = targetId;
        HdrSupported = hdrSupported;
        HdrEnabled = hdrEnabled;
        BitsPerColorChannel = bitsPerColorChannel;
        OutputTechnology = outputTechnology;
    }

    /// <summary>Identificador estável do monitor (device path do Windows).</summary>
    public string Id { get; }

    /// <summary>Nome amigável do monitor, ex.: "LG ULTRAGEAR".</summary>
    public string Name { get; }

    public string DevicePath { get; }

    public bool HdrSupported { get; }

    public bool HdrEnabled { get; }

    /// <summary>8 para SDR, 10 ou mais quando HDR está ativo.</summary>
    public uint BitsPerColorChannel { get; }

    public VideoOutputTechnology OutputTechnology { get; }

    public string ConnectionName => OutputTechnology switch
    {
        VideoOutputTechnology.Hd15 => "VGA",
        VideoOutputTechnology.SVideo => "S-Video",
        VideoOutputTechnology.CompositeVideo => "Composto",
        VideoOutputTechnology.ComponentVideo => "Componente",
        VideoOutputTechnology.Dvi => "DVI",
        VideoOutputTechnology.Hdmi => "HDMI",
        VideoOutputTechnology.Lvds => "LVDS",
        VideoOutputTechnology.DJapan => "D-Terminal",
        VideoOutputTechnology.Sdi => "SDI",
        VideoOutputTechnology.DisplayPortExternal => "DisplayPort",
        VideoOutputTechnology.DisplayPortEmbedded => "DisplayPort (embutido)",
        VideoOutputTechnology.UdiExternal => "UDI",
        VideoOutputTechnology.UdiEmbedded => "UDI (embutido)",
        VideoOutputTechnology.SdtvDongle => "Dongle SDTV",
        VideoOutputTechnology.Miracast => "Miracast",
        VideoOutputTechnology.IndirectWired => "Sem fio (com fio)",
        VideoOutputTechnology.IndirectVirtual => "Sem fio (virtual)",
        VideoOutputTechnology.Internal => "Interno",
        VideoOutputTechnology.ForceUefi => "UEFI",
        _ => "Outro",
    };

    internal NativeMethods.Luid AdapterId { get; }

    internal uint TargetId { get; }

    public override string ToString() =>
        HdrSupported
            ? $"{Name} ({ConnectionName}) — HDR {(HdrEnabled ? "ligado" : "desligado")}"
            : $"{Name} ({ConnectionName}) — sem HDR";
}
