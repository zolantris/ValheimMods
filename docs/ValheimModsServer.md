# Server Information

## Mods

- Server dev commands - (for speeding up things)
- VNEI - to quickly add items
- CustomSeed - to get a good spawn location when regenerating worlds. Example
  seed with immediate water near spawn. `DivyagCimv`

## Setup Commands

- Configure the command. And add the password to __quickconnect__ mod. Add a DNS
  route
  to avoid having to add more IPs. DNS routes will resolve to the dynamic ip
  that your ISP sets
  Windows .bat command

```bat
valheim_server -nographics -batchmode -name "ZolVehicles" -port 2456 -world "ZolVehicles" -password "<password>"
```

## Multiple Accounts

Use sandboxie with another steam account to be able to launch multiple instances
of a game with different profiles without steam freaking out.

**Steam is going to fight you if you allow file access so follow step 1 to the
letter.**

1. Select container that disables access to files
    - IE Do not let any files be copied / synced from the main machine to the
      sandbox. Syncing steam files from Main -> Sandbox breaks steam quickly and
      causes collisions.
2. Copy both R2ModMan and Steam installers into the User/current drive.
3. Steam. Install Valheim.
4. After installation go into steam and set all the files right click
   Properties. Then Configuration > Check run as administrator for all steam
   files
    - This includes bin files inside steam
5. Install R2ModMan. But you will realize it cannot execute valheim correctly.
6. Manually execute by running an admin script + powershell.
    ```powershell
    C:\steam\steamapps\common\Valheim\valheim.exe -console --doorstop-enable true --doorstop-target C:\Users\<your_username>\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\ashlands\BepInEx\core\BepInEx.Preloader.dll
    ```

Create script within the sandbox that does this quickly. You can do this by
running

```powershell
new-item -path ./ -name runValheimModded.ps1 -value "C:\steam\steamapps\common\Valheim\valheim.exe -console --doorstop-enable true --doorstop-target C:\Users\<your_username>\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\ashlands\BepInEx\core\BepInEx.Preloader.dll"
```

You should now be able to launch both client + server + a VM client or even
another server with this approach.