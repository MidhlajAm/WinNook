<div align="center">

<img src="WinNook%20Logo/winnook-icon.svg" width="120" alt="WinNook" />

# WinNook

**A Dynamic Island for Windows.**

A notch that lives at the top of your screen, reacts to what your PC is doing,
and expands into a dashboard when you need it.

Built with C# and WPF. No Electron, no browser, no background web server.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](https://www.microsoft.com/windows)
[![UI](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-5C2D91?style=flat-square)](https://github.com/lepoco/wpfui)

</div>

---

## What it is

WinNook puts a small black notch at the top of your desktop. Most of the time it just sits
there. When something happens — you change the volume, plug in the charger, start a track —
it briefly grows, tells you, and shrinks back. Click it and it opens into a dashboard of
widgets. Drag a file onto it and the file is kept on a shelf you can pull from later.

The interaction model is borrowed from Apple's Dynamic Island. The implementation is native
Windows: WPF for the UI, Win32 and WASAPI for the system hooks, SQLite for anything that has
to survive a restart.

## Highlights

**Motion that behaves like an object, not a slideshow.**
Every animated value — width, height, corner radius, colour — is a damped spring, integrated
analytically against the real frame clock. Springs are interruptible by construction: grab the
notch mid-expand and reverse it and the motion stays continuous, because a re-target keeps both
position *and* velocity. Parameters follow Apple's designer-facing model (damping ratio and
response) rather than raw stiffness values, and overshoot is reserved for states that carry
momentum. There are no fixed durations anywhere in the notch.

**It reacts to the machine, not just to clicks.**
A trigger engine listens to volume changes, power state, the foreground window, and the system
media session, then arbitrates between them by priority so two events never fight over the notch.

**Widgets are a registry, not a hardcoded list.**
Adding a widget is one descriptor class plus one `UserControl`. It then appears in the dashboard,
gets a toggle and reorder controls in the settings window, and persists its own state — with no
edits to the dashboard or the Control Center.

**It stays out of the way.**
Widgets with timers only run while the dashboard is actually visible. The notch hides itself
when a chosen app comes to the foreground. Settings writes are debounced and atomic.

## Widgets

| Widget | What it does |
| --- | --- |
| **Music** | Now playing, album art, transport controls. Shows a live spectrograph in the collapsed notch. |
| **Calendar** | Today's date. |
| **Volume** | Click to mute, scroll the tile to change level. Tracks hardware keys and the Windows mixer. |
| **Timer** | Countdown and Pomodoro presets. Keeps running while the notch is closed. |
| **System Stats** | Live CPU and memory load. |
| **Notes** | A scratchpad that saves itself as you type. |
| **Clipboard** | The last 25 things you copied, one click to paste back. |
| **Screenshot** | Captures every monitor, hiding the notch first. |
| **Quick Launch** | Up to six pinned apps, launched from the notch. |

Each one can be switched off and reordered.

## The Shelf

Drag files, folders, or selected text onto the notch and they land on the Shelf. Files are copied
into managed storage; folders are kept by reference, so pinning one never duplicates gigabytes.
Drag items back out into any application. Storage is capped (2 GB by default, configurable) and
new drops are refused with an on-screen reason rather than quietly filling the disk.

## Notifications

| Trigger | Shown as |
| --- | --- |
| Volume change | Level and speaker state |
| Charger connected or removed | Battery percentage with a matching icon |
| Battery at or below 20% | Low-battery warning |
| Timer finished | Completion notice |
| Screenshot taken | Confirmation with destination |

## Customisation

The Control Center covers appearance, motion, widgets, apps, and behaviour:

- **Appearance** — two style presets, **Nook** (default: fused to the screen edge, inverted corner
  fillets blending it into the bezel) and **Dynamic Island** (a pill floating clear of the edge, all
  four corners rounded). Both are just starting points for the sliders: idle, hover, and expanded
  dimensions, corner radius per state, top offset, and fillet radius.
- **Effects** — animation response, shadow blur, opacity, and depth.
- **Activity** — widget toggles and ordering.
- **Apps** — pick which installed apps to pin to Quick Launch, and which should hide the notch
  when focused.
- **Options** — run at startup, default notch label, dismiss behaviour, screenshot destination,
  Shelf storage budget.

Everything previews live while you drag a slider.

## Install

