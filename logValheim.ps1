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

function Get-LogColor
{
    Param(
        [Parameter(Position = 0)]
        [String]$LogEntry
    )

    process {
        if ($LogEntry.Contains("ValheimRAFT") -or $LogEntry.Contains("ValheimVehicles") -or $LogEntry.Contains("DynamicLocations") -or $LogEntry.Contains("ZdoWatcher") -or $LogEntry.Contains("Zolantris.Shared"))
        {
            if ( $LogEntry.Contains("Debug"))
            {
                Return "Green"
            }
            elseif ($LogEntry.Contains("Warn"))
            {
                Return "Yellow"
            }
            elseif ($LogEntry.Contains("Error") -or $LogEntry.Contains("NullReferenceException"))
            {
                Return "Red"
            }
            else
            {
                Return "White"
            }
        }
        # we should still see red for errors.        
        if ($LogEntry.Contains("NullReferenceException") -or $LogEntry.Contains("Error"))
        {
            Return "Red"
        }
        else
        {
            # makes other messages less visible            
            Return "Gray"
        }
    }
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
            Write-Host -ForegroundColor (Get-LogColor $_) $_
        }
