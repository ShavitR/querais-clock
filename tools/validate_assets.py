"""Validate the actual compressed runtime contract, not just Blender source."""
import gzip,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[1];results=[]
for key in ['atelier','slate','relay','signal']:
 path=root/'assets/collection'/f'{key}.mesh.json.gz';scene=json.loads(gzip.decompress(path.read_bytes()));parts=scene['parts'];triangles=0
 assert scene['key']==key and scene['width']>0 and scene['height']>0
 for p in parts:
  count=len(p['positions'])//3;ix=p['indices'];assert len(p['positions'])==len(p['normals'])==count*3 and len(p['uv'])==count*2 and len(ix)%3==0
  if p['name']!='DialText':assert count<=18000 and len(ix)<=54000,(key,p['name'],'WPF batch budget')
  assert all(isinstance(i,int) and 0<=i<count for i in ix),(key,p['name'],'index bounds')
  assert all(math.isfinite(v) for field in ['positions','normals','uv'] for v in p[field]),(key,p['name'],'non-finite data')
  for i in range(0,len(p['normals']),3):assert .98<sum(v*v for v in p['normals'][i:i+3])<1.02,(key,p['name'],'normal length')
  if p['name'].startswith('Gear_') or p['name']=='Balance':assert len(p['pivot'])==3 and all(math.isfinite(v) for v in p['pivot'])
  triangles+=len(ix)//3
 names={p['name'] for p in parts};required={'Case','Bezel','RearCover','Stand'} if key!='atelier' else {'Case','Bezel','Glass','RearGlass','Balance','Hand_Hour','Hand_Minute','Hand_Second','Crown','Date_Display'}
 if key in {'slate','signal'}:required.add('Glass')
 assert required<=names,(key,'missing semantic assembly',required-names)
 if key=='slate':assert 'Display_Main' in names and 'Electronics' in names
 if key=='relay':assert all(f'Flip_{i}' in names and f'Flap_{i}_Upper' in names and f'Flap_{i}_Lower' in names for i in range(4))
 if key=='signal':assert all(f'Seg_{i}_{j}' in names for i in range(6) for j in range(7))
 assert triangles<350000 and len(parts)<120
 results.append(dict(model=key,triangles=triangles,material_batches=len(parts),compressed_bytes=path.stat().st_size,passed=True))
print(json.dumps(results,indent=2));(root/'docs/asset-validation.json').write_text(json.dumps(results,indent=2)+'\n')
