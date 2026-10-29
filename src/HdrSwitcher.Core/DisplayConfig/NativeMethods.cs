using System.Runtime.InteropServices;

namespace HdrSwitcher.Core;

/// <summary>
/// P/Invoke da API DisplayConfig do Windows (user32.dll).
/// É a mesma API que o próprio Windows usa para ligar/desligar HDR por monitor.
/// </summary>
internal static class NativeMethods
{
    internal const uint QdcOnlyActivePaths = 2;

    internal const int ErrorInsufficientBuffer = 122;
    internal const int ErrorNotSupported = 50;
    internal const int ErrorInvalidParameter = 87;
    internal const int ErrorNotFound = 1168;

    internal enum DisplayConfigDeviceInfoType : uint
    {
        GetSourceName = 1,
        GetTargetName = 2,
        GetTargetPreferredMode = 3,
        GetAdapterName = 4,
        SetTargetPersistence = 5,
        GetTargetBaseType = 6,
        GetSupportedVirtualResolutions = 7,
        SetSupportedVirtualResolutions = 8,
        GetAdvancedColorInfo = 9,
        SetAdvancedColorState = 10,
        GetSdrWhiteLevel = 11,
        GetMonitorSpecialization = 12,
        SetMonitorSpecialization = 13,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DeviceInfoHeader
    {
        public uint Type;
        public uint Size;
        public Luid AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PathSourceInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rational
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PathTargetInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public int OutputTechnology;
        public uint Rotation;
        public uint Scaling;
        public Rational RefreshRate;
        public uint ScanLineOrdering;
        [MarshalAs(UnmanagedType.Bool)] public bool TargetAvailable;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PathInfo
    {
        public PathSourceInfo SourceInfo;
        public PathTargetInfo TargetInfo;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TargetDeviceName
    {
        public DeviceInfoHeader Header;
        public uint Flags;
        public int OutputTechnology;
        public ushort EdidManufactureId;
        public ushort EdidProductCodeId;
        public uint ConnectorInstance;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string MonitorFriendlyDeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string MonitorDevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdvancedColorInfo
    {
        public DeviceInfoHeader Header;
        public uint Value;
        public uint ColorEncoding;
        public uint BitsPerColorChannel;

        public readonly bool AdvancedColorSupported => (Value & 0x1) != 0;
        public readonly bool AdvancedColorEnabled => (Value & 0x2) != 0;
        public readonly bool WideColorEnforced => (Value & 0x4) != 0;
        public readonly bool AdvancedColorForceDisabled => (Value & 0x8) != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SetAdvancedColorState
    {
        public DeviceInfoHeader Header;
        public uint Value; // bit 0: habilitar advanced color (HDR)
    }

    [DllImport("user32.dll", SetLastError = false)]
    internal static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll", SetLastError = false)]
    internal static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        IntPtr pathArray,
        ref uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", SetLastError = false)]
    internal static extern int DisplayConfigGetDeviceInfo(ref TargetDeviceName deviceName);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", SetLastError = false)]
    internal static extern int DisplayConfigGetDeviceInfo(ref AdvancedColorInfo info);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigSetDeviceInfo", SetLastError = false)]
    internal static extern int DisplayConfigSetDeviceInfo(ref SetAdvancedColorState state);
}
