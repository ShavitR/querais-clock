import bpy, math, json, gzip
from pathlib import Path
from mathutils import Vector
ROOT=Path(globals().get('QUERAIS_PROJECT_ROOT', Path.cwd()));OUT=ROOT/'assets/collection';OUT.mkdir(exist_ok=True)
# This file owns the collection source. Keep unrelated Blender files separate.
# Four original objects in separate editable scenes, one source file.
for obj in list(bpy.data.objects):bpy.data.objects.remove(obj,do_unlink=True)
staging=bpy.data.scenes.new('Querais build staging');bpy.context.window.scene=staging
for old in list(bpy.data.scenes):
 if old!=staging:bpy.data.scenes.remove(old)
for old in list(bpy.data.materials):bpy.data.materials.remove(old)
materials={}
def material(name,c,metal=0,rough=.3,emission=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emission:p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=emission
 materials[name]=m;return m
material('shell',(.055,.065,.075),.7,.27);material('steel',(.44,.48,.52),.9,.23);material('polished',(.76,.80,.83),.98,.12);material('dial',(.79,.78,.73),.12,.32);material('ink',(.012,.019,.023),.1,.32);material('lume',(.85,.82,.70),.12,.29);material('accent',(.92,.23,.085),.35,.22);material('rubber',(.013,.018,.023),0,.57);material('glass',(.10,.14,.17),.05,.07);material('screen',(.008,.013,.018),.05,.18);material('emission',(.9,.37,.13),.1,.25,2);material('lettering',(.62,.66,.66),.5,.3)
p=next(n for n in materials['glass'].node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Transmission Weight'].default_value=1;p.inputs['IOR'].default_value=1.35
font=bpy.data.fonts.load('C:/Windows/Fonts/bahnschrift.ttf');serif=bpy.data.fonts.load('C:/Windows/Fonts/georgia.ttf')
scenes=[];current=None

def scene(name):
 global current
 s=bpy.data.scenes.new(name);bpy.context.window.scene=s;current=s;scenes.append(s);return s

def finish(o,name,mat,group=None,bevel=0):
 o.name=name;o.data.materials.append(materials[mat]);o['runtime_group']=group or name
 if bevel:
  b=o.modifiers.new('Precision edge radius','BEVEL');b.width=bevel;b.segments=4;o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
 return o

def box(name,loc,size,mat,group=None,bevel=.04):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,mat,group,bevel)

def cyl(name,r,depth,loc,mat,group=None,verts=96):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc);return finish(bpy.context.object,name,mat,group,.008)

def ring(name,profile,mat,group=None,N=192):
 vs=[(r*math.cos(i*math.tau/N),r*math.sin(i*math.tau/N),z) for r,z in profile for i in range(N)];fs=[]
 for j in range(len(profile)-1):
  for i in range(N):a=j*N+i;b=j*N+(i+1)%N;fs.append((a,b,b+N,a+N))
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,mat,group)
 for f in me.polygons:f.use_smooth=True
 return o

def text(name,txt,loc,size,mat='ink',group='DialText',fontface=None,preview=False):
 bpy.ops.object.text_add(location=loc);o=bpy.context.object;o.name=name;o.data.body=txt;o.data.font=fontface or font;o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.size=size;o.data.extrude=.0008;o.data.materials.append(materials[mat]);o['runtime_group']=group;o['preview_only']=preview;return o

def quad(name,x,y,z,w,h,mat='screen',group=None):
 me=bpy.data.meshes.new(name);me.from_pydata([(x-w/2,y-h/2,z),(x+w/2,y-h/2,z),(x+w/2,y+h/2,z),(x-w/2,y+h/2,z)],[],[(0,1,2,3)]);me.update();uv=me.uv_layers.new()
 for i,co in enumerate([(0,1),(1,1),(1,0),(0,0)]):uv.data[i].uv=co
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return finish(o,name,mat,group)

def screw(name,x,y,z,group):
 cyl(name,.048,.02,(x,y,z),'polished',group,32);box(name+' slot',(x,y,z+.014),(.058,.012,.008),'ink',group,.002)

def hand(name,length,width,z,mat):
 coords=[(-width*.44,-.22),(width*.44,-.22),(width*.52,length*.16),(width*.28,length-.06),(0,length+.025),(-width*.28,length-.06),(-width*.52,length*.16)]
 n=len(coords);vs=[(x,y,z+dz) for dz in [-.018,.018] for x,y in coords];fs=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,mat,name,.006);return o

