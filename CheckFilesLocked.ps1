param (
    [string[]]$filePaths
)

$lockedFiles = @()

foreach ($filePath in $filePaths)
{
    # Trim and skip empty or whitespace file paths
    $filePath = $filePath.Trim()
    if ( [string]::IsNullOrWhiteSpace($filePath))
    {
        continue
    }

    if (-Not (Test-Path $filePath))
    {
        Write-Host "File does not exist and will be ignored: $filePath"
        continue  # Skip to the next file
    }

    try
    {
        # Try to open the file in read-only mode to check if it's locked
        $fileStream = [System.IO.FileStream]::new($filePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::None)
        $fileStream.Close()
    }
    catch
    {
        $lockedFiles += $filePath
    }
}

if ($lockedFiles.Count -gt 0)
{
    Write-Host "Locked files: $( $lockedFiles -join ', ' )"
    exit 1  # One or more files are locked
}
else
{
    exit 0  # No files are locked
}