Grab `WinNook-Setup.exe` from [Releases](https://github.com/MidhlajAm/WinNook/releases) and run it.

**Requirements:** Windows 10 1809 (build 17763) or newer, 64-bit. Nothing else — the installer
carries its own .NET runtime, so there is no prerequisite to install first.

It installs per-user, so there is no UAC prompt. Setup offers a desktop shortcut and a
run-at-sign-in option, both optional and both changeable later from the Control Center.

> Windows SmartScreen will warn that the publisher is unknown, because the installer is not code
> signed yet. Choose **More info → Run anyway**.

Uninstall from **Settings → Apps**, or the Start Menu shortcut. It asks whether to keep your
settings, shelf, notes, and clipboard history, and keeps them by default.

## Using it

WinNook lives in the tray. Double-click the tray icon, or right-click it and choose
**Control Center**, to open settings. Only one instance runs at a time; launching it again brings
the existing Control Center forward.

| Gesture | Result |
| --- | --- |
| Hover the notch | Slight grow |
| Click | Expand to the widget dashboard |
| Click again, or click away | Collapse (which one is a setting) |
| Drag a file, folder, or text onto it | Added to the Shelf |
| Drag an item out of the Shelf | Drops into any app |

## Building from source

**Requirements:** the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/MidhlajAm/WinNook.git
cd WinNook
dotnet build "Project Nook.csproj"
dotnet run --project "Project Nook.csproj"
```

> Pass the project path explicitly. The solution pins concrete platforms and writes to a
> different output folder, which makes it easy to run a stale binary by mistake.

To produce an installer, publish self-contained and compile the
[Inno Setup](https://jrsoftware.org/isdl.php) script:

```bash
dotnet publish "Project Nook.csproj" -c Release -r win-x64 --self-contained true -p:Platform=x64
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer\WinNook.iss
```

The result lands in `Installer/Output/`.

## Architecture

```
App.xaml.cs              Composition root: DI container, single-instance mutex, tray icon
  │
  ├── UI/IslandWindow    The notch itself. Transparent, topmost, click-through where empty
  │     └── Animation/   Analytic damped springs driven by CompositionTarget.Rendering
  │
  ├── Services/
  │     ├── SystemEvents/  WASAPI volume, media session, power, foreground window, clipboard
  │     ├── TriggerEngine  Priority arbitration and auto-collapse for notifications
  │     └── Storage        Shelf files, SQLite persistence, screen capture
  │
  ├── Widgets/           Registry: descriptors, slots, ordering, enable state
  │
  └── MainWindow         Control Center
```

**Stack:** .NET 8 · WPF · [WPF-UI](https://github.com/lepoco/wpfui) · CommunityToolkit.Mvvm ·
NAudio (WASAPI) · sqlite-net · H.NotifyIcon

**Where things are kept:**

| Path | Contents |
| --- | --- |
| `%AppData%\WinNook\settings.json` | Preferences |
| `%LocalAppData%\WinNook\ShelfData\` | Shelf file copies |
| `%LocalAppData%\WinNook\shelf.db3` | Shelf index |
| `%LocalAppData%\WinNook\widgets.db3` | Notes and clipboard history |
| `%LocalAppData%\WinNook\logs\` | Error log |

## Adding a widget

```csharp
public class WeatherWidgetDescriptor : IWidgetDescriptor
{
    public string Id => "weather";                  // stable key, never change once shipped
    public string DisplayName => "Weather";
    public string Description => "Current conditions.";
    public SymbolRegular Icon => SymbolRegular.WeatherSunny24;
    public WidgetSize Size => WidgetSize.Tile;      // 64x64 tile, or Wide
    public bool EnabledByDefault => false;

    public FrameworkElement CreateView() => new WeatherWidgetControl();
}
```

Register the descriptor and its view model in `App.ConfigureServices`. That is the whole
integration — the dashboard, the settings list, ordering, and persistence pick it up.

If the widget holds a timer or a poll, implement `IWidgetLifecycle` and it will only run while
the dashboard is on screen.

## Privacy

Nothing leaves your machine. There is no telemetry, no account, and no network call in the app.

The clipboard widget stores text locally in SQLite and honours the standard opt-out formats
(`Clipboard Viewer Ignore` and the Windows clipboard-history flags), so content from password
managers is not recorded. It is off by default, and clearing the history deletes it.

## Roadmap

- [ ] Draw the notch and its fillets as a single geometry
- [ ] Multi-monitor placement
- [ ] Weather widget
- [ ] Calendar event integration
- [ ] Reduced-motion support
- [ ] Automated tests

## Contributing

Issues and pull requests are welcome. If you are adding a widget, follow the descriptor pattern
above rather than editing the dashboard directly.

## Author

**Midhlaj AM** — [GitHub](https://github.com/MidhlajAm) · [LinkedIn](https://www.linkedin.com/in/midhlaj-am/)

If WinNook is useful to you, [buy me a coffee](https://ko-fi.com/T8Q124XOE1).

## Brand

The mark is four panes with the island cutting across them: the panes are the desktop, the dark
band is WinNook sitting over it. The narrow highlight pane breaks the 2x2 grid deliberately, so it
does not read as the Windows logo.

<img src="WinNook%20Logo/winnook-icon.svg" width="72" alt="master" />&nbsp;&nbsp;&nbsp;
<img src="WinNook%20Logo/winnook-icon-small.svg" width="72" alt="small" />

| Asset | Use |
| --- | --- |
| `winnook-icon.svg` | Master. 40px and above, on the desktop or a neutral surface. |
| `winnook-icon-small.svg` | 32px and below. Everything sits on a blue tile so the dark island always has contrast. |
| `winnook-icon-mono.svg` | `currentColor`, for tray and monochrome contexts. |
| `winnook.ico` | 16 through 256 packed. App icon, window icon, tray. |

Palette: accent `#0078D4`, highlight `#4CC2FF`, island `#16181D` — never pure black, or the band
disappears into dark window chrome. Full guidance in
[`WinNook Logo/README.md`](WinNook%20Logo/README.md).

## Acknowledgements

Interaction model inspired by Apple's Dynamic Island. Motion design follows the principles in
Apple's *Designing Fluid Interfaces*. Not affiliated with or endorsed by Apple.

## License

Not yet licensed. Until a license is added, all rights are reserved.
