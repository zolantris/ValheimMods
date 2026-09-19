# ValheimScalability - Sector Mesh Clustering

A full scalability mod that allows users to tweak various settings to improve
performance, including but not limited to:

- Clustering Entire Zone based meshes to reduce draw calls only on areas you are
  currently in or are being interacted with.
- Increasing the speed at which a zone instantiates all of its meshes and
  objects.

## Background

This project comes from ValheimRAFT/ValheimVehicles and other related tweaks to
make vehicles and massive bases spawn faster and be more efficient on graphical
performance. The goal is to make Valheim run smoother on lower-end systems while
maintaining the core gameplay experience.

## The important lifecycle behavior is:

1. ZNetScene.AddInstance registers network/ZDO objects by exact ZDO sector.
2. PokeLocalZone scans a newly-created zone root once for non-network static
   renderers.
3. SpawnProxyLocation scans a client location hierarchy once.
4. Near + camera-visible sectors show originals and disable combined meshes.
5. Far/not-visible sectors use the combined sector mesh.
6. First damage immediately restores originals, permanently excludes that root
   for the
   controller lifetime, and starts a damage cooldown.
7. Repeated damage extends the cooldown; it does not rebuild every hit.
8. After the cooldown, one rebuild occurs, excluding all roots damaged during
   this
   sector-controller lifetime.

ZDO object
↓
ZNetScene.AddInstance
↓
exact ZDO sector

Non-ZDO zone object
↓
new ZoneSystem root
↓
one-time hierarchy scan

Client static location
↓
SpawnProxyLocation
↓
one-time hierarchy scan