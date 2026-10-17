# Third-party notices

Querais application code, original models, source artwork and renders use the root MIT license.

## .NET and Windows Desktop

The source uses Microsoft's .NET 10 and WPF/Windows Forms. Self-contained downloads redistribute the corresponding runtime packs. `tools/Build-Release.ps1` copies their actual license and third-party notices into the download's `licenses/` folder. The checked-in notices record the initial 10.0.11 runtime release.

- [.NET runtime](https://github.com/dotnet/runtime)
- [WPF](https://github.com/dotnet/wpf)
- [Windows Forms](https://github.com/dotnet/winforms)

## Optional asset authoring

[MCP for Blender](https://github.com/ahujasid/blender-mcp), by Siddharth Ahuja and contributors, is MIT licensed. Its original license is retained in `licenses/MCP-FOR-BLENDER-LICENSE.txt`. The setup downloads pinned commit `7a0373ec9199183cb460068c4f96aed9c579fb4f`; integration code and virtual environments are not vendored in this repository or the app download. Its dependencies retain their upstream licenses.

[Blender](https://www.blender.org/) is separately installed authoring software and is not redistributed with Querais. Original clock assets are distributed under Querais's MIT license.

## System fonts and icons

The app uses fonts and icon fonts supplied by Windows, including Segoe UI, Bahnschrift, Georgia and Segoe Fluent Icons. Those remain Microsoft's property and are not licensed by Querais. No font binaries are bundled. The Blender source references installed fonts; rendered clock artwork and modeled lettering are included as part of the clock designs.