s=scene('Atelier · Analog')
cyl('Caseback',1.96,.14,(0,0,-.31),'shell','Case');cyl('Rear medallion',1.58,.018,(0,0,-.39),'steel','Case')
# Rear engraving, correctly facing away from the dial.
for txt,y,size in [('Q U E R A I S',.22,.17),('ATELIER  /  01',-.12,.10),('PRECISION IN EVERY MOMENT',-.32,.055)]:
 o=text('Caseback engraving',txt,(0,y,-.404),size,'ink','Case');o.rotation_euler.y=math.pi
ring('Barrel',[(1.82,-.30),(1.97,-.25),(2.025,-.17),(2.025,.08),(1.98,.13)],'steel','Case')
for i in range(96):
 a=i*math.tau/96;o=box('Barrel flute',(2.018*math.sin(a),2.018*math.cos(a),-.075),(.018,.026,.20),'shell','Fluting',.004);o.rotation_euler.z=-a
ring('Outer chamfer',[(1.99,.10),(2.04,.13),(2.048,.20),(2.02,.24),(1.97,.26)],'polished','Bezel')
ring('Brushed bezel',[(1.77,.15),(1.80,.22),(1.86,.26),(1.97,.26)],'steel','Bezel')
ring('Inner rehaut',[(1.735,.075),(1.76,.145),(1.80,.205)],'polished','Bezel')
cyl('Recessed face',1.756,.045,(0,0,.055),'dial','Dial')
for r in [1.245,1.252,1.259,1.266,1.273,1.280,1.287,1.294]:ring('Engine turned rings',[(r,.080),(r+.0014,.080)],'lettering','DialDetail',144)
ring('Chapter track',[(1.555,.082),(1.558,.082)],'lettering','DialDetail')
for i in range(60):
 a=i*math.tau/60;r=1.65;major=i%5==0;o=box('Applied index',(r*math.sin(a),r*math.cos(a),.10),(.042 if major else .011,.14 if major else .047,.027 if major else .008),'polished' if major else 'ink','Indices',.006 if major else .001);o.rotation_euler.z=-a
 if major:
  o=box('Index inlay',(r*math.sin(a),r*math.cos(a),.119),(.015,.089,.004),'ink','Indices',.002);o.rotation_euler.z=-a
for i in range(1,13):
 a=i*math.tau/12;text('Numeral '+str(i),str(i),(1.40*math.sin(a),1.40*math.cos(a),.081),.15,'ink','DialText')
text('Brand','Q U E R A I S',(0,.65,.082),.12);text('Atelier','A T E L I E R',(0,.49,.082),.055)
text('Bottom lettering','PRECISION  /  LOCAL TIME',(0,-.72,.082),.047)
box('Date surround',(0,-.47,.088),(.34,.19,.018),'polished','DateSurround',.018);quad('Date_Display',0,-.47,.101,.30,.15,'dial')
text('Preview date','17',(0,-.47,.105),.13,preview=True)
h=hand('Hand_Hour',.97,.105,.17,'ink');m=hand('Hand_Minute',1.40,.07,.23,'ink');ss=hand('Hand_Second',1.54,.017,.29,'accent')
# Raised lume spines, assigned to their hand groups so they animate together.
box('Hour lume',(0,.50,.195),(.023,.69,.006),'lume','Hand_Hour',.006);box('Minute lume',(0,.69,.255),(.014,1.12,.006),'lume','Hand_Minute',.004)
ring('Seconds counterweight',[(.075,.29),(.088,.29)],'accent','Hand_Second',48).location.y=-.29
cyl('Floating spindle',.09,.065,(0,0,.30),'polished','Spindle');cyl('Enamel jewel',.036,.014,(0,0,.34),'accent','Spindle')
for a in [math.pi/4,3*math.pi/4,5*math.pi/4,7*math.pi/4]:screw('Bezel fastener',1.91*math.sin(a),1.91*math.cos(a),.268,'BezelScrews')
crown=cyl('Crown',.18,.28,(2.12,0,-.03),'steel','Crown',64);crown.rotation_euler.y=math.pi/2
for i in range(40):
 a=i*math.tau/40;o=box('Crown knurl',(2.13,.176*math.sin(a),-.03+.176*math.cos(a)),(.23,.017,.016),'polished','Crown',.003);o.rotation_euler.x=-a
