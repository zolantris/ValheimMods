# ValheimScalability

**ValheimScalability** is a client-side performance mod for Valheim focused on
improving rendering performance in large builds, dense settlements, and heavily
populated world areas.

The mod reduces the number of individual objects that need to be rendered by
grouping compatible static meshes into larger render batches. It also includes
safeguards for animated, interactive, destructible, modded, and wind-affected
objects so they can remain independent when needed.

The goal is simple:

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

Original gameplay objects remain loaded and functional. ValheimScalability only
changes how compatible geometry is rendered.

### Sub-Sector Rendering

Sectors can be divided into smaller rendering cells.

This allows the mod to restore normal object rendering close to the player while
still using optimized rendering farther away.

```ini
CellDivisionsPerSector = 4
```

Higher values create smaller cells and more precise transitions, but also create
more render batches.

For maximum performance, especially when using `FullCluster`, a value of `1` is
recommended.

### Adaptive Rendering

In Adaptive mode, optimized meshes are used farther from the player while nearby
areas return to normal Valheim rendering.

```ini
Mode = Adaptive
```

This is intended to balance compatibility and performance.

### Full Cluster Rendering

FullCluster mode keeps compatible geometry optimized regardless of player
distance.

```ini
Mode = FullCluster
```

This is the best mode for testing maximum performance and can be especially
useful around very large player-built structures.

### Easy Performance Comparison

You can completely disable cluster rendering without uninstalling the mod:

```ini
Mode = Off
```

This restores normal Valheim rendering while keeping the mod loaded.

That makes it easy to compare FPS between:

```ini
Mode = Off
```

and:

```ini
Mode = FullCluster
```

---

## Rendering Modes

ValheimScalability has three rendering modes:

### Off

```ini
Mode = Off
```

Uses normal Valheim rendering.

No generated cluster meshes are displayed or rebuilt.

Use this mode to compare performance against vanilla rendering without removing
the mod.

### Adaptive

```ini
Mode = Adaptive
```

Nearby areas use normal object renderers.

Distant areas use optimized clustered rendering.

This is the recommended compatibility-oriented mode.

### FullCluster

```ini
Mode = FullCluster
```

Uses optimized rendering everywhere it is safe to do so.

Player proximity does not disable batching.

This mode generally provides the largest rendering-performance benefit for very
large settlements and builds.

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

Launch the game once to generate the configuration file.

The config is normally created under:

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

Dedicated servers do not need to run the rendering system.

Each player can configure the rendering behavior for their own client.

---

## Recommended Settings

For a good general-purpose starting point:

```ini
[Rendering.SectorClustering]

Mode = Adaptive

CellDivisionsPerSector = 4

PlayerUnclusterRadius = 18

RequireCameraVisibilityToUncluster = true

UseLowestLod = true

SkipMaterialPropertyBlockRenderers = true
```

For maximum performance testing:

```ini
[Rendering.SectorClustering]

Mode = FullCluster

CellDivisionsPerSector = 1

UseLowestLod = false

SkipMaterialPropertyBlockRenderers = true
```

For an unoptimized comparison:

```ini
[Rendering.SectorClustering]

Mode = Off
```

---

## Large Builds

Players with extremely large buildings or settlements may benefit most from:

```ini
Mode = FullCluster
CellDivisionsPerSector = 1
```

Using one cell per sector allows more compatible objects to be grouped together
into the same generated render batches.

Increasing `CellDivisionsPerSector` can improve transition granularity in
Adaptive mode, but it also reduces the amount of geometry that can be combined
together.

---

## Object and Layer Filtering

ValheimScalability is designed to work with modded Valheim installations.

Because other mods can introduce unusual shaders, animated objects, custom
building pieces, or special rendering behavior, the batching system can be
customized without requiring a ValheimScalability update.

### Included Layers

```ini
IncludedLayers = Default,static_solid,Default_small,piece
```

Only objects on these layers are eligible for optimized rendering.

You can also use numeric Unity layer IDs.

To test only player/building pieces:

```ini
IncludedLayers = piece
```

### Excluded Layers

```ini
ExcludedLayers =
```

Any listed layer will always remain normally rendered.

Excluded layers override included layers.

---

## Object Exclusions

Objects can be excluded by name.

```ini
ExcludedObjectNames = portal,door,chest,cart,wheel,mechanism
```

Entries are:

- Comma separated
- Case insensitive
- Partial-name matches

For example:

```ini
ExcludedObjectNames = portal
```

can match:

```text
portal_wood
StonePortal
MyModPortalLarge
```

This is useful when another mod adds an object that should never be combined.

---

## Regex Filters

Advanced users can use regular expressions.

```ini
ExcludedObjectRegexList = ^.*portal.*$;^MyMod_Animated_.*$
```

