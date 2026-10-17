# Verification

The initial community release passed **28 automated behavior checks**. Baseline results are in [docs/verification-results.json](docs/verification-results.json), with runtime screenshots in [docs/screenshots](docs/screenshots).

## Covered behavior

The in-process harness invokes WPF handlers and changes native window state. It checks selectors, topmost/compact modes, duration input above 24 hours, invalid timer input, pause/resume, completion across views, stopwatch laps/pause, one-shot alarms, hidden/minimized rendering suspension and preference persistence.

Object checks cover inspection, rear view, glass, orbit, light direction/intensity, crown sweep, turntable movement, multi-revolution angle wrapping, mechanical flap plates, reduced motion, segment masks and the flat fallback.

Captures cover 1180x890, 640x650 and 520x530 outer windows, both themes, the collection, popovers and inspection. They render the native visual tree, including Viewport3D, and exclude OS chrome. They are separate from Cycles previews in assets/collection. The fixture restores all user preferences and original window dimensions.

## Run locally

```powershell
dotnet run --project src/Querais -c Release -- --verify
```

This opens, resizes and briefly minimizes the app. A checkout writes captures/results to ignored artifacts/qa; a portable copy uses its LocalAppData verification folder. Inspect results.json for failed checks or errors.

Windows CI compiles and packages the app; it does not run the desktop harness on the hosted runner.

## Manual qualification

Handler-level checks do not establish comprehensive pointer/keyboard, tray, native snapping, multi-monitor, Narrator or alternate Windows DPI qualification. These remain useful community tests.

Native WPF materials approximate Cycles source materials. Alarms require a running process and do not wake Windows. Timer/stopwatch sessions are not restored after exiting.
