"""Render real Cycles previews from the editable source; never used by the app."""
import bpy,math,json,traceback
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(globals().get('QUERAIS_PROJECT_ROOT',Path.cwd()))
status=ROOT/'artifacts/render-status.json';status.parent.mkdir(exist_ok=True)
jobs=globals().get('QUERAIS_RENDER_JOBS',['atelier','slate','relay','signal','atelier-rear','relay-detail'])
names={'atelier':'Atelier · Analog','slate':'Slate · Typography','relay':'Relay · Split flap','signal':'Signal · Luminous'}
done=[]
try:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/querais-collection.blend'))
 for job in jobs:
  key=job.split('-')[0];s=bpy.data.scenes[names[key]];bpy.context.window_manager.windows[0].scene=s
  # Preview pose goes on assembly pivots, preserving each hand's layers.
  if key=='atelier':
   for o in s.objects:
    if o.type=='EMPTY' and o.name.startswith('Assembly / Hand_'):
     g=o.name.split(' / ')[1];o.rotation_euler.z=math.radians({'Hand_Hour':-305,'Hand_Minute':-60,'Hand_Second':-228}[g])
  if key=='signal':
   off=bpy.data.materials.get('Unlit preview') or bpy.data.materials.new('Unlit preview');off.use_nodes=True;p=next(n for n in off.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.009,.02,.027,1);p.inputs['Roughness'].default_value=.3
   masks=[0x06,0x3f,0x3f,0x7f,0x6d,0x6f]
   for o in s.objects:
    g=o.get('runtime_group','')
    if g.startswith('Seg_'):
     _,i,j=g.split('_')
     if not masks[int(i)]&(1<<int(j)):o.data.materials.clear();o.data.materials.append(off)
  cam=s.camera
  if job.endswith('rear'):
   cam.rotation_euler=(math.radians(7),math.radians(164),0);cam.location=cam.rotation_euler.to_quaternion()@Vector((0,0,12));cam.data.ortho_scale=5.15
  if job.endswith('detail'):
   for o in s.objects:
    if o.type=='EMPTY' and o.name.startswith('Assembly / RearCover'):o.location.z-=1.0
   cam.rotation_euler=(math.radians(18),math.radians(148),0);cam.location=cam.rotation_euler.to_quaternion()@Vector((0,0,12));cam.data.ortho_scale=8.9
  if job.endswith('rear') or job.endswith('detail'):
   # Compose a rear product photograph with the same studio around that view.
   # Leaving all softboxes in front silhouettes the exhibition mechanism.
   rotation=Matrix.Rotation(math.pi,3,'Y')
   for light in [o for o in s.objects if o.type=='LIGHT']:
    light.location=rotation@light.location;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
  s.render.filepath=str(ROOT/'assets/collection'/(job+'-preview.png'));bpy.ops.render.render(write_still=True,scene=s.name)
  done.append(job);status.write_text(json.dumps({'done':done,'total':len(jobs)}))
 status.write_text(json.dumps({'done':done,'complete':True}))
except Exception:
 status.write_text(json.dumps({'done':done,'error':traceback.format_exc()}));raise
