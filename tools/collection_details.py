"""Original, inspectable construction for the Querais collection.

Executed inside build_collection.py before export. Dimensions are art units.
The mechanisms are illustrative assemblies, not manufacturing specifications.
"""
from mathutils import Matrix

def remove_names(*names):
 for o in list(current.objects):
  if any(o.name == n or o.name.startswith(n+'.') for n in names):bpy.data.objects.remove(o,do_unlink=True)

def rr_points(w,h,r,N=12):
 points=[]
 for cx,cy,start in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
  for i in range(N+1):
   a=math.radians(start+i*90/N);points.append((cx+r*math.cos(a),cy+r*math.sin(a)))
 return points

def frame(name,w,h,r,profile,mat,group,center=(0,0),N=12):
 # A closed, actually hollow section swept around a rounded rectangular path.
 loops=[rr_points(w+2*d,h+2*d,max(.015,r+d),N) for d,z in profile];n=len(loops[0]);vs=[(x+center[0],y+center[1],profile[j][1]) for j,loop in enumerate(loops) for x,y in loop];fs=[]
 for j in range(len(loops)-1):
  for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,mat,group)
 # Keep broad faces planar and radiused corners smooth; hard profile changes remain crisp.
 for p in me.polygons:p.use_smooth=False
 mod=o.modifiers.new('Optical edge highlights','BEVEL');mod.width=.008;mod.segments=2;o.modifiers.new('Face-weighted normals','WEIGHTED_NORMAL');return o

def panel(name,w,h,r,z,depth,mat,group,center=(0,0)):
 pts=rr_points(w,h,r,12);n=len(pts);vs=[(x+center[0],y+center[1],z+d) for d in [-depth/2,depth/2] for x,y in pts];fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return finish(o,name,mat,group,.004)

def toroid(name,r,wire,z,mat,group,center=(0,0),N=96):
 profile=[(r+wire*math.cos(a*math.tau/8),z+wire*math.sin(a*math.tau/8)) for a in range(9)];o=ring(name,profile,mat,group,N);o.location.x,o.location.y=center;return o

def socket(name,x,y,z,r=.048,group='Fasteners',rear=False):
 # Machined countersink, annular head and an actual hexagonal socket cavity.
 sign=-1 if rear else 1
 o=ring(name+' countersink',[(r*1.35,z),(r*1.16,z+sign*.008),(r,z+sign*.01)],'rubber',group,24);o.location.x=x;o.location.y=y
 o=ring(name+' turned head',[(r,z),(r,z+sign*.015),(r*.85,z+sign*.024),(r*.48,z+sign*.024),(r*.48,z+sign*.003)],'polished',group,24);o.location.x=x;o.location.y=y
 o=ring(name+' hex recess',[(r*.49,z+sign*.024),(r*.49,z+sign*.005)],'steel',group,6);o.location.x=x;o.location.y=y
 cyl(name+' socket floor',r*.47,.002,(x,y,z+sign*.004),'ink',group,6)

def gear(name,x,y,z,r,teeth,ratio=1):
 # Closed tooth annulus, hub, six spokes and a recessed jewel bearing.
 group='Gear_'+name;N=teeth*4
 profile=[];vs=[]
 for rad,dz in [(r*.74,-.018),(r,-.018),(r,.018),(r*.74,.018)]:
  for i in range(N):
   a=i*math.tau/N;radius=rad if rad<r else r*(1 if i%4 in (1,2) else .91);vs.append((x+radius*math.cos(a),y+radius*math.sin(a),z+dz))
 fs=[]
 for j in range(4):
  for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,((j+1)%4)*N+(i+1)%N,((j+1)%4)*N+i))
 me=bpy.data.meshes.new(group);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(group,me);bpy.context.collection.objects.link(o);finish(o,group,'brass',group,.002)
 for j in range(6):
  a=j*math.tau/6;o=box('Pierced wheel spoke',(x+r*.44*math.sin(a),y+r*.44*math.cos(a),z),(.035,r*.70,.032),'brass',group,.005);o.rotation_euler.z=-a
 cyl('Wheel hub',r*.17,.055,(x,y,z),'polished',group,32)
 for obj in current.objects:
  if obj.get('runtime_group')==group:obj['runtime_pivot']=[x,y,z];obj['runtime_ratio']=ratio
 cyl('Ruby bearing',.042,.012,(x,y,z-.045),'jewel','Jewels',24)
 toroid('Bearing chaton',.055,.012,z-.048,'brass','Jewels',(x,y),32)

