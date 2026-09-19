# ValheimScalability

**ValheimScalability** is a client-side performance mod for Valheim focused on
improving rendering performance in large builds, dense settlements, and heavily
populated world areas.

The mod reduces the number of individual objects that need to be rendered by
grouping compatible static meshes into larger render batches. It includes
safeguards for animated, interactive, destructible, modded, physics-driven, and
wind-affected objects so they can remain independent when needed.

> **Let Valheim render more without requiring players to build less.**

---

## Features

### Sector Mesh Clustering

ValheimScalability groups compatible static meshes within loaded world sectors
into larger combined meshes.

This can significantly reduce renderer and draw-call overhead in areas
containing large numbers of:

- Building pieces
- Walls
- Floors
- Roof pieces
- Static decorations
- Rocks and other compatible world geometry
- Other static meshes added by mods

Original gameplay objects remain loaded and functional. ValheimScalability
changes how compatible geometry is rendered; it does not remove the underlying
`Piece`, `WearNTear`, `ZNetView`, physics, collider, or other gameplay
components.

### Adaptive Rendering

In `Adaptive` mode, distant cells use clustered rendering while nearby visible
cells return to their original renderers.

This provides a compatibility-oriented balance between normal interaction and
lower draw-call overhead.

### Full Cluster Rendering

`FullCluster` keeps compatible geometry clustered regardless of player distance.

This generally provides the strongest draw-call reduction and is especially
useful for benchmarking and extremely large builds.

### Easy A/B Performance Testing

Set:

```ini
Mode = Off
```

to restore normal object rendering without uninstalling the mod.

This makes it easy to compare the same location with clustering enabled and
disabled.

---

## Rendering Modes

| Mode          | Behavior                                                                                     | Recommended Use                                           |
|---------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------------|
| `Off`         | Original Valheim renderers only. Generated cluster rendering is not presented or rebuilt.    | Baseline FPS comparisons, troubleshooting                 |
| `Adaptive`    | Nearby visible cells use originals; farther cells use clustered rendering.                   | General gameplay                                          |
| `FullCluster` | Uses clustered rendering everywhere it is safe; player proximity does not restore originals. | Maximum performance, very large settlements, benchmarking |

---

## Installation

Install ValheimScalability like a normal BepInEx Valheim mod.

### Thunderstore / Mod Manager

Install the mod through your preferred Thunderstore-compatible mod manager.

### Manual Installation

Copy the ValheimScalability plugin into your Valheim BepInEx plugins directory:

```text
Valheim/
└── BepInEx/
    └── plugins/
        └── ValheimScalability/
```

Launch the game once to generate the configuration file under:

```text
BepInEx/config/
```

---

## Client-Side Mod

ValheimScalability is a **client-side rendering optimization**.

It does not change:

- World data
- Building placement
- Damage values
- Networking ownership
- Saved objects
- Server-authoritative gameplay state

Dedicated servers do not need to run the rendering system. Each player can
configure the rendering behavior for their own client.

---

## Recommended Profiles

| Profile             | Mode          | Cell Divisions | Lowest LOD | MaterialPropertyBlocks | Notes                                                  |
|---------------------|---------------|---------------:|------------|------------------------|--------------------------------------------------------|
| General gameplay    | `Adaptive`    |            `1` | `true`     | Skip                   | Recommended starting point                             |
| Maximum performance | `FullCluster` |            `1` | `false`    | Skip                   | Strongest batching and easiest benchmark configuration |
| Baseline / disabled | `Off`         |            `1` | N/A        | N/A                    | Compare against normal rendering                       |

`CellDivisionsPerSector = 1` is the default because it produced the strongest
real-world gains during testing. Higher values trade batching efficiency for
finer Adaptive-mode proximity and culling granularity.

---

# Configuration Reference

## Sector Clustering

Section:

```ini
[Rendering.SectorClustering]
```

