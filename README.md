# HDR Switcher

App de bandeja do Windows que detecta os monitores conectados e liga/desliga HDR em cada um com um clique — ou com o atalho global **Ctrl+Alt+H**.

Usa a API **DisplayConfig** do próprio Windows (`user32.dll`), então não precisa de driver, software de terceiros nem privilégios de administrador.

## Recursos

- Detecção automática dos monitores ativos ao abrir o menu e ao plugar/desplugar telas
- Um item por monitor, com check indicando se o HDR está ligado
- Monitores sem suporte a HDR aparecem desabilitados ("sem HDR")
- Atalho global **Ctrl+Alt+H** alterna todos: liga se algum estiver desligado, desliga se todos estiverem ligados
- Ícone da bandeja muda de cor (azul = HDR ligado em algum monitor, cinza = tudo desligado)
- Duplo clique no ícone também alterna todos
- Opção "Iniciar com o Windows" (registro `HKCU\...\Run`, sem admin)
- Instância única
- CLI para scripts/Stream Deck

## Requisitos

- Windows 10 1803+ ou Windows 11
- Para compilar: .NET SDK 9

## Uso

Rode `HdrSwitcher.exe`. Ele fica na bandeja, sem janela principal.

**Menu (clique no ícone):**

```
LU28R55  ·  DisplayPort          [✓]
LG IPS FULLHD  ·  HDMI           (desabilitado — sem HDR)
LG ULTRAGEAR  ·  DisplayPort     [ ]
─────────────────────────────
Alternar todos            Ctrl+Alt+H
Atualizar agora
─────────────────────────────
Iniciar com o Windows     [ ]
─────────────────────────────
Sair
```

**Atalho global:** `Ctrl+Alt+H`.

## CLI

O projeto `HdrSwitcher.Cli` gera o executável `hdr`:

```text
hdr list                  Lista monitores e o estado do HDR
hdr on <nome>             Liga o HDR nos monitores que casarem com o nome
hdr off <nome>            Desliga o HDR
hdr toggle <nome>         Alterna o HDR
hdr all on|off|toggle     Aplica a todos os monitores com HDR
```

Exemplos:

```powershell
hdr list
hdr on ultragear
hdr off samsung
hdr all toggle
```

## Compilar e publicar

```powershell
# build normal
dotnet build HdrSwitcher.sln

# executável único, sem precisar do runtime instalado (~30 MB)
dotnet publish src/HdrSwitcher.App/HdrSwitcher.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o dist
```

O resultado é `dist/HdrSwitcher.exe`.

Para uma versão bem menor (exige o .NET 9 Desktop Runtime instalado), use `--self-contained false`.

## Como funciona

Para cada monitor ativo:

1. `QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS)` enumera os caminhos de vídeo ativos.
2. `DisplayConfigGetDeviceInfo(GET_TARGET_NAME)` retorna o nome amigável e o device path (usado como ID estável).
3. `DisplayConfigGetDeviceInfo(GET_ADVANCED_COLOR_INFO)` retorna os bits `advancedColorSupported` e `advancedColorEnabled`.
4. `DisplayConfigSetDeviceInfo(SET_ADVANCED_COLOR_STATE)` liga/desliga o HDR.

O app escuta `SystemEvents.DisplaySettingsChanged` para atualizar o ícone quando a configuração de vídeo muda, e reconstrói o menu a cada abertura — assim o estado nunca fica velho.

## Limitações

- Alternar HDR troca o modo de vídeo: a tela pisca/fica preta por 1–3 segundos. É comportamento do Windows.
- Alguns jogos só percebem o novo estado de HDR depois de serem reabertos.
- Monitores sem suporte retornam `ERROR_NOT_SUPPORTED` ao tentar ligar; o app simplesmente mantém o item desabilitado.
- Se outro app já usa `Ctrl+Alt+H`, o atalho não é registrado e o app avisa no primeiro balloon.

## Estrutura

```
src/HdrSwitcher.Core/     P/Invoke da DisplayConfig + MonitorService
src/HdrSwitcher.App/      App de bandeja (WinForms, tray-only)
src/HdrSwitcher.Cli/      CLI `hdr`
```
