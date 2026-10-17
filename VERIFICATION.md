# Verification

The last full desktop run of the detailed collection passed **37 native checks**, plus validation of all four compressed assets. The final reduced-motion adjustment was then verified with **seven windowless checks**; screen testing was stopped at the user's request. Results are in [native results](docs/verification-results.json), [final windowless results](docs/headless-check-results.json) and [asset validation](docs/asset-validation.json), with runtime screenshots in [docs/screenshots](docs/screenshots).

## Covered behavior

The in-process harness invokes WPF handlers and changes native window state. It checks selectors, topmost/compact modes, duration input above 24 hours, invalid timer input, pause/resume, completion across views, stopwatch laps/pause, one-shot alarms, hidden/minimized rendering suspension and preference persistence.

Object checks cover inspection, rear view, glass, orbit, light direction/intensity, crown sweep, turntable movement, multi-revolution angle wrapping, mechanical flap plates, reduced motion, segment masks and the flat fallback.

The detailed collection adds per-model triangle/batch budgets, independent exhibition movement, reduced-motion oscillator rest, real window-size assertions and a raster check for visible luminous pixels. The latter catches optical-well occlusion that material-reference counts alone missed. Asset validation checks finite data, normal lengths, index bounds, animation pivots, required assemblies and WPF batch limits.

Captures cover 1180x890, 640x650 and 520x530 outer windows, both themes, the collection, popovers, reverse sides and inspection. Main-window captures render the native visual tree at 144 DPI, including Viewport3D, and exclude OS chrome. This checks scaled rendering, not OS-wide multi-monitor DPI behavior. They are separate from Cycles previews in assets/collection. The fixture restores all user preferences and original window dimensions/state.

## Run locally

The final renderer checks create no Window, tray icon or input simulation:

```powershell
dotnet run --project tests/Querais.RendererChecks -c Release
```

They exercise real asset loading, independent motion, reduced motion and model budgets, writing `artifacts/headless-check-results.json`. They do not establish visual or pointer/keyboard qualification.

The desktop harness below deliberately opens, moves and resizes windows. Run it only when that will not interrupt your work.

```powershell
dotnet run --project src/Querais -c Release -- --verify
```

This opens, resizes and briefly minimizes the app. A checkout writes captures/results to ignored artifacts/qa; a portable copy uses its LocalAppData verification folder. Inspect results.json for failed checks or errors.

Windows CI compiles and packages the app; it does not run the desktop harness on the hosted runner.

## Manual qualification

Handler-level checks do not establish comprehensive pointer/keyboard, tray, native snapping, multi-monitor, Narrator or alternate Windows DPI qualification. These remain useful community tests.

Native WPF materials approximate Cycles source materials. Alarms require a running process and do not wake Windows. Timer/stopwatch sessions are not restored after exiting.
