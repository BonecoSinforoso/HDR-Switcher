using HdrSwitcher.Core;
using Microsoft.Win32;

namespace HdrSwitcher.App;

internal sealed class TrayApp : ApplicationContext
{
    private readonly MonitorService _monitors = new();
    private readonly AppSettings _settings;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _tray;
    private readonly HotkeyWindow _hotkey;
    private readonly Control _dispatcher;
    private Icon? _currentIcon;

    public TrayApp()
    {
        _settings = SettingsStore.Load();
        _settings.StartWithWindows = StartupManager.IsEnabled;

        // Control invisível usado para voltar ao thread da UI quando o Windows avisa
        // que a configuração de vídeo mudou.
        _dispatcher = new Control();
        _ = _dispatcher.Handle;

        _menu = new ContextMenuStrip { ShowCheckMargin = true, ShowImageMargin = false };
        _menu.Opening += (_, _) => RebuildMenu();

        _tray = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _tray.DoubleClick += (_, _) => ToggleAll();

        _hotkey = new HotkeyWindow();
        _hotkey.HotkeyPressed += (_, _) => ToggleAll();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        RefreshIcon();

        if (!_hotkey.Registered)
        {
            Notify($"Não foi possível registrar o atalho {HotkeyWindow.HotkeyDisplayName} (outro app pode estar usando).");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            _hotkey.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _menu.Dispose();
            _dispatcher.Dispose();
            _currentIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_dispatcher.IsDisposed)
            return;

        _dispatcher.BeginInvoke(new Action(RefreshIcon));
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        IReadOnlyList<MonitorInfo> monitors;
        try
        {
            monitors = _monitors.GetMonitors();
        }
        catch (Exception ex)
        {
            _menu.Items.Add(new ToolStripMenuItem($"Erro ao ler monitores: {ex.Message}") { Enabled = false });
            _menu.Items.Add(new ToolStripSeparator());
            AddCommonItems();
            return;
        }

        foreach (MonitorInfo monitor in monitors)
            _menu.Items.Add(CreateMonitorItem(monitor));

        if (monitors.Count == 0)
            _menu.Items.Add(new ToolStripMenuItem("Nenhum monitor detectado") { Enabled = false });

        _menu.Items.Add(new ToolStripSeparator());
        AddCommonItems();
    }

    private ToolStripMenuItem CreateMonitorItem(MonitorInfo monitor)
    {
        var item = new ToolStripMenuItem($"{monitor.Name}  ·  {monitor.ConnectionName}")
        {
            Checked = monitor.HdrEnabled,
            Enabled = monitor.HdrSupported,
            ToolTipText = monitor.HdrSupported
                ? (monitor.HdrEnabled ? "HDR ligado — clique para desligar" : "HDR desligado — clique para ligar")
                : "Este monitor não suporta HDR",
        };

        if (monitor.HdrSupported)
            item.Click += (_, _) => SetHdr(monitor, !monitor.HdrEnabled);

        return item;
    }

    private void AddCommonItems()
    {
        _menu.Items.Add(new ToolStripMenuItem(
            $"Alternar todos\t{HotkeyWindow.HotkeyDisplayName}",
            null,
            (_, _) => ToggleAll()));

        _menu.Items.Add(new ToolStripMenuItem("Atualizar agora", null, (_, _) => RefreshIcon()));
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripMenuItem("Iniciar com o Windows", null, (_, _) => ToggleStartWithWindows())
        {
            Checked = StartupManager.IsEnabled,
        });

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Sair", null, (_, _) => ExitApplication()));
    }

    private void SetHdr(MonitorInfo monitor, bool enabled)
    {
        HdrChangeResult result = _monitors.SetHdr(monitor, enabled);
        Notify(result.Message);
        RefreshIcon();
    }

    private void ToggleAll()
    {
        try
        {
            IReadOnlyList<HdrChangeResult> results = _monitors.ToggleAll();
            RefreshIcon();

            if (results.Count == 0)
            {
                Notify("Nenhum monitor com HDR detectado.");
                return;
            }

            List<HdrChangeResult> failures = results.Where(r => !r.Success).ToList();
            if (failures.Count > 0)
            {
                Notify($"Falha em {failures.Count} monitor(es): {failures[0].Message}");
                return;
            }

            List<MonitorInfo> supported = _monitors.GetMonitors().Where(m => m.HdrSupported).ToList();
            bool allOn = supported.Count > 0 && supported.All(m => m.HdrEnabled);

            Notify(allOn
                ? $"HDR ligado em {supported.Count} monitor(es)."
                : "HDR desligado.");
        }
        catch (Exception ex)
        {
            Notify($"Falha ao alternar HDR: {ex.Message}");
        }
    }

    private void ToggleStartWithWindows()
    {
        bool enable = !StartupManager.IsEnabled;

        try
        {
            StartupManager.Set(enable);
            _settings.StartWithWindows = enable;
            SettingsStore.Save(_settings);
            Notify(enable ? "Vai iniciar junto com o Windows." : "Não vai mais iniciar com o Windows.");
        }
        catch (Exception ex)
        {
            Notify($"Falha ao alterar a inicialização: {ex.Message}");
        }
    }

    private void RefreshIcon()
    {
        bool anyOn = false;
        try
        {
            anyOn = _monitors.GetMonitors().Any(m => m.HdrEnabled);
        }
        catch
        {
            // Mantém o ícone anterior se a leitura falhar.
        }

        Icon icon = AppIconFactory.Create(anyOn);
        _tray.Icon = icon;
        _tray.Text = anyOn ? "HDR Switcher — HDR ligado" : "HDR Switcher — HDR desligado";

        _currentIcon?.Dispose();
        _currentIcon = icon;
    }

    private void Notify(string message)
    {
        _tray.BalloonTipTitle = "HDR Switcher";
        _tray.BalloonTipText = message;
        _tray.ShowBalloonTip(2500);
    }

    private void ExitApplication()
    {
        _tray.Visible = false;
        ExitThread();
    }
}
