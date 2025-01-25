param (
    [string]$UnityHubPath,
    [string]$ValheimGameFolder,
    [string]$WindowsPlayerExe,
    [string]$UnityPlayerDll,
    [string]$WinPixEventRuntimeDll,
    [string]$ManagedFolder,
    [string]$ResourceFolder
)

# Debugging: Output all variables passed to the script
Write-Host "Debugging: The following variables were passed to the script:"
Write-Host "UnityHubPath: $UnityHubPath"
Write-Host "ValheimGameFolder: $ValheimGameFolder"
Write-Host "WindowsPlayerExe: $WindowsPlayerExe"
Write-Host "UnityPlayerDll: $UnityPlayerDll"
Write-Host "WinPixEventRuntimeDll: $WinPixEventRuntimeDll"
Write-Host "ManagedFolder: $ManagedFolder"
Write-Host "ResourceFolder: $ResourceFolder"

# Function to check if a path is valid
function Test-ValidPath {
    param (
        [string]$path
    )
    if (-not (Test-Path $path)) {
        Write-Error "The path '$path' is invalid."
        exit
    }
}

# Function to compare files by hash and only copy if they are different
function Copy-FileIfDifferent {
    param (
        [string]$sourceFile,
        [string]$destinationFile
    )
    Write-Host "Comparing files: $sourceFile and $destinationFile"

    # If the destination file exists, remove it before copying
    if (Test-Path $destinationFile) {
        Write-Host "File $destinationFile exists. Removing it before copying."
        Remove-Item $destinationFile -Force
    }

    # Now copy the file
    Write-Host "Copying $sourceFile to $destinationFile"
    Copy-Item $sourceFile -Destination $destinationFile -Force
}

# Ensure the paths are valid
Write-Host "Validating paths..."
Test-ValidPath $UnityHubPath
Test-ValidPath $ValheimGameFolder
Test-ValidPath $WindowsPlayerExe
Test-ValidPath $UnityPlayerDll
Test-ValidPath $WinPixEventRuntimeDll
Test-ValidPath $ManagedFolder
Test-ValidPath $ResourceFolder

# Copy and rename WindowsPlayer.exe to valheim.exe
$destinationFile = "$ValheimGameFolder\valheim.exe"
Copy-FileIfDifferent -sourceFile $WindowsPlayerExe -destinationFile $destinationFile

# Copy other necessary DLL files
Copy-FileIfDifferent -sourceFile $UnityPlayerDll -destinationFile "$ValheimGameFolder\UnityPlayer.dll"
Copy-FileIfDifferent -sourceFile $WinPixEventRuntimeDll -destinationFile "$ValheimGameFolder\WinPixEventRuntime.dll"

# Copy the managed and resource folders, overwriting all contents
Write-Host "Copying the managed and resource folders..."
Copy-Item -Path $ManagedFolder -Destination "$ValheimGameFolder\valheim_Data\managed" -Recurse -Force
Copy-Item -Path $ResourceFolder -Destination "$ValheimGameFolder\valheim_Data\resource" -Recurse -Force

Write-Host "Files have been copied and integrated."
