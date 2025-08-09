# Define target paths
$unityPluginsDir = "C:\Users\fre\dev\repos\ValheimMods\src\ValheimRAFT.Unity\Assets\Plugins"

$eldritchTarget = "$unityPluginsDir\Eldritch.Core"
$eldritchSource = "C:\Users\fre\dev\repos\ValheimMods\src\Eldritch.Core"

$zolantrisTarget = "$unityPluginsDir\Zolantris.Shared"
$zolantrisSource = "C:\Users\fre\dev\repos\ValheimMods\src\Shared"

# Remove existing folders or links (if they exist)
#if (Test-Path $eldritchTarget) { Remove-Item $eldritchTarget -Force }
#if (Test-Path $zolantrisTarget) { Remove-Item $zolantrisTarget -Force }

# Create junctions (safe for folders and works in Unity)
cmd /c mklink /J "$eldritchTarget" "$eldritchSource"
cmd /c mklink /J "$zolantrisTarget" "$zolantrisSource"
