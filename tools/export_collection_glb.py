import bpy
from pathlib import Path
ROOT=Path(globals().get('QUERAIS_PROJECT_ROOT', Path.cwd()))
# Save sources already contain refined materials and all zero-pose editing data.
for key,name in [('atelier','Atelier · Analog'),('slate','Slate · Typography'),('relay','Relay · Split flap'),('signal','Signal · Luminous')]:
 s=bpy.data.scenes[name];window=bpy.context.window_manager.windows[0];window.scene=s
 with bpy.context.temp_override(window=window,scene=s):
  bpy.ops.object.select_all(action='DESELECT')
  for o in list(s.objects):
   if o.type=='FONT':
    o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o.select_set(False)
  for o in s.objects:
   if o.type=='MESH' and not o.get('runtime_group','').startswith('Flip_'):o.select_set(True)
  bpy.ops.export_scene.gltf(filepath=str(ROOT/'assets/collection'/(key+'.glb')),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_draco_mesh_compression_enable=True)
print('Four scene-isolated GLBs exported with refined materials. Editable source preserved.')
