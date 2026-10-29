namespace HdrSwitcher.Core;

/// <summary>
/// Tipo de saída de vídeo do monitor (DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY).
/// </summary>
public enum VideoOutputTechnology
{
    Other = -1,
    Hd15 = 0,
    SVideo = 1,
    CompositeVideo = 2,
    ComponentVideo = 3,
    Dvi = 4,
    Hdmi = 5,
    Lvds = 6,
    DJapan = 8,
    Sdi = 9,
    DisplayPortExternal = 10,
    DisplayPortEmbedded = 11,
    UdiExternal = 12,
    UdiEmbedded = 13,
    SdtvDongle = 14,
    Miracast = 15,
    IndirectWired = 16,
    IndirectVirtual = 17,
    Internal = 18,
    ForceUefi = 19,
}