| Setting                              | Default    | Range / Values                   | Description                                                                                                                            |
|--------------------------------------|------------|----------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| `Mode`                               | `Adaptive` | `Off`, `Adaptive`, `FullCluster` | Selects whether clustering is disabled, proximity-aware, or always active where safe.                                                  |
| `CellDivisionsPerSector`             | `1`        | `1-8`                            | Divides each Valheim sector into smaller cluster cells. `1` produces the largest batches and is recommended by default.                |
| `PlayerUnclusterRadius`              | `18`       | `0-128`                          | Adaptive only. Cardinal/Manhattan world-space radius around the player that can restore original renderers.                            |
| `PlayerProximityPollInterval`        | `0.15`     | `0.05-2` seconds                 | Frequency of Adaptive-mode proximity checks.                                                                                           |
| `RequireCameraVisibilityToUncluster` | `true`     | Boolean                          | Adaptive only. Nearby cells are restored only when also visible to the local camera.                                                   |
| `AlwaysClusterPieceLayerNearPlayer`  | `false`    | Boolean                          | Experimental. Keeps eligible `piece` layer rendering clustered even near the player.                                                   |
| `FullClusterUsesSingleCellPerSector` | `true`     | Boolean                          | Forces one cell per sector in FullCluster to minimize batch fragmentation.                                                             |
| `MaintenanceInterval`                | `0.20`     | `0.05-2` seconds                 | Interval for build/rebuild/cleanup maintenance.                                                                                        |
| `InitialBuildDelay`                  | `0.75`     | `0-5` seconds                    | Delay before the first cluster build for a new cell.                                                                                   |
| `RegistrationSettleDelay`            | `0.35`     | `0.05-2` seconds                 | Debounce after new objects register before rebuilding.                                                                                 |
| `RemovalRebuildDelay`                | `0.20`     | `0-2` seconds                    | Debounce after previously clustered geometry disappears.                                                                               |
| `DamageCooldown`                     | `8`        | `0-60` seconds                   | Time after the latest damage before an affected cell may rebuild.                                                                      |
| `VisibilityHeight`                   | `2048`     | `64-8192`                        | Vertical size used for cell camera-frustum checks.                                                                                     |
| `UseLowestLod`                       | `true`     | Boolean                          | Uses the lowest available LOD renderer when building optimized representations.                                                        |
| `SkipMaterialPropertyBlockRenderers` | `true`     | Boolean                          | Recommended. Leaves renderers using MaterialPropertyBlocks original because arbitrary per-object shader state cannot be safely merged. |
| `MinimumEstimatedDrawCallSavings`    | `1`        | `1-1024`                         | Requires at least this estimated draw-call saving before using an optimized representation.                                            |
| `LogBatchDiagnostics`                | `false`    | Boolean                          | Logs generated batch keys, instance counts, and presentation-state diagnostics.                                                        |

### Cell Division Tradeoff

| `CellDivisionsPerSector` | Cells per Sector | Batching        | Culling / Adaptive Granularity |
|-------------------------:|-----------------:|-----------------|--------------------------------|
|                      `1` |                1 | Best            | Coarsest                       |
|                      `2` |                4 | Very good       | Better                         |
|                      `4` |               16 | More fragmented | Fine                           |
|                      `8` |               64 | Most fragmented | Finest                         |

For most users, leave this at `1`. Increase it only if you specifically prefer
finer Adaptive transitions or culling granularity and are willing to trade some
batching efficiency.

---

## Object, Layer, Material, and Shader Filters

Section:

```ini
[Rendering.SectorClustering.Filters]
```

| Setting                     | Default                                                                                                                           | Description                                                                                                   |
|-----------------------------|-----------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| `IncludedLayers`            | `Default,static_solid,Default_small,piece`                                                                                        | Comma-separated layer names or numeric layer IDs eligible for batching. Empty means all layers.               |
| `ExcludedLayers`            | *(empty)*                                                                                                                         | Layers that must remain original. Exclusions override includes.                                               |
| `IncludedObjectNames`       | *(empty)*                                                                                                                         | Optional comma-separated, case-insensitive object-name substrings. Empty means all object names are eligible. |
| `ExcludedObjectNames`       | `portal,door,chest,cart,wheel,mechanism,Destruction,Portal_destruction,Destruction_Cube,vehicle_water_mesh,animated,energy_level` | Object-name substrings that always remain original.                                                           |
| `IncludedObjectRegexList`   | *(empty)*                                                                                                                         | Optional semicolon-separated regular expressions for object names.                                            |
| `ExcludedObjectRegexList`   | *(empty)*                                                                                                                         | Semicolon-separated regular expressions for object names that must remain original.                           |
| `ExcludedMaterialNames`     | *(empty)*                                                                                                                         | Comma-separated material-name substrings to exclude.                                                          |
| `ExcludedMaterialRegexList` | *(empty)*                                                                                                                         | Semicolon-separated material-name regular expressions to exclude.                                             |
| `ExcludedShaderNames`       | *(empty)*                                                                                                                         | Comma-separated shader-name substrings to exclude.                                                            |
| `ExcludedShaderRegexList`   | *(empty)*                                                                                                                         | Semicolon-separated shader-name regular expressions to exclude.                                               |

