#requires -Version 7.0
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $projectRoot 'dist'
$stage = Join-Path $dist ('.stage-' + [guid]::NewGuid().ToString('N'))
$app = Join-Path $stage 'Querais'
New-Item -ItemType Directory -Force -Path $app | Out-Null

dotnet publish (Join-Path $projectRoot 'src/Querais/Querais.csproj') -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $app
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

foreach ($name in @('LICENSE', 'README.md', 'THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $app
}
$noticeDir = Join-Path $app 'licenses'
New-Item -ItemType Directory -Force -Path $noticeDir | Out-Null
$assets = Get-Content -LiteralPath (Join-Path $projectRoot 'src/Querais/obj/project.assets.json') -Raw | ConvertFrom-Json -Depth 100
$runtimePackages = @($assets.project.frameworks.PSObject.Properties | ForEach-Object { $_.Value.downloadDependencies } | Where-Object { $_.name -match '^Microsoft\.(NETCore|WindowsDesktop)\.App\.Runtime\.win-x64$' } | Sort-Object name -Unique)
if ($runtimePackages.Count -ne 2) { throw 'Could not identify both restored runtime packs for license collection.' }
foreach ($package in $runtimePackages) {
    $version = $package.version.Trim('[', ']').Split(',')[0].Trim()
    $relativePath = $package.name.ToLowerInvariant() + '/' + $version
    $packagePath = $null
    foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
        $candidate = Join-Path $folder $relativePath
        if (Test-Path -LiteralPath $candidate) { $packagePath = $candidate; break }
    }
    if (-not $packagePath) { throw "Runtime package not found: $($package.name)" }
    $prefix = $package.name + '-' + $version
    $copied = 0
    foreach ($name in @('LICENSE', 'LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT')) {
        $notice = Join-Path $packagePath $name
        if (Test-Path -LiteralPath $notice) {
            Copy-Item -LiteralPath $notice -Destination (Join-Path $noticeDir ($prefix + '-' + $name))
            $copied++
        }
    }
    if ($copied -eq 0) { throw "Missing runtime license: $($package.Name)" }
}

$zip = Join-Path $dist 'Querais-win-x64.zip'
Compress-Archive -Path $app -DestinationPath $zip -Force
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Value ($hash + '  Querais-win-x64.zip') -Encoding utf8NoBOM
Write-Output "Portable app: $app"
Write-Output "Release ZIP: $zip"
Write-Output "SHA-256: $hash"
