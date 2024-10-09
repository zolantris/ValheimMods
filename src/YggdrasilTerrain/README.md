# Yggdrasil Terrain

A terrain mod allowing for physical access and building on the Yggdrasil branch
in the sky.

## Features

- makes the yggdrasil branch physical
- Support player walking on the root
- Building type collisions.
- Building can be built on the Yggdrasil root.
- Vehicle collisions,
    - carts and other vanilla pieces
    - raft vehicles (requires custom rudder config values. Set the base speeds
      to extreme numbers. Requires flying the raft to the Yggdrassil branch in
      the
      current raft mod).
- Allows for collision overrides via config.

## **LayerTypes**

The mod defaults to overriding the branch layer to `"piece"` E.G. layer `10`.
This layer will not spawn
anything, but will allow building. Spawning using layer terrain will cause tons
of issues...other layers may also cause these problems.

Original layer is `"skybox"` but things cannot be built/collide when player is
placing items with skybox layers. Overriding this collision logic seems
dangerous + prone to mod incompatibility so this has not been done.

## Layer Overrides via config

- `terrain` EG layer `11`
    - :warning: As MAJOR GAME-BREAKING sideffect :warning:
        - it adds huge columns of rock that reach the yggdrassil branch.
          This can be
          immersion breaking for MOST people and likely breaks other
          features of the game.
        - The columns issue appears on all biomes. May
          cause FPS drops. Use
          terrain (11) at your own risk.
    - Allows for spawning
      yggdrasil roots and other biome specific material. E.G. mistlands.
      If
      the root is over meadows it would spawn mushrooms etc.
    - To disable spawns swap the layer to 10 which is the Piece layer. Requires
      the Layer.ModOverridesEnabled = true to be set.
    - Having layer 11 and loading parts of the map under the root could
      permenantly affect terrain loads. Which could impact game
      performance...BEWARE!

## About the Yggdrasil Branch

The branch location xyz ranges useful for using `tp`

- x = -3652 to 10000 (at least maybe up to 11000)
- y= 2118.809 +- 1000 likely, will drop off the world at much lower values
- z = +-500

There is a command for teleporting to a fixed location on the branch. Should be
the same across all worlds. Same command is `tp <name> x,z,y` format
`tp <character_name> 9947.041,500.9553,2118.809`

## Config Commands

- `yggdrasilTerrain teleport` requires the user to be an admin in order to
  teleport to the yggdrasil root directly.

## Possible Future plans

- improve teleportation based on location on map and pinpoint the accuracy so
  player is set directly on the root without falling