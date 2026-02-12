$ErrorActionPreference = 'Stop'

$RepoZipUrl = 'https://github.com/DrakathUBI/RepositorioChatGPT/archive/refs/heads/main.zip'
$TargetRoot = Join-Path $env:USERPROFILE 'Desktop'
$ZipFile = Join-Path $env:TEMP 'RepositorioChatGPT-main.zip'

Write-Host "Baixando repositório sem Git: $RepoZipUrl"
Invoke-WebRequest -Uri $RepoZipUrl -OutFile $ZipFile

if (-not (Test-Path $TargetRoot)) {
    New-Item -ItemType Directory -Path $TargetRoot | Out-Null
}

Write-Host "Extraindo para: $TargetRoot"
Expand-Archive -Path $ZipFile -DestinationPath $TargetRoot -Force

$Extracted = Join-Path $TargetRoot 'RepositorioChatGPT-main'
$Final = Join-Path $TargetRoot 'RepositorioChatGPT'

if (Test-Path $Final) {
    Remove-Item -Recurse -Force $Final
}

Rename-Item -Path $Extracted -NewName 'RepositorioChatGPT'

Write-Host "Concluído. Pasta pronta em: $Final"
Write-Host "Próximo passo:"
Write-Host "  cd $Final"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1"
