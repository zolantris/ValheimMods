$folders = @(
    "src\ValheimRAFT.Unity\Assets\Plugins\Eldritch.Core",
    "src\ValheimRAFT.Unity\Assets\Plugins\Zolantris.Shared"
)

foreach ($folder in $folders) {
    if (Test-Path $folder) {
        Write-Host "Setting assume-unchanged for $folder"
        git update-index --assume-unchanged $folder
    } else {
        Write-Warning "$folder does not exist, skipping."
    }
}