you might wanna make sure that you have a clean valheim installation first. easiest way to achieve this is by renaming the current valheim installation and then executing the file integrity check in steam.
for the dev build:
first things first: head to the new valheim installation and delete the valheim.exe. then head to your unity hub installation, it's probably in program files/unity/hub. don't confuse it with program files/unity hub.
all the way in C:\Program Files\Unity\Hub\Editor\2022.3.17f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono, there is a WindowsPlayer.exe. copy that into the valheim folder and rename it to valheim.exe
then copy the UnityPlayer.dll and the WinPixEventRuntime.dll into the same folder, replacing the existing files.
in your unity installation, there is also a Data folder and inside of that, you will find a managed and a resource folder. copy both of these folders into the valheim_Data folder in your valheim installation, replacing all files.
inside the valheim_Data folder, there is a boot.config file. open that in any editor and append the following two lines:

```boot.config
player-connection-debug=1
wait-for-managed-debugger=1
```

save the file and you are done. you can start valheim to check, if that worked. if it did, it will they development build in the bottom right corner of the start menu.
if everything is fine, just install bepinex and your mods. start up the game again and check for any errors in your mods. as i said, the dev build is way more strict. fix it, if it finds anything. done.
