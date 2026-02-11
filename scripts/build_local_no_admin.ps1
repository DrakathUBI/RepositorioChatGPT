$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RepoRoot 'MacroStudio/MacroStudio.csproj'
$DotnetRoot = Join-Path $env:USERPROFILE '.dotnet'
$ToolsPath = Join-Path $DotnetRoot 'tools'
$OutputDir = Join-Path $RepoRoot 'dist'

if (-not (Test-Path $DotnetRoot)) {
    New-Item -ItemType Directory -Path $DotnetRoot | Out-Null
}
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$dotnetExe = Join-Path $DotnetRoot 'dotnet.exe'

if (-not (Test-Path $dotnetExe)) {
    Write-Host 'Baixando instalador local do .NET SDK (sem admin)...'
    $installScript = Join-Path $env:TEMP 'dotnet-install.ps1'
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installScript
    & powershell -ExecutionPolicy Bypass -File $installScript -Version 8.0.303 -InstallDir $DotnetRoot
}

$env:DOTNET_ROOT = $DotnetRoot
$env:PATH = "$DotnetRoot;$ToolsPath;$env:PATH"

Write-Host 'Restaurando pacotes...'
& $dotnetExe restore $Project

Write-Host 'Publicando MacroStudio.exe (single-file)...'
& $dotnetExe publish $Project -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o $OutputDir

Write-Host ''
Write-Host 'Concluído! Executável gerado em:'
Write-Host (Join-Path $OutputDir 'MacroStudio.exe')
