param(
    [String]$LogPath
)

if (-Not $LogPath)
{
    Write-Host "Path is not specified." -ForegroundColor Red
    return
}

if (-Not (Test-Path $LogPath))
{
    Write-Host "File not found: $LogPath" -ForegroundColor Yellow
    return
}

# Define patterns to exclude
$excludePatterns = @(".*Activating default element Continue")

# Tail the log file and apply filtering only
gc -wait -tail 10 $LogPath |
        Where-Object {
            # Check if the line matches any exclusion pattern
            $_ -notmatch $excludePatterns
        } |
        ForEach-Object {
            Write-Host $_
        }
