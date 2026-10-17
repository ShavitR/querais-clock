import bpy, math
from pathlib import Path
from mathutils import Matrix
ROOT=Path(globals().get('QUERAIS_PROJECT_ROOT', Path.cwd()))
glass=bpy.data.materials['glass'].node_tree.nodes.get('Principled BSDF');glass.inputs['Base Color'].default_value=(.94,.97,1,1);glass.inputs['Roughness'].default_value=.015;glass.inputs['IOR'].default_value=1.28
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/querais-collection.blend'))
for key,name in [('atelier','Atelier · Analog'),('slate','Slate · Typography'),('relay','Relay · Split flap'),('signal','Signal · Luminous')]:
 s=bpy.data.scenes[name];bpy.context.window_manager.windows[0].scene=s
 if key=='atelier':
  for o in s.objects:
   g=o.get('runtime_group','')
   if g.startswith('Hand_'):o.matrix_world=Matrix.Rotation(math.radians({'Hand_Hour':-305,'Hand_Minute':-60,'Hand_Second':-228}[g]),4,'Z')@o.matrix_world
 if key=='signal':
  off=bpy.data.materials.new('Preview unlit');off.diffuse_color=(.018,.025,.03,1);off.use_nodes=True;off.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.018,.025,.03,1)
  masks=[0x06,0x3f,0x3f,0x7f,0x6d,0x6f]
  for o in s.objects:
   n=o.get('runtime_group','')
   if n.startswith('Seg_'):
    _,i,j=n.split('_')
    if not masks[int(i)]&(1<<int(j)):o.data.materials.clear();o.data.materials.append(off)
 s.render.filepath=str(ROOT/'assets/collection'/(key+'-preview.png'));bpy.ops.render.render(write_still=True,scene=s.name)
print('Inspected glass improved and all four Cycles previews refined; editable zero-pose source retained.')
