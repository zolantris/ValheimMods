# Cannons

A guide to placing, aiming, and firing all cannon types in ValheimRAFT.

---

## Contents

- [Overview](#overview)
- [Cannon Types](#cannon-types)
- [Ammunition](#ammunition)
- [Cannon Groups](#cannon-groups)
- [Controls](#controls)
    - [Fixed Cannon via Steering Wheel](#fixed-cannon-via-steering-wheel)
    - [Fixed Cannon via Cannon Control Center](#fixed-cannon-via-cannon-control-center)
    - [Handheld Cannon](#handheld-cannon)
- [Placement Tips](#placement-tips)
- [Config](#config)
- [Further Reading](#further-reading)

---

## Overview

ValheimRAFT adds three cannon variants: a **fixed cannon** that mounts to a
vehicle or base, a **Cannon Control Center** that acts as the fire-control
station for fixed cannons off-vehicle, and a **handheld cannon** the player
carries and fires directly. Fixed cannons on a vehicle are controlled from the *
*Steering Wheel**.

---

## Cannon Types

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Name</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Icon</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="padding:8px;"><b>Cannon (Fixed)</b></td>
      <td style="padding:8px; text-align:center;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/cannon_fixed.png" width="80"/></td>
      <td style="padding:8px;">Mounts to any vehicle or base. Fired from the Steering Wheel when on a vehicle, or from the Cannon Control Center when placed off-vehicle. Added in <code>v3.6.x</code>.</td>
    </tr>
    <tr>
      <td style="padding:8px;"><b>Cannon Control Center</b></td>
      <td style="padding:8px; text-align:center;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/cannon_control_center.png" width="80"/></td>
      <td style="padding:8px;">A telescope-style controller for fixed cannons placed off-vehicle. Controls all fixed cannons within its <code>DiscoveryRadius</code> (default 15 units). Added in <code>v3.6.x</code>.</td>
    </tr>
    <tr>
      <td style="padding:8px;"><b>Cannon Turret (UNRELEASED)</b></td>
      <td style="padding:8px; text-align:center;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/cannon_turret.png" width="80"/></td>
      <td style="padding:8px;">Auto-aiming turret cannon. Acquires targets automatically within its detection range.</td>
    </tr>
  </tbody>
</table>

---

## Ammunition

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Ammo</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Icon</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="padding:8px;"><b>Cannonball (Solid)</b></td>
      <td style="padding:8px; text-align:center;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/cannon_ball_bronze.png" width="60"/></td>
      <td style="padding:8px;">Piercing impact. Penetrates prefabs based on material tier. Compatible with both handheld and fixed cannons.</td>
    </tr>
    <tr>
      <td style="padding:8px;"><b>Cannonball (Explosive)</b></td>
      <td style="padding:8px; text-align:center;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/cannon_ball_blackmetal.png" width="60"/></td>
      <td style="padding:8px;">Blackmetal cannonball. Explodes on impact delivering a powerful AoE blast. Compatible with both handheld and fixed cannons.</td>
    </tr>
  </tbody>
</table>

---

## Cannon Groups

Fixed cannons are organised into **four directional groups** based on the
orientation of the vehicle or Cannon Control Center:

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Group</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Direction</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="padding:8px;">1</td>
      <td style="padding:8px;">Forward</td>
    </tr>
    <tr>
      <td style="padding:8px;">2</td>
      <td style="padding:8px;">Left (port)</td>
    </tr>
    <tr>
      <td style="padding:8px;">3</td>
      <td style="padding:8px;">Right (starboard)</td>
    </tr>
    <tr>
      <td style="padding:8px;">4</td>
      <td style="padding:8px;">Backward (aft)</td>
    </tr>
  </tbody>
</table>

Each group displays a number showing how many cannons belong to it. You cycle
between groups and fire them independently.

### Group Cycling Demo

- all cannons must be near powder barrel.
- all cannons must be near telescope (or on a vehicle)
- all cannons must have a container with cannonballs near them.

[cannons_groups_demo.mp4](https://raw.githubusercontent.com/zolantris/ValheimMods/chore/documentation-improvements/src/ValheimRAFT/docs/assets/cannons_groups_demo.mp4)

## Controls

### Fixed Cannon via Steering Wheel

Board the vehicle and take the helm (**E** on the Steering Wheel). All fixed
cannons on the vehicle are now under your control via the group system.

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Input</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Action</th>
    </tr>
  </thead>
  <tbody>
    <tr><td style="padding:8px;"><b>Hold Block</b></td><td style="padding:8px;">Activates cannon controls mode</td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + W</b></td><td style="padding:8px;">Tilt active group barrels <b>down</b></td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + S</b></td><td style="padding:8px;">Tilt active group barrels <b>up</b></td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + A</b></td><td style="padding:8px;">Cycle active group <b>backward</b></td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + D</b></td><td style="padding:8px;">Cycle active group <b>forward</b></td></tr>
    <tr><td style="padding:8px;"><b>Tap Block</b> (no hold)</td><td style="padding:8px;"><b>Fire</b> the active group</td></tr>
  </tbody>
</table>

> Firing triggers cannons in sequence with a configurable delay between each
> shot (`Cannon_FiringDelayPerCannon`, default 0.1 s).

---

### Fixed Cannon via Cannon Control Center

The **Cannon Control Center** (telescope) controls all fixed cannons within its
discovery radius when placed off-vehicle. Interact with it (**E**) to take
control. The same hotkeys as the Steering Wheel apply.

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Input</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Action</th>
    </tr>
  </thead>
  <tbody>
    <tr><td style="padding:8px;"><b>Hold Block</b></td><td style="padding:8px;">Activates cannon controls mode</td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + W</b></td><td style="padding:8px;">Tilt active group barrels <b>down</b></td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + S</b></td><td style="padding:8px;">Tilt active group barrels <b>up</b></td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + A</b></td><td style="padding:8px;">Cycle active group <b>backward</b></td></tr>
    <tr><td style="padding:8px;"><b>Hold Block + D</b></td><td style="padding:8px;">Cycle active group <b>forward</b></td></tr>
    <tr><td style="padding:8px;"><b>Tap Block</b> (no hold)</td><td style="padding:8px;"><b>Fire</b> the active group</td></tr>
  </tbody>
</table>

> The direction the Control Center faces determines which group is "forward".
> Rotate it during placement to align groups with your intended firing arcs.

> Only one Cannon Control Center can be active within its discovery radius.
> Placing a second one too close will be blocked.

---

### Handheld Cannon

Equip the Handheld Cannon as a weapon. It auto-aims at targets using the same
system as a vanilla auto-turret.

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Input</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Action</th>
    </tr>
  </thead>
  <tbody>
    <tr><td style="padding:8px;"><b>Primary Attack</b></td><td style="padding:8px;">Fire</td></tr>
    <tr><td style="padding:8px;"><b>Reload</b> (automatic)</td><td style="padding:8px;">Reloads after each shot. Default reload time: 6 s</td></tr>
  </tbody>
</table>

---

## Placement Tips

- Place fixed cannons **perpendicular to the hull** for broad-side firing arcs —
  the group system will sort them into the correct left/right groups
  automatically based on their facing direction relative to the Wheel or Control
  Center.
- Cannons facing within ±45° of the vehicle forward direction are assigned to
  the **forward** group; within ±45° of backward to the **aft** group;
  left/right flanks get their respective groups.
- Tilt angles are clamped by `CannonBarrelAimMinTiltRotation` /
  `CannonBarrelAimMaxTiltRotation` (default ±180°). Narrow these in config if
  you want limited elevation on deck-mounted guns.
- The **player protection range** (`CannonPlayerProtectionRange`, default 15
  units) prevents cannons from firing when a friendly player is inside that
  radius of the muzzle.

---

## Config

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Setting</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Default</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Description</th>
    </tr>
  </thead>
  <tbody>
    <tr><td style="padding:8px;"><code>Cannon_FireVelocity</code></td><td style="padding:8px;">90</td><td style="padding:8px;">Muzzle velocity of fired cannonballs.</td></tr>
    <tr><td style="padding:8px;"><code>Cannon_FiringDelayPerCannon</code></td><td style="padding:8px;">0.1 s</td><td style="padding:8px;">Delay between each cannon firing in a group salvo.</td></tr>
    <tr><td style="padding:8px;"><code>Cannon_ReloadTime</code></td><td style="padding:8px;">6 s</td><td style="padding:8px;">Reload time for fixed cannons. Range: 0.1 – 60 s.</td></tr>
    <tr><td style="padding:8px;"><code>CannonAutoAimSpeed</code></td><td style="padding:8px;">10</td><td style="padding:8px;">How fast cannons track and fire at targets. Lower values may miss fast-moving targets.</td></tr>
    <tr><td style="padding:8px;"><code>CannonAutoAimYOffset</code></td><td style="padding:8px;">1</td><td style="padding:8px;">Vertical aim offset (0 = dead center, 1 = aim toward top of target). Compensates for gravity drop.</td></tr>
    <tr><td style="padding:8px;"><code>CannonAimMaxYRotation</code></td><td style="padding:8px;">15°</td><td style="padding:8px;">Maximum horizontal sweep angle per cannon barrel.</td></tr>
    <tr><td style="padding:8px;"><code>CannonBarrelAimMaxTiltRotation</code></td><td style="padding:8px;">180°</td><td style="padding:8px;">Maximum upward barrel elevation.</td></tr>
    <tr><td style="padding:8px;"><code>CannonBarrelAimMinTiltRotation</code></td><td style="padding:8px;">-180°</td><td style="padding:8px;">Maximum downward barrel depression.</td></tr>
    <tr><td style="padding:8px;"><code>CannonPlayerProtectionRange</code></td><td style="padding:8px;">15 units</td><td style="padding:8px;">Cannons will not fire if a friendly player is within this radius of the muzzle.</td></tr>
    <tr><td style="padding:8px;"><code>CannonTiltAdjustSpeed</code></td><td style="padding:8px;">0.5</td><td style="padding:8px;">Speed of manual barrel tilt via Block+W/S. 0% = 10× slower than 100%.</td></tr>
    <tr><td style="padding:8px;"><code>DiscoveryRadius</code> (Control Center)</td><td style="padding:8px;">15 units</td><td style="padding:8px;">Radius in which a single Cannon Control Center manages all fixed cannons.</td></tr>
    <tr><td style="padding:8px;"><code>Cannon_HasFireAudio</code></td><td style="padding:8px;">true</td><td style="padding:8px;">Toggle cannon fire sound effects.</td></tr>
    <tr><td style="padding:8px;"><code>Cannon_FireAudioVolume</code></td><td style="padding:8px;">1</td><td style="padding:8px;">Volume of cannon fire audio.</td></tr>
    <tr><td style="padding:8px;"><code>Cannon_ReloadAudioVolume</code></td><td style="padding:8px;">1</td><td style="padding:8px;">Volume of cannon reload audio.</td></tr>
  </tbody>
</table>

---

## Further Reading

- [Basics Tutorial](./Tutorial_Basics.md) — building your first vehicle
- [Full config reference](https://github.com/zolantris/ValheimMods/blob/main/src/ValheimRAFT/docs/ValheimRAFT_AutoDoc.md) —
  all config options

