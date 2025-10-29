$unityAssets = "src/ValheimRAFT.Unity/Assets"
$links = @(
    @{ Link = "$unityAssets/Plugins/Eldritch.Core"; Target = "src/Eldritch.Core" },
    @{ Link = "$unityAssets/Plugins/Zolantris.Shared"; Target = "src/Shared" -replace '\.', '/' }
)

foreach ($l in $links)
{
    if (Test-Path $l.Link)
    {
        Remove-Item -Recurse -Force $l.Link
    }
    New-Item -ItemType Junction -Path $l.Link -Target $l.Target | Out-Null
}
Write-Host "Links recreated."