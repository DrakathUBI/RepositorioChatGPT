$ErrorActionPreference = 'Stop'

function Find-ProjectFile {
    param(
        [string[]]$SearchRoots
    )

    foreach ($root in $SearchRoots) {
        if ([string]::IsNullOrWhiteSpace($root)) { continue }
        if (-not (Test-Path $root)) { continue }

        $direct = Join-Path $root 'MacroStudio\MacroStudio.csproj'
        if (Test-Path $direct) {
            return (Resolve-Path $direct).Path
        }

        $found = Get-ChildItem -Path $root -Filter 'MacroStudio.csproj' -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($null -ne $found) {
            return $found.FullName
        }
    }

    return $null
}

$ScriptRoot = $PSScriptRoot
$CurrentDir = (Get-Location).Path
$ParentOfScript = Split-Path -Parent $ScriptRoot

$ProjectPath = Find-ProjectFile -SearchRoots @($CurrentDir, $ScriptRoot, $ParentOfScript)

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    throw @"
Não encontrei o arquivo MacroStudio.csproj.

Como corrigir:
1) Faça git clone do repositório completo.
2) Entre na pasta do repositório.
3) Rode este script a partir da raiz do projeto.

Exemplo:
  git clone https://github.com/DrakathUBI/RepositorioChatGPT.git
  cd RepositorioChatGPT
  powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
"@
}

$RepoRoot = Split-Path -Parent (Split-Path -Parent $ProjectPath)
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
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

Write-Host "Projeto encontrado: $ProjectPath"
Write-Host 'Restaurando pacotes...'
& $dotnetExe restore $ProjectPath
if ($LASTEXITCODE -ne 0) {
    throw "Falha no dotnet restore (exit code: $LASTEXITCODE)."
}

Write-Host 'Publicando MacroStudio.exe (single-file)...'
& $dotnetExe publish $ProjectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o $OutputDir
if ($LASTEXITCODE -ne 0) {
    throw "Falha no dotnet publish (exit code: $LASTEXITCODE)."
}

$exePath = Join-Path $OutputDir 'MacroStudio.exe'
if (-not (Test-Path $exePath)) {
    throw "Publicação terminou sem gerar o executável esperado: $exePath"
}

Write-Host ''
Write-Host 'Concluído! Executável gerado em:'
Write-Host $exePath