Name filters are case-insensitive substring matches. Regex lists use semicolons
so regular expressions can contain commas.

For troubleshooting only player/build pieces, a useful temporary filter is:

```ini
IncludedLayers = piece
```

---

## Warm Zone Cache

Section:

```ini
[Rendering.SectorClustering.Cache]
```

ValheimScalability uses a bounded warm-zone cache so sailing or exploring across
a large world does not retain generated cluster meshes forever.

Active zones are **not** constrained by the warm-cache limits. The limits apply
only after a zone leaves the active area.

| Setting                            | Default | Range / Special Value      | Description                                                                          |
|------------------------------------|--------:|----------------------------|--------------------------------------------------------------------------------------|
| `Enabled`                          |  `true` | Boolean                    | Retains a bounded set of recently/frequently used inactive zone clusters.            |
| `MaxWarmZones`                     |    `96` | `0-4096`; `0` = unlimited  | Maximum number of inactive warm zones retained.                                      |
| `MaxWarmMeshMemoryMB`              |   `768` | `0-32768`; `0` = unlimited | Approximate generated-mesh memory budget for inactive warm zones.                    |
| `MaxSingleWarmZoneMB`              |   `256` | `0-8192`; `0` = unlimited  | Prevents one extremely large inactive zone from monopolizing the warm cache.         |
| `WarmZoneRetentionSeconds`         |   `300` | `0-86400`                  | Normal warm-zone retention time.                                                     |
| `FrequentVisitThreshold`           |     `3` | `1-1000`                   | Number of distinct visits before a zone receives frequent-zone retention.            |
| `MaxFrequentWarmZones`             |    `16` | `0-1024`                   | Maximum number of frequent inactive zones given extended retention.                  |
| `FrequentWarmZoneRetentionSeconds` |  `1800` | `0-86400`                  | Extended retention for frequently revisited bases, portal hubs, farms, docks, etc.   |
| `LogCacheDiagnostics`              | `false` | Boolean                    | Logs warm-cache entry, reactivation, eviction, and approximate retained mesh memory. |

The lifecycle is approximately:

```text
ACTIVE
  current/nearby zone
       ↓
WARM
  recent/frequent inactive zone
  bounded by memory + count + retention
       ↓
COLD
  generated cluster data destroyed
```

Players with large amounts of RAM can raise the limits if they frequently
teleport between very large builds.

---

## Trees, Vegetation, and Experimental GPU Instancing

Vegetation requires special handling because many tree and plant shaders use
per-object data for wind movement.

Naively combining multiple trees into one mesh can make the entire group sway as
one object, so wind-affected objects are **not** forced through normal static
mesh combining.

Section:

```ini
[Rendering.SectorClustering.Instancing]
```

| Setting                   | Default                                                                    | Description                                                                                          |
|---------------------------|----------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| `Enabled`                 | `false`                                                                    | Experimental manual GPU instancing path for detected vegetation/wind renderers. Disabled by default. |
| `ObjectNameHints`         | `tree,bush,sapling,beech,birch,fir,pine`                                   | Object-name hints used to classify vegetation candidates.                                            |
| `MaterialNameHints`       | `leaf,leaves,branch,foliage,tree,bush,vegetation`                          | Material-name hints used to classify vegetation candidates.                                          |
| `ShaderNameHints`         | `vegetation,tree,foliage,wind,plant`                                       | Shader-name hints used to classify vegetation candidates.                                            |
| `CandidateRegexList`      | *(empty)*                                                                  | Optional semicolon-separated regexes matched against object/material/shader descriptors.             |
| `WindShaderPropertyNames` | `_Wind,_WindStrength,_WindSpeed,_WindIntensity,_Sway,_SwaySpeed,_WindData` | Shader properties that identify wind/instancing candidates.                                          |

With experimental instancing disabled, detected wind/vegetation objects remain
normally rendered rather than being incorrectly mesh-combined.

---

## Destructible Objects

ValheimScalability tracks damage to optimized objects.

When an optimized destructible object is damaged:

1. The affected rendering cell temporarily returns to normal rendering.
2. The damaged object is excluded from future clustering for that loaded sector
   lifetime.
3. Repeated damage extends the cooldown rather than rebuilding on every hit.
4. Remaining compatible geometry can rebuild after the area has been quiet long
   enough.

