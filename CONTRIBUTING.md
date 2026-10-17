# Contributing to Querais

Small, well-explained improvements are welcome.

## Start locally

Install the .NET 10 SDK on Windows, fork the repo, clone your fork, then run:

```powershell
dotnet run --project src/Querais -c Release
```

Runtime meshes are included. Blender is only needed for model work; see [asset authoring](docs/ASSET_AUTHORING.md).

## Before a pull request

- Explain the problem and resulting behavior. Include screenshots for visual changes.
- Run `dotnet build src/Querais -c Release`.
- Use `--verify` for controls, preferences and timekeeping changes. It opens and resizes the app, so run it when that will not interrupt your work.
- Check both themes, a small window, keyboard focus and reduced motion. Test the interaction manually; the harness does not replace pointer, accessibility or DPI checks.
- Keep changes focused. Discuss large features in an issue first.

Do not commit preferences, alarm data, raw MCP logs, virtual environments or build output.

## Where to look

- `MainWindow.xaml.cs`: shell, controls, time tools and tray.
- `SculptureView.cs`: native 3D collection, materials and interaction.
- `ClockSurface.cs`: flat faces.
- `Preferences.cs`: local storage and legacy import.
- `Verification.cs`: behavior checks and captures.

All application files are in `src/Querais`. Models are in `assets/collection`.

## Bug reports

Include Windows/app version, clock/finish, display scale, reproduction steps and expected/actual behavior. A focused screenshot helps with visual issues.

## License

Contributions use the repository's MIT license. Submit only code and assets you have the right to share, and preserve third-party notices.
