import bpy
from pathlib import Path
ROOT=Path(globals().get('QUERAIS_PROJECT_ROOT', Path.cwd()));bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/querais-collection.blend'))
# Named assembly pivots make the source and portable exports practical to edit.
for s in list(bpy.data.scenes):
 if not any(o.type in {'MESH','FONT'} for o in s.objects):continue
 groups={}
 for o in list(s.objects):
  if o.type not in {'MESH','FONT'}:continue
  name=o.get('runtime_group','Preview')
  if name not in groups:
   pivot=bpy.data.objects.new('Assembly / '+name,None);s.collection.objects.link(pivot);pivot.empty_display_size=.15
   if name=='Crown':pivot.location=(2.12,0,-.03)
   groups[name]=pivot
  matrix=o.matrix_world.copy();o.parent=groups[name];o.matrix_world=matrix
bpy.context.window_manager.windows[0].scene=bpy.data.scenes['Atelier · Analog']
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/querais-collection.blend'))
print('Editable collection organized into named assemblies with central hand pivots; source saved.')
