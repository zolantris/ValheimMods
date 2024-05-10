# Testing ValheimRAFT / Vehicles

## Contents

## Steps

### Sail
CheckList
- [ ] Ropes
- [ ] Ropes attach to prefabs after clicking the rope and the target prefab
- [ ] Ropes de-attach to prefabs after clicking the rope and the target again.
- [ ] Ropes do not

## Prefabs

### ShipWheel

### ShipRudder

- [ ] The rudder can rotate based on the wheel
- [ ] The rudder moves the ship wake effects to it's location.

### Colliders

- Placing a prefab on the raft expands the colliders correctly
    - [ ] `ExactPrefabBounds=false` colliders should expand to the center of the prefab. Likely the mesh center, but some prefabs have a typical centers.
    - [ ] `ExactPrefabBounds=true` is enabled the prefab should be fully within the collider. The size should be near equal to the ship. If it is much larger, there is an issue.
- [ ] If `HullCollisionOnly` the Float and the Blocking colliders will only expand if a Hull prefab is placed.

### Sails

- Placing a custom sail should render the sail material clearly on one side and opaque see through on the other side.
- The sail should be customizable with a logo
- The sail should:
  - allow raising
  - allow rotation
  - 
- [ ] The Raft renders
- [ ] The Raft renders
- [ ] The Raft renders
- [ ] The Raft renders
- [ ] The Raft renders