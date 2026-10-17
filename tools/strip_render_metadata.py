"""Remove local authoring paths from previews without changing image pixels."""
import hashlib,struct
from pathlib import Path
root=Path(__file__).resolve().parents[1]
for path in (root/'assets/collection').glob('*-preview.png'):
 data=path.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n';offset=8;chunks=[];before=[];after=[]
 while offset<len(data):
  length=struct.unpack('>I',data[offset:offset+4])[0];kind=data[offset+4:offset+8];chunk=data[offset:offset+length+12];offset+=length+12
  if kind==b'IDAT':before.append(chunk)
  if kind not in {b'tEXt',b'iTXt',b'zTXt',b'eXIf'}:
   chunks.append(chunk)
   if kind==b'IDAT':after.append(chunk)
 assert hashlib.sha256(b''.join(before)).digest()==hashlib.sha256(b''.join(after)).digest()
 path.write_bytes(data[:8]+b''.join(chunks))
print('Removed preview authoring metadata; compressed pixel data preserved exactly.')