This allows destructible static geometry to remain interactive without
rebuilding combined meshes on every damage event.

---

# Compatibility Matrix

ValheimScalability is intentionally conservative. If an object cannot be safely
represented by a shared static cluster mesh, it should remain on its original
renderer.

| Object / Rendering Type                            | Supported for Mesh Batching?             | Behavior / Notes                                                                                                                                                                                                            |
|----------------------------------------------------|------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Vanilla static building pieces                     | **Yes**                                  | Primary target of the mod. Compatible pieces sharing suitable rendering state are combined.                                                                                                                                 |
| Static modded building pieces                      | **Usually**                              | Supported when they use compatible `MeshRenderer` / `MeshFilter` geometry and pass configured filters.                                                                                                                      |
| Static rocks / world geometry                      | **Usually**                              | Eligible when static and using supported materials/shaders.                                                                                                                                                                 |
| Destructible static objects                        | **Yes, with safeguards**                 | Damage restores the affected rendering cell and excludes the damaged object from subsequent batching for that loaded-sector lifetime.                                                                                       |
| `LODGroup` objects                                 | **Yes**                                  | ValheimScalability selects an eligible LOD renderer; `UseLowestLod` controls whether the lowest available LOD is preferred.                                                                                                 |
| `MeshRenderer` + `MeshFilter`                      | **Yes**                                  | Main supported rendering path.                                                                                                                                                                                              |
| `SkinnedMeshRenderer`                              | **No**                                   | Not static mesh geometry and is left on its original renderer.                                                                                                                                                              |
| Animator-driven / animated objects                 | **No / excluded**                        | Animation requires independent transforms/state and should remain original.                                                                                                                                                 |
| Characters / creatures / players                   | **No**                                   | Explicitly excluded from clustering.                                                                                                                                                                                        |
| Moving / non-kinematic `Rigidbody` objects         | **No**                                   | A root under a non-kinematic `Rigidbody` is rejected. One combined world mesh cannot safely follow independently moving physics objects.                                                                                    |
| Internal meshes under a moving Rigidbody hierarchy | **No**                                   | Intentionally not batched by ValheimScalability. This includes interiors/components whose parent hierarchy is physically moving.                                                                                            |
| Kinematic Rigidbody hierarchy                      | **Potentially**                          | May be eligible if otherwise static and compatible, because only non-kinematic Rigidbody hierarchies are automatically rejected.                                                                                            |
| Doors, chests, portals, carts, wheels, mechanisms  | **Normally no**                          | Common interactive names are excluded by default. Users can customize filters.                                                                                                                                              |
| MaterialPropertyBlock renderers                    | **Normally no**                          | Skipped by default because per-renderer shader state cannot be safely reconstructed generically.                                                                                                                            |
| Wind-animated trees / vegetation                   | **Not with static mesh combining**       | Left original by default. Experimental GPU instancing is available separately.                                                                                                                                              |
| Custom / unusual mod shaders                       | **Best effort**                          | Can be excluded by object, material, shader name, layer, or regular expression if needed.                                                                                                                                   |
| ValheimRAFT vehicles and vehicle internals         | **Handled by ValheimRAFT**               | ValheimScalability deliberately avoids moving Rigidbody hierarchies. ValheimRAFT already has its own vehicle-internal piece clustering/rendering optimizations, so the two systems do not need to compete for those meshes. |
| Other vehicle / moving-base mods                   | **Not automatically batched internally** | Their moving Rigidbody hierarchy should remain original unless that mod provides its own safe vehicle-local batching system.                                                                                                |

### Why Rigidbody Internals Are Not Combined

A combined mesh created by ValheimScalability represents static world-space
geometry for a sector/cell.

If the source hierarchy is driven by a non-kinematic Rigidbody, combining its
internal renderers into a static sector mesh would separate the visuals from the
physics object as soon as it moved.

For this reason, ValheimScalability rejects roots that have a non-kinematic
`Rigidbody` in their parent hierarchy.

This is particularly important for ships, vehicles, physics contraptions, and
moving bases.

### ValheimRAFT

ValheimRAFT is a special case because it already manages optimization of pieces
attached to its moving vehicle hierarchy.

ValheimScalability focuses on **world-sector rendering**, while ValheimRAFT
handles **vehicle-local rendering**. ValheimScalability therefore does not need
to mesh-batch the internal pieces of a moving ValheimRAFT vehicle.

---

## Troubleshooting

### An object disappears or looks incorrect

