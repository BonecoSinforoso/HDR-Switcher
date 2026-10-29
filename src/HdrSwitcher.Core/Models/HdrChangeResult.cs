namespace HdrSwitcher.Core;

/// <summary>Resultado de uma tentativa de ligar/desligar HDR em um monitor.</summary>
public sealed class HdrChangeResult
{
    private HdrChangeResult(bool success, bool changed, bool notSupported, MonitorInfo? monitor, string message)
    {
        Success = success;
        Changed = changed;
        NotSupported = notSupported;
        Monitor = monitor;
        Message = message;
    }

    public bool Success { get; }

    /// <summary>True quando o estado realmente mudou (false se já estava no estado pedido).</summary>
    public bool Changed { get; }

    public bool NotSupported { get; }

    public MonitorInfo? Monitor { get; }

    public string Message { get; }

    internal static HdrChangeResult CreateOk(MonitorInfo monitor, string message, bool changed) =>
        new(true, changed, false, monitor, message);

    internal static HdrChangeResult CreateNotSupported(string message) =>
        new(false, false, true, null, message);

    internal static HdrChangeResult CreateError(MonitorInfo monitor, string message) =>
        new(false, false, false, monitor, message);
}
