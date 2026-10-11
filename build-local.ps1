param([switch]$CloseRunningApp)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$project = Join-Path $PSScriptRoot "src\PushADB\PushADB.csproj"
$dist = Join-Path $PSScriptRoot "dist"
$zip = Join-Path $PSScriptRoot "PushADB-Windows-x64.zip"
$staging = Join-Path $env:TEMP ("PushADB-package-" + [guid]::NewGuid().ToString("N"))
Write-Host "=== PushADB Windows x64 ===" -ForegroundColor Cyan

$running = @(Get-CimInstance Win32_Process -Filter "Name = 'PushADB.exe'" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    if (-not $CloseRunningApp) {
        throw "PushADB est ouvert. Ferme l'application et relance le build, ou passe -CloseRunningApp."
    }
    foreach ($p in $running) {
        Write-Host "Fermeture de PushADB PID $($p.ProcessId)..." -ForegroundColor Yellow
        $process = Get-Process -Id $p.ProcessId -ErrorAction SilentlyContinue
        if ($process) {
            $null = $process.CloseMainWindow()
            if (-not $process.WaitForExit(5000)) {
                throw "PushADB PID $($p.ProcessId) refuse de se fermer. Ferme-le manuellement."
            }
        }
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "SDK .NET 8 absent" }
dotnet --version
if ($LASTEXITCODE -ne 0) { throw "dotnet indisponible" }
dotnet restore $project -r win-x64 --configfile (Join-Path $PSScriptRoot "NuGet.Config") --force
if ($LASTEXITCODE -ne 0) { throw "Restauration NuGet echouee" }
dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $dist
if ($LASTEXITCODE -ne 0) { throw "Compilation echouee" }
if (-not (Test-Path (Join-Path $dist "PushADB.exe"))) { throw "Executable non cree" }

New-Item -Path $staging -ItemType Directory -Force | Out-Null
try {
    $adbDir = Join-Path $dist "platform-tools"
    if (-not (Test-Path (Join-Path $adbDir "adb.exe"))) {
        $tempZip = Join-Path $env:TEMP "pushadb-platform-tools.zip"
        $tempDir = Join-Path $env:TEMP "pushadb-platform-tools"
        Invoke-WebRequest "https://dl.google.com/android/repository/platform-tools-latest-windows.zip" -OutFile $tempZip
        if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
        Expand-Archive $tempZip -DestinationPath $tempDir -Force
        Copy-Item (Join-Path $tempDir "platform-tools") $adbDir -Recurse -Force
    }
    if (-not (Test-Path (Join-Path $adbDir "adb.exe"))) { throw "ADB introuvable dans dist/platform-tools" }
    Copy-Item (Join-Path $PSScriptRoot "README.md") (Join-Path $dist "README.md") -Force
    Copy-Item (Join-Path $dist "*") $staging -Recurse -Force -ErrorAction Stop
    $tmpZip = Join-Path $env:TEMP ("PushADB-" + [guid]::NewGuid().ToString("N") + ".zip")
    try {
        Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $tmpZip -ErrorAction Stop
        Add-Type -AssemblyName System.IO.Compression
        $archive = [System.IO.Compression.ZipFile]::OpenRead($tmpZip)
        try {
            $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace([char]92, [char]47) })
            if ($names -notcontains "PushADB.exe") { throw "ZIP invalide : PushADB.exe manquant" }
            if (-not ($names | Where-Object { $_ -match "(^|/)platform-tools/adb\.exe$" })) { throw ("ZIP invalide : ADB manquant. Entrees ADB : " + (($names | Where-Object { $_ -match "adb.exe$" }) -join ", ")) }
        } finally { $archive.Dispose() }
        Move-Item $tmpZip $zip -Force
    } finally { if (Test-Path $tmpZip) { Remove-Item $tmpZip -Force } }
} finally { Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue }
if (-not (Test-Path $zip)) { throw "Archive finale introuvable" }
$hash = (Get-FileHash -Path $zip -Algorithm SHA256).Hash
"$hash  PushADB-Windows-x64.zip" | Set-Content -Path (Join-Path $PSScriptRoot "PushADB-Windows-x64.zip.sha256") -Encoding ascii
Write-Host "=== ZIP VERIFIE : $zip ===" -ForegroundColor Green
Write-Host "SHA256: $hash"
