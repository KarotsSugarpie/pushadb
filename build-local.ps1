# PushADB - compile and package locally on Windows
# Run in PowerShell from repository root: powershell -ExecutionPolicy Bypass -File .\build-local.ps1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$project = Join-Path $PSScriptRoot "src\PushADB\PushADB.csproj"
$dist = Join-Path $PSScriptRoot "dist"
$zip = Join-Path $PSScriptRoot "PushADB-Windows-x64.zip"
Write-Host "=== PushADB Windows x64 ===" -ForegroundColor Cyan
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "SDK .NET 8 absent : installe https://dotnet.microsoft.com/download/dotnet/8.0"
}
dotnet --version
if ($LASTEXITCODE -ne 0) { throw "dotnet indisponible" }
dotnet restore $project -r win-x64 --configfile (Join-Path $PSScriptRoot "NuGet.Config") --force
if ($LASTEXITCODE -ne 0) { throw "Restore echoue : verifier acces HTTPS a api.nuget.org (443)" }
dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $dist
if ($LASTEXITCODE -ne 0) { throw "Compilation echouee" }
$adbDir = Join-Path $dist "platform-tools"
if (-not (Test-Path (Join-Path $adbDir "adb.exe"))) {
    $tempZip = Join-Path $env:TEMP "pushadb-platform-tools.zip"
    $tempDir = Join-Path $env:TEMP "pushadb-platform-tools"
    Invoke-WebRequest "https://dl.google.com/android/repository/platform-tools-latest-windows.zip" -OutFile $tempZip
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
    Expand-Archive $tempZip -DestinationPath $tempDir
    Copy-Item (Join-Path $tempDir "platform-tools") $adbDir -Recurse -Force
}
Copy-Item (Join-Path $PSScriptRoot "README.md") (Join-Path $dist "README.md") -Force
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $dist "*") -DestinationPath $zip
Write-Host "=== SUCCES : $zip ===" -ForegroundColor Green
