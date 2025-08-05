# Remove only if the path is a symlinked directory (junction)
$folders = @(
    "src\ValheimRAFT.Unity\Assets\Plugins\Eldritch.Core\",
    "src\ValheimRAFT.Unity\Assets\Plugins\Zolantris.Shared\"
)

foreach ($folder in $folders) {
    git update-index --assume-unchanged $folder
}