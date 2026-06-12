# ============================================
# CREATE UPDATE INFO FOR AUTO-UPDATER
# ============================================

param(
    [string]$Version = "1.0.0",
    [string]$ReleaseNotes = "Initial release",
    [string]$DownloadUrl = "https://downloads.corrastudio.com/update.zip",
    [bool]$IsMandatory = $false
)

# Calculate hash of update file
$updateFile = "CorraStudio-$Version-update.zip"
$fileHash = Get-FileHash -Path $updateFile -Algorithm SHA256
$fileSize = (Get-Item $updateFile).Length

$updateInfo = @{
    version = $Version
    releaseDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    releaseNotes = $ReleaseNotes
    downloadUrl = $DownloadUrl
    installerUrl = $DownloadUrl -replace "update.zip", "setup.exe"
    fileSize = $fileSize
    fileHash = $fileHash.Hash.ToLower()
    isMandatory = $IsMandatory
    files = @(
        @{
            name = "CorraStudio.exe"
            relativePath = "CorraStudio.exe"
            hash = $fileHash.Hash.ToLower()
            size = $fileSize
        }
    )
}

$updateInfo | ConvertTo-Json -Depth 10 | Out-File -FilePath "update-info.json" -Encoding utf8

Write-Host "Created update-info.json for version $Version" -ForegroundColor Green
