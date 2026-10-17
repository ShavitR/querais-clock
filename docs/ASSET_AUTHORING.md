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
./.tools/venv/Scripts/python.exe tools/mcp_call.py tools/refine_collection.py
./.tools/venv/Scripts/python.exe tools/mcp_call.py tools/export_collection_glb.py
./.tools/venv/Scripts/python.exe tools/mcp_call.py tools/organize_collection_source.py
```

The first script replaces that Blender process's scene; save unrelated work first. It creates original models, neutral-pose runtime meshes, GLBs, editable source and actual Cycles previews. Later scripts refine previews, isolate GLB scenes and organize source pivots.

The MCP client injects the checkout root. Calls are logged locally under ignored `artifacts/`.

## Runtime contract

Each `*.mesh.json.gz` contains a scene key, dimensions and parts with semantic names, material roles, positions, normals, UVs and triangle indices. Hands rotate around the clock center. Relay's `Flip_*` parts rotate around a hinge; Signal's `Seg_*` parts follow digit masks.

Preserve names consumed by `SculptureView.cs`, or update loader and checks together. Live displays and crisp dial lettering use native textures on actual surfaces; modeled lettering remains in the source and GLBs.

Inspect both Cycles renders and the running app: native lighting/transparency differs from PBR. Original Querais assets use the root MIT license. External tools and fonts retain their own licenses.