cyl('Crystal',1.79,.018,(0,0,.375),'glass','Glass')
# Preview pose is applied only when rendering; exports remain zeroed.

# Digital clock 1: a cantilevered glass and aluminum typographic object.
s=scene('Slate · Typography')
box('Slate chassis',(0,0,-.18),(6.4,3.10,.52),'steel','Case',.22)
box('Slate graphite insert',(0,0,.105),(6.16,2.87,.12),'rubber','Bezel',.17)
box('Slate display recess',(0,.09,.17),(5.94,2.45,.035),'screen','ScreenBack',.13)
quad('Display_Main',0,.13,.195,5.65,2.05)
text('Preview time','10:08',(0,.20,.20),1.68,'lume','Preview',serif,True)
text('Slate brand','Q U E R A I S',(0,-1.27,.18),.10,'lettering','Brand')
for x in [-2.3,2.3]:
 o=box('Cantilever foot',(x,-1.57,-.36),(.74,.18,1.0),'shell','Stand',.05);o.rotation_euler.x=math.radians(-12)
for x in [-3.13,3.13]:
 for y in [-.60,0,.60]:box('Side key',(x,y,-.16),(.10,.29,.10),'polished','Keys',.024)
quad('Glass',0,.05,.225,6.0,2.6,'glass','Glass')

# Digital clock 2: a true mechanism, four independent split flaps.
s=scene('Relay · Split flap')
box('Relay housing',(0,0,-.25),(7.0,3.20,.70),'shell','Case',.22)
box('Relay front lip',(0,0,.11),(6.84,3.05,.16),'steel','Bezel',.16)
box('Mechanism recess',(0,.09,.207),(6.57,2.58,.04),'rubber','ScreenBack',.10)
for i in range(4):
 x=(i-1.5)*1.48+(0.10 if i>=2 else -.10)
 for half,y in [('Upper',.69),('Lower',-.51)]:
  box('Flap slab',(x,y,.245),(1.34,1.16,.055),'screen','FlapBody',.035)
  quad(f'Flap_{i}_{half}',x,y,.280,1.31,1.13,'screen')
 # This is a separate real 3D plate with its pivot at the seam.
 o=quad(f'Flip_{i}',x,.69,.287,1.31,1.13,'screen');o.hide_render=True
 for dx in [-.65,.65]:
  o=cyl('Pivot hinge',.045,.12,(x+dx,.09,.30),'polished','Hinges',24);o.rotation_euler.y=math.pi/2
 text('Preview digit','1008'[i],(x,.13,.288),2.05,'lume','Preview',preview=True)
text('Relay brand','Q U E R A I S',(0,-1.37,.215),.09,'lettering','Brand')
for x in [-2.7,2.7]:box('Relay foot',(x,-1.68,-.27),(.85,.16,.62),'rubber','Stand',.04)
for x in [-3.25,3.25]:
 for y in [-1.30,1.30]:screw('Housing screw',x,y,.21,'Fasteners')

# Digital clock 3: extruded luminous seven-segment geometry under smoked glass.
s=scene('Signal · Luminous')
box('Signal aluminum case',(0,0,-.15),(6.9,2.65,.55),'steel','Case',.28)
box('Smoked display bevel',(0,0,.12),(6.61,2.38,.16),'shell','Bezel',.22)
box('Dark display cavity',(0,.05,.216),(6.31,2.07,.03),'screen','ScreenBack',.15)
# Segment polygons laid out with their own mesh names and real thickness.
def segment(name,x,y,z,w,h,horizontal=True):
 if horizontal:poly=[(-w*.39,-h*.5),(w*.39,-h*.5),(w*.5,0),(w*.39,h*.5),(-w*.39,h*.5),(-w*.5,0)]
 else:poly=[(-w*.5,-h*.39),(0,-h*.5),(w*.5,-h*.39),(w*.5,h*.39),(0,h*.5),(-w*.5,h*.39)]
 n=len(poly);vs=[(x+a,y+b,z+c) for c in [-.008,.008] for a,b in poly];fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)];me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,'emission',name,.003)
