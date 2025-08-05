Write-Host "Deleting all bin and obj folders under src\*"

$root = Join-Path $PSScriptRoot "src"
Get-ChildItem -Path $root -Directory -Recurse -Include bin,obj |
        ForEach-Object {
            Write-Host "Deleting $( $_.FullName )"
            Remove-Item $_.FullName -Recurse -Force
        }

Write-Host "Done!"