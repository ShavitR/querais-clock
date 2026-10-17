import asyncio, json, os, sys
from pathlib import Path
from datetime import datetime, timezone
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
ROOT=Path(__file__).resolve().parents[1]
async def main():
    (ROOT/'artifacts').mkdir(exist_ok=True)
    env=dict(os.environ,BLENDER_HOST='127.0.0.1',BLENDER_MCP_DISABLE_TELEMETRY='1')
    params=StdioServerParameters(command=str(ROOT/'.tools/venv/Scripts/python.exe'),args=['-m','blender_mcp.server'],env=env)
    async with stdio_client(params) as (r,w):
        async with ClientSession(r,w) as s:
            await s.initialize()
            listing=await s.list_tools()
            print('MCP tools:', ', '.join(t.name for t in listing.tools))
            tool='get_scene_info' if len(sys.argv)<2 else 'execute_blender_code'
            args={} if len(sys.argv)<2 else {'code':'QUERAIS_PROJECT_ROOT = '+repr(str(ROOT))+'\n'+Path(sys.argv[1]).read_text(encoding='utf-8-sig'),'user_prompt':'Create and refine the Querais clock collection assets.'}
            result=await s.call_tool(tool,args,read_timeout_seconds=__import__('datetime').timedelta(seconds=600))
            out=result.model_dump(mode='json')
            record={'utc':datetime.now(timezone.utc).isoformat(),'tool':tool,'script':sys.argv[1] if len(sys.argv)>1 else None,'result':out}
            with (ROOT/'artifacts/mcp-calls.jsonl').open('a',encoding='utf-8') as f:f.write(json.dumps(record)+'\n')
            print(json.dumps(out,indent=2))
            if result.isError or any(c.type=='text' and c.text.startswith('Error executing code:') for c in result.content):
                raise RuntimeError('Blender MCP reported failure; inspect the response above.')
asyncio.run(main())
