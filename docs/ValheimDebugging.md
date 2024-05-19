## How to Create a Dev Build

1. Fresh install Valheim. (to make sure things are fully clean)
2. Install the Unity Version of the target Valheim Game. IE `2022.3.17f1`
3. Within the install target. C:\Program
   Files\Unity\Hub\Editor\2022.3.17f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono
    - Copy `WindowsPlayer.exe` and paste within `<ValheimGameFolder>`.
    - Rename `WindowsPlayer.exe` to `valheim.exe`.
    - Copy and paste `UnityPlayer.dll` and `WinPixEventRuntime.dll`
      within `<ValheimGameFolder>`
    - Copy `managed` and `resource` folder from `<C:\Program
      Files\Unity\Hub\Editor\2022.3.17f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono\data\`
      into the `valheim_Data` folder and overwrite all contents.
4. Go into `boot.config` and append
   ```boot.config
   player-connection-debug=1
   ```
    - To wait for debugger connection add `wait-for-managed-debugger=1`

## Alternatively using the doorstop 4.x.x

- Install everything like above but skip step 4.
- Turn on debugging in doorstop.config
- Make sure doorstop.config exists within the valheim.exe folder
  IE <ValheimGameFolder> path.
- Connect to the doorstop debugger instead.