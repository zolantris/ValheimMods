param(
    [String]$LogPath
)

if (-Not $LogPath) {
    Write-Host "Path is not specified." -ForegroundColor Red
    return
}

if (-Not (Test-Path $LogPath)) {
    Write-Host "File not found: $LogPath" -ForegroundColor Yellow
    return
}

function Get-LogColor {
  Param(
      [Parameter(Position=0)]
      [String]$LogEntry
  )

  process {
    if ($LogEntry.Contains("ValheimRAFT") -or $LogEntry.Contains("ValheimVehicles")){
      if ($LogEntry.Contains("Debug")) {Return "Green"}
      elseif ($LogEntry.Contains("Warn")) {Return "Yellow"}
      elseif ($LogEntry.Contains("Error") -or $LogEntry.Contains("NullReferenceException")) {Return "Red"}
      else {Return "White"}
    }
    if ($LogEntry.Contains("NullReferenceException")) {Return "Red"}
    else {Return "White"}
  }
}


gc -wait -tail 10 $LogPath | ForEach {Write-Host -ForegroundColor (Get-LogColor $_) $_}