Regex entries are separated with semicolons.

There are also regex filters for:

- Included objects
- Excluded objects
- Materials
- Shaders

This allows compatibility fixes for third-party mods without waiting for a new
ValheimScalability release.

---

## Material and Shader Exclusions

Problematic materials and shaders can also be excluded.

```ini
ExcludedMaterialNames =
ExcludedMaterialRegexList =

ExcludedShaderNames =
ExcludedShaderRegexList =
```

If a modded object looks incorrect while clustered, excluding its shader or
material is often the easiest compatibility solution.

---

## Trees, Vegetation, and Wind Animation

Vegetation requires special handling because many Valheim tree and plant shaders
use per-object information for wind movement.

Naively combining multiple trees into one mesh can make the entire group sway as
if it were one giant object.

ValheimScalability therefore does **not** force wind-affected objects through
normal mesh combining.

An experimental GPU-instancing path is available:

```ini
[Rendering.SectorClustering.Instancing]

Enabled = false
```

It is disabled by default.

When disabled, detected wind/vegetation objects remain normally rendered rather
than being incorrectly combined.

Advanced users can customize how vegetation is detected:

```ini
ObjectNameHints = tree,bush,sapling,beech,birch,fir,pine

MaterialNameHints = leaf,leaves,branch,foliage,tree,bush,vegetation

ShaderNameHints = vegetation,tree,foliage,wind,plant
```

---

## Destructible Objects

ValheimScalability tracks damage to optimized objects.

When an optimized destructible object is damaged:

1. The affected rendering cell temporarily returns to normal rendering.
2. The damaged object is excluded from future clustering for that loaded sector
   lifetime.
3. Additional damage extends the cooldown instead of rebuilding the cluster
   every hit.
4. The remaining compatible geometry is rebuilt after the area has been quiet
   long enough.

This allows trees, rocks, building pieces, and other destructible objects to
remain interactive without rebuilding cluster meshes on every hit.

The cooldown can be adjusted:

```ini
DamageCooldown = 8
```

---

## Important Configuration Options

### CellDivisionsPerSector

```ini
CellDivisionsPerSector = 4
```

Controls how many rendering cells are created inside each Valheim sector.

Examples:

```text
1 = 1 cell
2 = 4 cells
4 = 16 cells
8 = 64 cells
```

Lower values generally create fewer, larger batches.

Higher values allow more precise Adaptive transitions but increase batching
overhead.

For `FullCluster`, use:

```ini
CellDivisionsPerSector = 1
```

unless you have a specific reason to subdivide the sector.

### PlayerUnclusterRadius

```ini
PlayerUnclusterRadius = 18
```

Only used in Adaptive mode.

Controls how close the player must be before nearby cells return to normal
object rendering.

### MinimumEstimatedDrawCallSavings

```ini
MinimumEstimatedDrawCallSavings = 1
```

Prevents the mod from replacing original renderers when doing so would not
meaningfully reduce rendering work.

### SkipMaterialPropertyBlockRenderers

```ini
SkipMaterialPropertyBlockRenderers = true
```

Recommended.

Objects using MaterialPropertyBlocks may contain per-object shader data that
cannot safely be represented by a shared combined mesh.

Keeping this enabled improves compatibility.

### UseLowestLod

```ini
UseLowestLod = true
```

Allows distant clustered geometry to use the lowest available LOD.

Disable this if you prefer full-detail geometry in clustered areas.

---

## Troubleshooting

### An object disappears or looks incorrect

Add part of its object name to:

```ini
ExcludedObjectNames =
```

For example:

```ini
ExcludedObjectNames = MyProblemObject
```

If many objects using the same material or shader are affected, use the material
or shader exclusion settings instead.

### A modded animated object becomes static

Exclude the object, material, or shader.

ValheimScalability attempts to automatically avoid animated renderers, but
modded objects can use custom animation techniques.

### Trees move strangely

Make sure vegetation is not being forced through normal static mesh combining.

The experimental instancing system can be disabled with:

```ini
[Rendering.SectorClustering.Instancing]

Enabled = false
```

### Performance becomes worse

Try:

```ini
Mode = FullCluster
CellDivisionsPerSector = 1
```

If you are testing building performance, also try:

```ini
IncludedLayers = piece
```

This isolates optimization to player/build pieces.

Then compare against:

```ini
Mode = Off
```

### I changed the config but old options are still visible

BepInEx does not automatically remove old config entries after a mod updates its
settings.

Older versions of ValheimScalability may have created options such as:

```text
Enabled
PresentationMode
NearPlayerSectorRadius
VisibilityUpdateInterval
```

These are no longer used.

Delete the ValheimScalability config file and launch the game again if you want
a clean regenerated configuration.

---

## Compatibility

ValheimScalability is designed to be conservative.