for i in range(6):
 scale=.87 if i<4 else .39;x=(-2.47+i*1.02) if i<4 else (2.10+(i-4)*.51);yc=.15 if i<4 else -.11
 for j,(dx,dy,hor) in enumerate([(0,.85,True),(.35,.43,False),(.35,-.43,False),(0,-.85,True),(-.35,-.43,False),(-.35,.43,False),(0,0,True)]):segment(f'Seg_{i}_{j}',x+dx*scale,yc+dy*scale,.257,.60*scale if hor else .09*scale,.09*scale if hor else .68*scale,hor)
for y in [-.2,.5]:cyl('Colon',.047,.018,(-.94,y,.255),'emission','Colon',32)
text('Signal brand','Q U E R A I S',(2.34,.65,.255),.075,'lettering','Brand')
quad('Glass',0,.03,.30,6.35,2.1,'glass','Glass')
for x in [-2.45,2.45]:box('Signal foot',(x,-1.38,-.12),(.78,.15,.52),'rubber','Stand',.04)
for i in range(9):
 box('Cooling groove',(-1.3+i*.32,1.29,-.14),(.055,.025,.27),'shell','Cooling',.008)

exec(compile((ROOT/'tools/collection_details.py').read_text(encoding='utf-8-sig'),str(ROOT/'tools/collection_details.py'),'exec'))

# The same studio rig is retained in each editable scene.
for s in scenes:
 bpy.context.window.scene=s;s.render.engine='CYCLES';s.cycles.samples=64;s.cycles.use_denoising=True;s.world=bpy.data.worlds.new(s.name+' world');s.world.use_nodes=True;world=next(n for n in s.world.node_tree.nodes if n.type=='BACKGROUND');world.inputs[0].default_value=(.09,.11,.14,1);world.inputs[1].default_value=.45
 for name,loc,power,size,color in [('Softbox key',(-3,4,6),550,5,(.90,.95,1)),('Warm rim',(4,1,3),400,3,(1,.8,.61)),('Strip edge',(-4,-2,2),300,2,(.66,.81,1))]:
  bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='RECTANGLE';o.data.size=size;o.data.size_y=size*.5;o.data.color=color;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.object.camera_add();cam=bpy.context.object;cam.rotation_euler=(math.radians(13),math.radians(-12),0);cam.location=cam.rotation_euler.to_quaternion()@Vector((0,0,12));cam.data.type='ORTHO';cam.data.ortho_scale=5.6 if 'Atelier' in s.name else 8.4;s.camera=cam;s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.resolution_x=1200;s.render.resolution_y=1100 if 'Atelier' in s.name else 720;s.render.resolution_percentage=100;s.view_settings.view_transform='AgX'
# Export evaluated geometry into bounded native batches.
exec(compile((ROOT/'tools/export_runtime_meshes.py').read_text(encoding='utf-8-sig'),'export_runtime_meshes.py','exec'))
# Save editable text, construction and neutral animation pivots.
for s in scenes:
 bpy.context.window.scene=s;groups={}
 for o in list(s.objects):
  if o.type not in {'MESH','FONT','CURVE'}:continue
  name=o.get('runtime_group','Preview')
  if name not in groups:
   pivot=bpy.data.objects.new('Assembly / '+name,None);s.collection.objects.link(pivot);pivot.empty_display_size=.12
   if 'runtime_pivot' in o:pivot.location=o['runtime_pivot']
   elif name=='Crown':pivot.location=(2.12,0,-.03)
   groups[name]=pivot
  # matrix_world can be stale until Blender evaluates a newly created parent.
  # Set the local basis explicitly so nonzero assembly pivots cannot translate
  # the wheel/crown twice. This also preserves a useful animation pivot in GLB.
  matrix=o.matrix_world.copy();o.parent=groups[name];o.matrix_parent_inverse.identity();o.matrix_basis=groups[name].matrix_basis.inverted()@matrix
 s.render.filepath='//renders/'+s.name.split(' ')[0].lower()+'.png'
bpy.context.window.scene=scenes[0];bpy.data.scenes.remove(staging);bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/querais-collection.blend'))
print('QUERAIS DETAIL COLLECTION SAVED: four editable scenes and independent native runtime assemblies. Run render_collection.py and export_collection_glb.py next.')