def rear_text(txt,x,y,z,size,group='RearCover',mat='lettering'):
 o=text('Engraving / '+txt,txt,(x,y,z),size,mat,group);o.rotation_euler.y=math.pi;return o

def board(w,h,z,kind):
 panel(kind+' ceramic PCB',w,h,.12,z,.045,'pcb','Electronics')
 # Routed copper paths and fine solder pads give scale; actual components, not a photo.
 for i in range(12):
  x=-w*.42+i*w*.076;box('Copper bus',(x,.1,z-.029),(.012,h*.72,.007),'brass','Circuit',.001)
  for y in [-h*.33,h*.33]:cyl('Solder pad',.023,.008,(x,y,z-.037),'polished','Circuit',12)
 for i in range(5):
  x=-w*.32+i*w*.16;box('Controller package',(x,-.17,z-.085),(.34,.45,.10),'rubber','Components',.015)
  for k in range(5):
   for side in [-1,1]:box('IC lead',(x+side*.19,-.34+k*.085,z-.046),(.07,.026,.018),'polished','Circuit',.003)
 for x,y in [(-w*.39,h*.27),(w*.39,h*.27),(0,h*.27)]:
  cyl('Capacitor',.09,.16,(x,y,z-.11),'shell','Components',24);box('Capacitor polarity',(x,y,z-.197),(.10,.012,.003),'lettering','Components',.001)

def rear_service(w,h,z,label):
 panel('Removable service panel',w,h,.15,z,.055,'shell','RearCover')
 frame('Rear perimeter seal',w+.05,h+.05,.17,[(0,z+.04),(-.025,z+.04),(-.025,z+.025),(0,z+.025),(0,z+.04)],'rubber','RearSeal')
 rear_text('Q U E R A I S',0,.25,z-.031,.13)
 rear_text(label,0,.02,z-.031,.075)
 rear_text('DESKTOP INSTRUMENT  /  DESIGNED IN CODE',0,-.19,z-.031,.043)
 for x in [-w*.44,w*.44]:
  for y in [-h*.40,h*.40]:socket('Service screw',x,y,z-.03,.055,'RearCover',True)
 # Recessed USB-C surround, tongue and eight distinct contacts. No painted-on socket.
 frame('Power socket rim',.48,.18,.07,[(0,z-.039),(0,z-.07),(-.035,z-.08),(-.035,z-.04),(0,z-.039)],'polished','RearCover',(0,-h*.34),8)
 panel('Power well',.41,.12,.04,z-.043,.005,'ink','RearCover',(0,-h*.34))
 box('Connector tongue',(0,-h*.34,z-.056),(.29,.034,.019),'rubber','RearCover',.008)
 for i in range(8):box('Connector contact',(-.119+i*.034,-h*.34,z-.068),(.015,.025,.003),'brass','RearCover',.001)

# Materials deliberately separate surface processes: brushed, polished, bead blasted,
# ceramic, copper and ruby. Noise is microscopic roughness, not dirt or fake wear.
material('brass',(.55,.31,.10),.88,.25);material('jewel',(.35,.006,.05),.2,.13);material('pcb',(.018,.09,.075),.28,.43);material('ceramic',(.10,.14,.18),.25,.31)
for role in ['steel','polished','shell','brass','dial']:
 m=materials[role];p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 if 'Anisotropic IOR Level' in p.inputs:p.inputs['Anisotropic IOR Level'].default_value=.65 if role=='steel' else .25
 tex=m.node_tree.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=260;tex.inputs['Detail'].default_value=2
 bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.075 if role=='steel' else .035;bump.inputs['Distance'].default_value=.006;m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],p.inputs['Normal'])
p=next(n for n in materials['glass'].node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.97,.985,1,1);p.inputs['Roughness'].default_value=.018;p.inputs['IOR'].default_value=1.28

