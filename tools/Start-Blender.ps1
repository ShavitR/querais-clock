param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$blenderExe = Get-ChildItem -LiteralPath "$env:ProgramFiles\Blender Foundation" -Filter blender.exe -Recurse | Select-Object -First 1 -ExpandProperty FullName
if (-not $blenderExe) { throw 'Blender was not found under Program Files/Blender Foundation.' }
Start-Process -FilePath $blenderExe -ArgumentList @('--python', ('"' + (Join-Path $PSScriptRoot 'start_blender.py') + '"')) -WindowStyle Hidden
