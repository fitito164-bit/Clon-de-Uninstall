# Compila Quitar en tu Windows: pruebas, ejecutable único y, si tenés Inno Setup 6, el instalador.
# Requisito: SDK de .NET 10 (https://dotnet.microsoft.com/download/dotnet/10.0).
# Uso:  powershell -ExecutionPolicy Bypass -File .\build.ps1 -Version 0.2.0

param([string]$Version = "0.2.0")

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Check($step) {
    if ($LASTEXITCODE -ne 0) { throw "Falló: $step (código $LASTEXITCODE)" }
}

dotnet test Quitar.slnx -c Release
Check "pruebas"

dotnet publish src/Quitar.Desktop/Quitar.Desktop.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version `
    -o publish
Check "publicación"

$iscc = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
if (Test-Path $iscc) {
    & $iscc "/DAppVersion=$Version" installer\quitar.iss
    Check "instalador"
    Write-Host "Listo: installer\Output\Quitar-Setup-$Version.exe"
} else {
    Write-Host "Inno Setup 6 no está instalado, así que no se armó el instalador."
    Write-Host "Ejecutable portable listo: publish\Quitar.exe"
}
