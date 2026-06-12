# ============================================
# CORRA STUDIO - BUILD INSTALLER SCRIPT
# ============================================

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\publish"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Corra Studio - Build Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Clean old builds
Write-Host "`n[1/6] Cleaning old builds..." -ForegroundColor Yellow
Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\src\CorraStudio.Presentation.Wpf\bin\$Configuration" -Recurse -Force -ErrorAction SilentlyContinue

# 2. Restore NuGet packages
Write-Host "`n[2/6] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore CorraStudio.sln

# 3. Build application
Write-Host "`n[3/6] Building application..." -ForegroundColor Yellow
dotnet publish src/CorraStudio.Presentation.Wpf/CorraStudio.Presentation.Wpf.csproj `
    -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true `
    -p:DebugType=embedded -p:DebugSymbols=false `
    -o "$OutputDir\app"

# 4. Create version file
Write-Host "`n[4/6] Creating version file..." -ForegroundColor Yellow
$versionJson = @{
    Version = $Version
    ReleaseDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json
$versionJson | Out-File -FilePath "$OutputDir\app\version.json" -Encoding utf8

# 5. Copy assets
Write-Host "`n[5/6] Copying assets..." -ForegroundColor Yellow
Copy-Item ".\src\CorraStudio.Deployment\Assets\*" -Destination "$OutputDir\app" -Recurse -ErrorAction SilentlyContinue

# 6. Create installer
Write-Host "`n[6/6] Creating installer..." -ForegroundColor Yellow

# Create ZIP archive
Compress-Archive -Path "$OutputDir\app\*" -DestinationPath "$OutputDir\CorraStudio-$Version-portable.zip"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "BUILD COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Output: $OutputDir" -ForegroundColor Green
Write-Host "Portable ZIP: CorraStudio-$Version-portable.zip" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
