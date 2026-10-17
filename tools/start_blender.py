import bpy, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'.tools/blender-mcp'))
import addon
addon.register()
server=addon.BlenderMCPServer(host='127.0.0.1',port=9876)
bpy.types.blendermcp_server=server
server.start()
