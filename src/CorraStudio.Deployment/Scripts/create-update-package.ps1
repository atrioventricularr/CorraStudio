# ============================================
# CREATE UPDATE PACKAGE FOR AUTO-UPDATER
# ============================================

param(
    [string]$Version = "1.0.0",
    [string]$BuildDir = ".\publish\app"
)

$updateZip = "CorraStudio-$Version-update.zip"

Write-Host "Creating update package for version $Version..." -ForegroundColor Yellow

# Create ZIP of new files
Compress-Archive -Path "$BuildDir\*" -DestinationPath $updateZip -Force

# Create update info
.\create-update-info.ps1 -Version $Version -ReleaseNotes "New features and bug fixes"

Write-Host "Update package created: $updateZip" -ForegroundColor Green
Write-Host "Upload to: https://downloads.corrastudio.com/" -ForegroundColor Green
