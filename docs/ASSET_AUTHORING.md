# Asset authoring

Blender is optional for building and using Querais. Runtime meshes are included.

## Set up

Install Blender, Git and [uv](https://docs.astral.sh/uv/). In PowerShell 7:

```powershell
./tools/Setup-Blender.ps1
./tools/Start-Blender.ps1
./.tools/venv/Scripts/python.exe tools/mcp_call.py
```

Setup pins the upstream [MCP for Blender](https://github.com/ahujasid/blender-mcp) source and creates an isolated `.tools/venv`. It does not edit global AI-client settings. The launcher registers the addon in that Blender process, on `127.0.0.1:9876`. MCP server telemetry is disabled by the local client.

The last command performs an MCP handshake, lists tools and queries Blender's live scene. The addon needs Blender's GUI event loop; do not use `--background`.

## Edit

Open `assets/querais-collection.blend`. It contains four scenes, studio rigs and named assemblies with hand pivots. Windows' Bahnschrift and Georgia fonts are used; substitute available fonts on other operating systems. No font binaries are distributed.

To rebuild in a dedicated Blender instance:

```powershell
./.tools/venv/Scripts/python.exe tools/mcp_call.py tools/build_collection.py
./.tools/venv/Scripts/python.exe tools/mcp_call.py tools/render_collection.py
./.tools/venv/Scripts/python.exe tools/mcp_call.py tools/export_collection_glb.py
./.tools/venv/Scripts/python.exe tools/validate_assets.py
./.tools/venv/Scripts/python.exe tools/strip_render_metadata.py
```

The first script replaces that Blender process's scene; save unrelated work first. It creates the four scenes, detailed original construction, neutral-pose runtime meshes and organized editable source. `collection_details.py` supplies the case sections, exhibition movement, flap cartridges, optics and electronics. The next scripts render six real Cycles views, export isolated GLBs and validate the native contract.

Rendering modifies preview poses in memory and never saves them over the neutral source. GLB export reopens the neutral source. To export native meshes after hand editing, call `tools/export_runtime_meshes.py` through MCP while the neutral source is open.

The MCP client injects the checkout root. Calls are logged locally under ignored `artifacts/`.

## Runtime contract

Each `*.mesh.json.gz` contains a scene key, dimensions and parts with semantic names, material roles, positions, normals, UVs and triangle indices. Optional `pivot` and `ratio` fields describe the exhibition mechanism. Hands rotate around the clock center. Relay's `Flip_*` parts rotate around a hinge; Signal's `Seg_*` parts follow digit masks.

Large semantic assemblies may span multiple material batches with the same name. Each runtime batch stays within 18,000 vertices / 54,000 indices. The modeled `DialText` is an exception because the native loader replaces it with a crisp vector overlay. Model budgets are 350,000 triangles and 120 batches. The exporter removes irrelevant UV seams from reflective geometry without simplifying its shape.

The scene mechanisms and electronic assemblies are illustrative. They do not represent a working physical calibre or circuit. System time remains authoritative. The native renderer approximates studio reflections; it is not Cycles or a full PBR engine. See [design research](DESIGN_RESEARCH.md) for sources and tradeoffs.

Preserve names consumed by `SculptureView.cs`, or update loader and checks together. Live displays and crisp dial lettering use native textures on actual surfaces; modeled lettering remains in the source and GLBs.

Inspect both Cycles renders and the running app: native lighting/transparency differs from PBR. Original Querais assets use the root MIT license. External tools and fonts retain their own licenses.
