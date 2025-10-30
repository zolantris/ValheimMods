# Eldritch Valheim

Eldritch Valheim is a mod for the game Valheim that introduces eldritch-themed
content, including new creatures, items, and biomes inspired by cosmic horror
and Lovecraftian lore.

This has not been tested in multiplayer, but it should work.

This mod is meant
for Valheim but Eldritch.Core is meant for integration into other Unity games (
and maybe my own game eventually).

## Tutorial

1. Install the mod, then start them game
2. To edit how xenos spawn you can open the `EldritchValheim.dll.config` file in
   a
   text editor or using bepinexconfiguration manager.
3. Most of the configuration is server synced - IT WILL REQUIRE a server restart
   to apply config updates for now.

## Version

- 1.0.0: Initial release with Xenomorphs and basic eldritch-themed content. (The
  content works, but is limited in scope and polish. Future updates will expand
  and refine the mod.)

## Installation

1. Ensure you have Valheim installed and updated to the latest version.
2. Download the Eldritch Valheim mod from the official repository or modding
   community.
3. Extract the contents of the downloaded file into your Valheim
   `BepInEx/plugins` directory.
4. Launch Valheim and enjoy the new eldritch content (must be launch via
   R2modman or doorstop commands).

## Features

Xenomorphs: Introduces terrifying alien creatures that roam the world, adding a
new layer of challenge and horror. The AI currently is buggy for movement so it
currently uses the vanilla AI (making the Xeno less realistic).

### Monsters

<!-- Replaced the previous bullet list with a structured HTML table for clarity and to include spawn IDs -->

<table>
  <thead>
    <tr>
      <th>Image</th>
      <th>Name</th>
      <th>ID</th>
      <th>Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/Eldritch.Valheim/Icons/xeno-drone.png" alt="Xenomorph Drone" style="max-width:120px; height:auto;"/></td>
      <td>Xenomorph Drone</td>
      <td><code>xeno_drone</code></td>
      <td>Fast and agile pack hunters. Stealthy predators that can ambush players. Weakness: Fire (planned). Behaviors: hunt, leap, charge, retreat, creep. (Movement/nav AI limited in 1.0.0)</td>
    </tr>
    <tr>
      <td><img src="assets/images/example_monster.png" alt="Example Monster" style="max-width:120px; height:auto;"/></td>
      <td>Example Monster</td>
      <td><code>example_monster</code></td>
      <td>Placeholder entry. Add a short description and use the ID for spawning and config references.</td>
    </tr>
  </tbody>
</table>

<p>
The values in the <code>ID</code> column are the canonical spawn identifiers used by the mod (for example in spawn commands, config files, or spawn tables). Use these IDs when spawning monsters via console commands or programmatic spawners.
</p>

<p>
Image notes: place small PNG/JPG images in `src/Eldritch.Valheim/assets/images/` (or update the paths above). Recommended width: 120px (use responsive styling as above). If images are not present, the <code>&lt;img&gt;</code> tags will show broken-image placeholders in GitHub/Markdown viewers.
</p>

## Permissions / Legal

Some of this content must remain free (EG some assets). Most code/content from
Eldritch.Core is only available to use with permission from the author as _some
of_ this code may be introduced in paid content/games.

Do not sell or redistribute this mod or its
assets without permission from the owner (and the IP of any content)