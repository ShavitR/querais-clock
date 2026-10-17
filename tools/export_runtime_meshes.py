"""Scene-isolated native export, including evaluated normals and animation pivots.
WPF's recommended mesh limits are respected without reducing model detail.
"""
import bpy,json,gzip
from pathlib import Path
from mathutils import Vector
ROOT=Path(globals().get('QUERAIS_PROJECT_ROOT',Path.cwd()));OUT=ROOT/'assets/collection'
def split_batch(g):
 # Keep native lettering as one vector-overlay placeholder. The loader replaces
 # its modeled glyphs, so it does not send that dense mesh to WPF.
 if g['name']=='DialText':return [g]
 chunks=[];remap={};new=None
 for start in range(0,len(g['indices']),3):
  triangle=g['indices'][start:start+3]
  if new is None or len(remap)+len(set(triangle)-remap.keys())>18000 or len(new['indices'])>=54000:
   new={k:v for k,v in g.items() if k not in {'positions','normals','uv','indices'}};new.update(positions=[],normals=[],uv=[],indices=[]);chunks.append(new);remap={}
  for i in triangle:
   if i not in remap:
    remap[i]=len(remap);new['positions'].extend(g['positions'][i*3:i*3+3]);new['normals'].extend(g['normals'][i*3:i*3+3]);new['uv'].extend(g['uv'][i*2:i*2+2])
   new['indices'].append(remap[i])
 return chunks

for key,name in [('atelier','Atelier · Analog'),('slate','Slate · Typography'),('relay','Relay · Split flap'),('signal','Signal · Luminous')]:
 s=bpy.data.scenes[name];win=bpy.context.window_manager.windows[0];win.scene=s;groups={}
 with bpy.context.temp_override(window=win,scene=s):
  bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
  for o in list(s.objects):
   if o.type not in {'MESH','FONT','CURVE'} or o.get('preview_only',False):continue
   ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();mat=o.data.materials[0].name;group=o.get('runtime_group',o.name)
   g=groups.setdefault((group,mat),dict(name=group,material=mat,positions=[],normals=[],uv=[],indices=[],lookup={}))
   if 'runtime_pivot' in o:g['pivot']=list(o['runtime_pivot']);g['ratio']=o.get('runtime_ratio',1)
   normal_matrix=o.matrix_world.to_3x3().inverted().transposed()
   for tri in me.loop_triangles:
    for li in tri.loops:
     v=o.matrix_world@me.vertices[me.loops[li].vertex_index].co;n=(normal_matrix@me.corner_normals[li].vector).normalized()
     uv=me.uv_layers.active.data[li].uv if me.uv_layers.active else Vector(((v.x+4)/8,(v.y+2)/4))
     # The reflective runtime shader uses normals, not object UV islands.
     # Removing irrelevant UV seams shares vertices without changing the surface.
     if mat in {'steel','polished','brass','shell','jewel'}:uv=Vector((n.x*.5+.5,n.y*.5+.5))
     pos=tuple(round(c,5) for c in v);normal=tuple(round(c,5) for c in n);tex=tuple(round(c,5) for c in uv);lookup=pos+normal+tex
     if lookup not in g['lookup']:g['lookup'][lookup]=len(g['positions'])//3;g['positions'].extend(pos);g['normals'].extend(normal);g['uv'].extend(tex)
     g['indices'].append(g['lookup'][lookup])
   ev.to_mesh_clear()
 parts=[]
 for g in groups.values():del g['lookup'];parts.extend(split_batch(g))
 payload=dict(key=key,width=4.7 if key=='atelier' else 7.3,height=4.5 if key=='atelier' else 3.7,parts=parts)
 (OUT/(key+'.mesh.json.gz')).write_bytes(gzip.compress(json.dumps(payload,separators=(',',':')).encode(),compresslevel=9))
 print('EXPORTED',key,len(parts),'native batches',sum(len(g['indices'])//3 for g in parts),'triangles; max vertices',max(len(g['positions'])//3 for g in parts if g['name']!='DialText'))