# ATELIER: an architectural case and a complete exhibition side.
current=scenes[0];bpy.context.window.scene=current
remove_names('Caseback','Rear medallion','Caseback engraving','Bezel fastener')
ring('Exhibition caseback',[(1.97,-.25),(2.01,-.32),(2.01,-.56),(1.95,-.63),(1.72,-.63),(1.67,-.58),(1.67,-.53)],'steel','RearCover')
ring('Caseback polished bevel',[(1.72,-.633),(1.68,-.623),(1.66,-.586)],'polished','RearCover')
ring('Case midline shadow joint',[(2.025,-.125),(2.03,-.12),(2.03,-.10),(2.025,-.095)],'rubber','CaseSeal')
ring('Crystal compression gasket',[(1.785,.24),(1.805,.26),(1.805,.33),(1.785,.34)],'rubber','CrystalSeal')
ring('Crystal polished lip',[(1.79,.335),(1.805,.34),(1.807,.35),(1.79,.355)],'polished','Bezel')
for i in range(4):
 a=math.pi/4+i*math.pi/2;socket('Bezel socket',1.91*math.sin(a),1.91*math.cos(a),.264,.045,'BezelScrews')
for i in range(8):
 a=i*math.tau/8;socket('Exhibition lock',1.84*math.sin(a),1.84*math.cos(a),-.635,.042,'RearCover',True)
cyl('Movement base',1.67,.035,(0,0,-.30),'ceramic','MovementPlate',128)
# Circular grain and stepped perimeter of the main plate.
for r in [1.51,1.54,1.57,1.60]:toroid('Mainplate turned edge',r,.004,-.327,'steel','MovementPlate')
for args in [('Centre',-.12,-.05,-.40,.54,48,.25),('Third',.68,.37,-.435,.37,36,-.5),('Fourth',.98,-.26,-.46,.30,30,1),('Barrel',-.75,.68,-.395,.55,52,-.08),('Escape',.48,-.77,-.44,.29,20,-2)]:gear(*args)
# Open bridges, radiused edges, blued screws and ruby bearings.
for name,loc,size,angle in [('Barrel bridge',(-.70,.49,-.48),(1.05,.20,.08),-24),('Train bridge',(.53,.13,-.52),(.20,1.37,.07),-29),('Balance cock',(-.68,-.74,-.53),(.24,.58,.075),-42)]:
 o=box(name,loc,size,'steel','MovementBridge',.045);o.rotation_euler.z=math.radians(angle)
for x,y in [(-1.12,.67),(-.30,.30),(.26,.72),(.87,-.38),(-.87,-.91)]:socket('Bridge screw',x,y,-.566,.045,'MovementBridge',True)
bx,by=-.76,-.61
toroid('Balance rim',.39,.027,-.46,'brass','Balance',(bx,by),96)
for i in range(3):
 a=i*math.tau/3;o=box('Balance arm',(bx+.19*math.sin(a),by+.19*math.cos(a),-.46),(.034,.37,.025),'brass','Balance',.008);o.rotation_euler.z=-a
pts=[]
for i in range(360):
 a=i*math.tau/60;r=.055+.24*i/359;pts.append((bx+r*math.cos(a),by+r*math.sin(a),-.49))
curve=bpy.data.curves.new('Six turn hairspring','CURVE');curve.dimensions='3D';curve.bevel_depth=.003;curve.bevel_resolution=1;sp=curve.splines.new('POLY');sp.points.add(len(pts)-1)
for p,co in zip(sp.points,pts):p.co=(*co,1)
o=bpy.data.objects.new('Hairspring',curve);bpy.context.collection.objects.link(o);o.data.materials.append(materials['steel']);o['runtime_group']='Balance'
for o in current.objects:
 if o.get('runtime_group')=='Balance':o['runtime_pivot']=[bx,by,-.46]
cyl('Balance ruby',.037,.014,(bx,by,-.515),'jewel','Jewels',32)
cyl('Rear sapphire',1.665,.012,(0,0,-.60),'glass','RearGlass',128)
rear_text('Q U E R A I S  /  ATELIER',0,1.25,-.57,.075,'MovementBridge')
rear_text('ILLUSTRATIVE CALIBRE',.10,-1.25,-.34,.055,'MovementPlate')
# A domed crystal and explicit edge replace a flat cylinder lid.
remove_names('Crystal')
ring('Box sapphire',[(0,.392),(.7,.391),(1.3,.386),(1.65,.376),(1.76,.36),(1.79,.345),(1.79,.333),(1.74,.345),(1.3,.371),(.7,.376),(0,.377)],'glass','Glass',144)
# Crown construction and a signed inset end-cap.
o=cyl('Crown endcap',.144,.018,(2.273,0,-.03),'polished','Crown',48);o.rotation_euler.y=math.pi/2
o=cyl('Crown inset',.104,.019,(2.283,0,-.03),'shell','Crown',48);o.rotation_euler.y=math.pi/2
for z in [-.105,.045]:box('Crown Q engraving',(2.295,0,z),(.006,.058,.012),'lettering','Crown',.003)

