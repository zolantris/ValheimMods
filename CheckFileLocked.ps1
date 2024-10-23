param (
    [string[]]$filePaths
)

$lockedFiles = @()

foreach ($filePath in $filePaths) {
    if (-Not (Test-Path $filePath)) {
        Write-Host "File does not exist and will be ignored: $filePath"
        continue  # Skip to the next file
    }

    try {
        $fileStream = [System.IO.File]::Open($filePath, 'Open', 'Read', 'None')
        $fileStream.Close()
    } catch {
        $lockedFiles += $filePath
    }
}

if ($lockedFiles.Count -gt 0) {
    Write-Host "Locked files: $($lockedFiles -join ', ')"
    exit 1  # One or more files are locked
} else {
    exit 0  # No files are locked
}