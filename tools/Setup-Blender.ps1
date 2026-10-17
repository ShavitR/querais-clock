#requires -Version 7.0
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$toolsDir = Join-Path $projectRoot '.tools'
$checkout = Join-Path $toolsDir 'blender-mcp'
$revision = '7a0373ec9199183cb460068c4f96aed9c579fb4f'
foreach ($command in @('git', 'uv')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Install $command first." }
}
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
if (-not (Test-Path -LiteralPath $checkout)) {
    git clone --no-checkout https://github.com/ahujasid/blender-mcp.git $checkout
    if ($LASTEXITCODE -ne 0) { throw 'Could not clone MCP for Blender.' }
    git -C $checkout checkout --detach $revision
    if ($LASTEXITCODE -ne 0) { throw 'Could not check out the pinned integration.' }
} else {
    $current = (git -C $checkout rev-parse HEAD).Trim()
    if ($current -ne $revision) { throw 'The existing Blender MCP folder uses a different revision; preserve it and use a fresh checkout for this setup.' }
}
$python = Join-Path $toolsDir 'venv/Scripts/python.exe'
if (-not (Test-Path -LiteralPath $python)) {
    uv venv (Join-Path $toolsDir 'venv')
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the authoring environment.' }
}
uv pip install --python $python -e $checkout
if ($LASTEXITCODE -ne 0) { throw 'Could not install the integration dependencies.' }
Write-Output 'Ready. Run tools/Start-Blender.ps1, then the scene query shown in docs/ASSET_AUTHORING.md.'