# SLATE: thin cantilevered display, a deep extrusion and a floating polished rim.
current=scenes[1];bpy.context.window.scene=current
remove_names('Slate chassis','Slate graphite insert','Cantilever foot','Side key','Glass')
frame('Slate monocoque',6.44,3.14,.23,[(0,-.74),(0,.08),(-.022,.15),(-.11,.18),(-.16,.13),(-.16,-.74),(0,-.74)],'steel','Case')
frame('Diamond cut aperture',6.20,2.91,.17,[(0,.154),(-.025,.184),(-.053,.184),(-.065,.159),(0,.154)],'polished','Bezel')
frame('Recessed display gasket',6.08,2.79,.14,[(0,.15),(-.028,.169),(-.028,.19),(0,.19),(0,.15)],'rubber','CrystalSeal')
panel('Optical cover',6.04,2.68,.12,.227,.018,'glass','Glass',(0,.05))
for x in [-2.20,2.20]:
 o=box('Milled cantilever',(x,-1.58,-.43),(.58,.20,1.40),'steel','Stand',.06);o.rotation_euler.x=math.radians(-12)
 box('Foot isolation pad',(x,-1.697,-.35),(.49,.025,1.18),'rubber','Stand',.012)
 box('Stand highlight',(x,-1.49,.14),(.40,.025,.12),'polished','Stand',.01)
for i in range(3):
 y=.56-i*.56;o=cyl('Tactile side crown',.13,.11,(3.245,y,-.25),'shell','Keys',40);o.rotation_euler.y=math.pi/2
 for j in range(20):
  a=j*math.tau/20;o=box('Key knurl',(3.26,y+.127*math.sin(a),-.25+.127*math.cos(a)),(.08,.012,.013),'polished','Keys',.002);o.rotation_euler.x=-a
board(5.86,2.55,-.47,'Slate');rear_service(6.10,2.80,-.75,'SLATE  /  TYPOGRAPHIC DISPLAY')
for i in range(22):box('Vent louver',(-2.1+i*.2,1.48,-.38),(.075,.028,.31),'rubber','Cooling',.012)

# RELAY: an open skeletal chassis around four tangible flap cartridges.
current=scenes[2];bpy.context.window.scene=current
remove_names('Relay housing','Relay front lip','Mechanism recess','Housing screw','Relay foot')
frame('Relay structural cage',7.04,3.24,.19,[(0,-.98),(0,.11),(-.04,.17),(-.17,.18),(-.17,-.98),(0,-.98)],'shell','Case')
frame('Front satin surround',6.98,3.18,.17,[(0,.17),(-.025,.215),(-.14,.215),(-.16,.17),(0,.17)],'steel','Bezel')
frame('Polished aperture',6.67,2.87,.11,[(0,.208),(-.017,.228),(-.04,.224),(-.04,.205),(0,.208)],'polished','Bezel')
panel('Mechanical bed',6.60,2.70,.10,-.79,.055,'ceramic','MovementPlate',(0,.09))
for i in range(4):
 x=(i-1.5)*1.48+(0.10 if i>=2 else -.10)
 # Two cheek plates per cassette, a shared transverse axle and a card fan.
 for side in [-1,1]:
  xx=x+side*.696;box('Cartridge cheek',(xx,.09,-.25),(.055,2.48,.95),'shell','Cartridge',.025)
  o=cyl('Axle bush',.105,.095,(xx,.09,-.16),'brass','Hinges',32);o.rotation_euler.y=math.pi/2
 o=cyl('Flap drum axle',.043,1.44,(x,.09,-.16),'polished','Hinges',32);o.rotation_euler.y=math.pi/2
 for j in range(16):
  # The visible stacked edges describe a real radial card mechanism.
  a=math.radians(58+j*15);yy=.09+1.04*math.cos(a);zz=-.16+1.04*math.sin(a)
  # Behind the reading surface; top and rear edges show through the open cage.
  if zz>.19:continue
  o=box('Radial flap card',(x,yy,zz),(1.29,.052,.34),'screen','CardStack',.009);o.rotation_euler.x=-a
  box('Card edge',(x,yy+.018,zz),(1.23,.008,.007),'lettering','CardEdges',.002)
 for y in [-1.17,1.35]:
  box('Retaining rail',(x,y,.16),(1.38,.055,.16),'shell','Cartridge',.014)
  for dx in [-.60,.60]:socket('Cassette fastener',x+dx,y,.251,.031,'Hinges')
 # A recessed brass indexing wheel, individual teeth and central spindle.
 xx=x+.42;yy=-.70
 gear('Relay'+str(i),xx,yy,-.87,.25,24,(-1 if i%2 else 1)*.5)