Add part of its object name to `ExcludedObjectNames`, or exclude its
material/shader if the entire rendering type is affected.

### A modded animated object becomes static

Exclude the object, material, or shader. Some mods animate meshes using systems
that cannot be generically detected.

### Trees move strangely

Keep experimental instancing disabled and make sure vegetation is not being
forced through static mesh combining:

```ini
[Rendering.SectorClustering.Instancing]
Enabled = false
```

### Performance becomes worse

Start with:

```ini
Mode = FullCluster
CellDivisionsPerSector = 1
```

For building-only testing:

```ini
IncludedLayers = piece
```

Then compare the same location against:

```ini
Mode = Off
```

### Old config options are still visible

BepInEx does not automatically remove obsolete config entries.

Older versions may have created settings such as:

```text
Enabled
PresentationMode
NearPlayerSectorRadius
VisibilityUpdateInterval
```

Delete the ValheimScalability config file and relaunch if you want a clean
regenerated configuration.

---

## Performance Notes

ValheimScalability primarily targets **rendering overhead**.

It can reduce the cost of rendering thousands of individual static objects, but
it does not remove the underlying gameplay objects.

Systems such as:

- `Piece`
- `WearNTear`
- `ZNetView`
- Physics
- Colliders
- Structural support calculations
- AI
- Scripts from other mods

can still consume CPU time even when their visible meshes are combined.

As a result, performance improvements depend on whether the area is
rendering-limited or simulation-limited.

---

# Real-World Performance Benchmarks

The results below are from actual in-game testing and show the type of
improvement ValheimScalability can provide in rendering-heavy areas.

Performance will vary with build density, visible pieces, materials/shaders,
other mods, view distance, render resolution, and CPU/GPU limits.

## Test System

| Component | Hardware                              |
|-----------|---------------------------------------|
| CPU       | Intel Core i9-10900K                  |
| GPU       | NVIDIA GeForce RTX 3090 Ti            |
| Memory    | 128 GB DDR4                           |
| Display   | Samsung Odyssey G9, 7680x2160, 120 Hz |

> The display supports 7680x2160, but the exact in-game render resolution used
> for these benchmark passes was not recorded.

## Dense Settlement / Large Build

This test used an existing save containing a heavily populated player-built area
with a large number of building pieces.

| Rendering Mode                 |      Observed FPS | Notes                                                                       |
|--------------------------------|------------------:|-----------------------------------------------------------------------------|
| Normal / unoptimized rendering |           ~14 FPS | Same high-density settlement before effective clustering                    |
| `FullCluster`                  |        ~36-60 FPS | `CellDivisionsPerSector = 1`; large reduction in individual rendered meshes |
| `Adaptive`                     | Benchmark pending | Depends on player position, radius, visibility, and clustering granularity  |

The measured `FullCluster` result represents roughly a **2.6x to 4.3x increase
in frame rate** compared with the ~14 FPS baseline in this specific test.

## Light Meadow / Small Build

A near-empty Meadows area with a small wooden structure was also used while
tuning cluster granularity.

| Configuration                   | Observed FPS |
|---------------------------------|-------------:|
| Earlier clustered configuration |      ~60 FPS |
| `CellDivisionsPerSector = 1`    |     ~100 FPS |

This was not a clean vanilla-vs-mod benchmark. It demonstrates that excessive
cluster subdivision can create enough additional render batches and management
overhead to reduce the benefit of mesh combining.

## Reproducing the Benchmark

For a useful comparison:

1. Stand in the same location and face the same direction.
2. Wait several seconds for the area to finish loading.
3. Record FPS with `Mode = Off`.
4. Record FPS with `Mode = FullCluster` and `CellDivisionsPerSector = 1`.
5. Optionally compare `Mode = Adaptive`.

For Adaptive results, also report `PlayerUnclusterRadius`,
`RequireCameraVisibilityToUncluster`, and `CellDivisionsPerSector`.

---

## Reporting Problems

When reporting a rendering compatibility issue, please include:

- ValheimScalability version
- Valheim version
- Affected prefab/object name
- Mod that added the object, if applicable
- Your `Rendering.SectorClustering` configuration
- Screenshots showing the problem
- Whether the problem disappears with `Mode = Off`

For performance reports, include FPS from the same location with both
`Mode = Off` and `Mode = FullCluster`.

---

## GitHub

This project can be found at:

https://github.com/zolantris/ValheimMods/tree/main/src/ValheimScalability

---

## License

See the repository license for details.