Objects are left alone when the mod cannot safely determine that they can be
optimized.

The mod avoids or can exclude:

- Characters
- Moving rigidbodies
- Animated meshes
- Interactive objects
- Objects using unsupported rendering state
- Problematic shaders
- MaterialPropertyBlock renderers
- Damaged/destructible objects
- Mod-added objects selected through config filters

Because Valheim has a large modding ecosystem, not every third-party shader or
rendering system can be automatically detected.

The configurable name, regex, layer, material, and shader filters are provided
specifically so users can resolve compatibility issues without uninstalling
either mod.

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
- Other mod scripts

can still consume CPU time even when their renderers are optimized.

As a result, performance improvements will vary depending on what is limiting
performance in a particular area.

Large construction-heavy areas are expected to benefit more than areas limited
by simulation, physics, AI, or script processing.

---

## Real-World Performance Benchmarks

The results below are from actual in-game testing and are intended to show the
type of improvement ValheimScalability can provide in rendering-heavy areas.

Performance will vary significantly depending on:

- Build density
- Number of visible pieces
- Materials and shaders in use
- Other installed mods
- View distance
- Resolution
- CPU/GPU limits
- Whether the area is rendering-limited or simulation-limited

### Test System

```text
CPU:     Intel Core i9-10900K
GPU:     NVIDIA GeForce RTX 3090 Ti
Memory:  128 GB DDR4
Display: Samsung Odyssey G9, 7680x2160, 120 Hz
```

> The display supports 7680x2160, but the exact in-game render resolution used
> for these benchmark passes was not recorded.

### Dense Settlement / Large Build Test

This test used an existing save containing a heavily populated player-built area
with a large number of building pieces.

| Rendering Mode                 |      Observed FPS | Notes                                                                            |
|--------------------------------|------------------:|----------------------------------------------------------------------------------|
| Normal / unoptimized rendering |           ~14 FPS | Same high-density settlement before effective clustering                         |
| `FullCluster`                  |        ~36-60 FPS | `CellDivisionsPerSector = 1`; large reduction in individual rendered meshes      |
| `Adaptive`                     | Benchmark pending | Will vary depending on player position, radius, visibility, and cell subdivision |

The FullCluster result represents roughly a **2.6x to 4.3x increase in frame
rate** compared with the ~14 FPS baseline in this specific test.

### Light Meadow / Small Build Test

A near-empty Meadows area with a small wooden structure was also used while
tuning cluster granularity.

```text
Earlier clustered configuration:  ~60 FPS
CellDivisionsPerSector = 1:       ~100 FPS
```

This was not a clean vanilla-vs-mod benchmark. It demonstrates the importance of
cluster granularity: excessive cell subdivision can create enough additional
render batches and management overhead to reduce the benefit of mesh combining.

### Benchmark Configuration

The strongest measured gains so far were observed with:

```ini
[Rendering.SectorClustering]

Mode = FullCluster
CellDivisionsPerSector = 1
SkipMaterialPropertyBlockRenderers = true
```

and experimental vegetation instancing disabled:

```ini
[Rendering.SectorClustering.Instancing]

Enabled = false
```

### How to Benchmark on Your Own System

For the most useful comparison:

1. Stand in the same location.
2. Face the same direction.
3. Wait several seconds for the area to finish loading.
4. Record FPS with:

```ini
Mode = Off
```

5. Then test:

```ini
Mode = FullCluster
CellDivisionsPerSector = 1
```

6. Optionally compare:

```ini
Mode = Adaptive
```

For Adaptive results, also report:

```ini
CellDivisionsPerSector =
PlayerUnclusterRadius =
RequireCameraVisibilityToUncluster =
```

because those settings directly affect how much nearby geometry returns to
normal rendering.

### Interpreting the Results

ValheimScalability primarily reduces rendering overhead.

A large improvement from `Off` to `FullCluster` usually indicates that
individual renderer/draw submission cost was a major bottleneck.

A smaller improvement can mean the area is instead limited by systems such as:

- `Piece`
- `WearNTear`
- `ZNetView`
- Physics
- Colliders
- Structural support calculations
- AI
- Scripts from other mods

Those systems continue to exist even when their visible meshes are combined.

## Reporting Problems

When reporting a rendering compatibility issue, please include:

- ValheimScalability version
- Valheim version
- The affected prefab/object name
- The mod that added the object, if applicable
- Your `Rendering.SectorClustering` config
- Screenshots showing the rendering problem
- Whether the problem disappears with:

```ini
Mode = Off
```

For performance reports, also include FPS from the same location with:

```ini
Mode = Off
```

and:

```ini
Mode = FullCluster
CellDivisionsPerSector = 1
```

This makes it much easier to determine whether the bottleneck is rendering or
another game system.

---

## License

See the repository license for details.