for x in [-2.64,2.64]:
 box('Relay pedestal',(x,-1.71,-.43),(.85,.21,.98),'steel','Stand',.05);box('Pedestal isolator',(x,-1.827,-.43),(.72,.024,.84),'rubber','Stand',.009)
for x in [-3.31,3.31]:
 for y in [-1.40,1.40]:socket('Cage lock',x,y,.22,.045,'Fasteners')
rear_service(6.70,2.88,-1.035,'RELAY  /  FOUR INDEPENDENT CARTRIDGES')

# SIGNAL: ceramic segment cells in a finned instrument housing.
current=scenes[3];bpy.context.window.scene=current
remove_names('Signal aluminum case','Smoked display bevel','Dark display cavity','Signal foot','Cooling groove','Glass')
frame('Signal extrusion',6.94,2.71,.19,[(0,-.79),(0,.08),(-.04,.16),(-.15,.16),(-.15,-.79),(0,-.79)],'steel','Case')
frame('Anodized front shroud',6.66,2.43,.14,[(0,.14),(-.045,.22),(-.13,.22),(-.13,.15),(0,.14)],'shell','Bezel')
frame('Optical polished bevel',6.43,2.20,.10,[(0,.222),(-.018,.249),(-.034,.25),(-.04,.22),(0,.222)],'polished','Bezel',(0,.03))
panel('Segment cell substrate',6.31,2.07,.08,.205,.027,'screen','ScreenBack',(0,.05))
for i in range(6):
 scale=.87 if i<4 else .39;x=(-2.47+i*1.02) if i<4 else (2.10+(i-4)*.51);yc=.15 if i<4 else -.11
 for j,(dx,dy,hor) in enumerate([(0,.85,True),(.35,.43,False),(.35,-.43,False),(0,-.85,True),(-.35,-.43,False),(-.35,.43,False),(0,0,True)]):
  w=(.60 if hor else .09)*scale;h=(.09 if hor else .68)*scale
  # Each light has an individual deep black well and a thin ceramic mask.
  panel('Segment optical well',w+.035*scale,h+.035*scale,.014*scale,.228,.025,'rubber','SegmentCells',(x+dx*scale,yc+dy*scale))
 for y in [-.92,1.01]:socket('Display module screw',x,y,.242,.022,'ModuleScrews')
panel('Smoked optical crystal',6.35,2.10,.08,.31,.018,'glass','Glass',(0,.03))
for i in range(37):
 x=-2.93+i*.163
 box('Rear cooling fin',(x,0,-.77),(.035,2.32,.22),'shell','HeatSink',.011)
 box('Top cooling rib',(x,1.342,-.31),(.038,.055,.72),'shell','Cooling',.012)
for x in [-2.4,2.4]:
 box('Signal pedestal',(x,-1.44,-.29),(.82,.18,.88),'shell','Stand',.05);box('Signal isolator',(x,-1.54,-.29),(.7,.022,.77),'rubber','Stand',.009)
board(6.16,1.93,-.41,'Signal');rear_service(6.52,2.30,-.92,'SIGNAL  /  OPTICAL SEGMENT INSTRUMENT')
print('DETAIL CONSTRUCTION COMPLETE: exhibition movement, hollow cases, flap cartridges, optical cells, service ports and electronics.')
