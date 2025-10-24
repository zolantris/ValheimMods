# Xeno Physics Setup Guide

## Current Architecture (Single Rigidbody)

### ✅ What You Have Now:
- **Main Rigidbody**: On the root GameObject (handles all physics)
- **Visual Rigidbody**: On the "Visual" child (kinematic, for animation only)
- **Compound Colliders**: All colliders are children of the main Rigidbody
- **CollisionDelegate**: Forwards collision/trigger events from children to main AI

### Benefits:
- ✅ No physics hierarchy conflicts
- ✅ Smooth interpolation and collision detection
- ✅ Single physics authority
- ✅ Easy to manage center of mass

---

## Future: Adding Attack Hit Detection (Hands/Tail)

When you're ready to add melee attack detection, here's the recommended approach:

### Option 1: Trigger Colliders (Recommended)
**Best for**: Hit detection, damage zones, attack range

```csharp
// On hand/tail bone GameObject:
// 1. Add a collider component
// 2. Set isTrigger = true
// 3. Ensure it's a child of the main Rigidbody
// 4. Add CollisionDelegate component

// The CollisionDelegate will forward to XenoDroneAI:
public void OnTriggerEnter(Collider other)
{
    // Detect what body part hit
    if (IsAttackCollider(other))
    {
        // Apply damage to target
        var target = other.GetComponentInParent<Health>();
        if (target) target.TakeDamage(attackDamage);
    }
}

private bool IsAttackCollider(Collider col)
{
    // Check if this collider is a hand or tail
    return col.name.Contains("Hand") || col.name.Contains("Tail");
}
```

### Option 2: Animation Events + Overlap Sphere
**Best for**: Precise timing, multi-hit detection

```csharp
// Called from animation event at attack frame
public void OnAttackFrame()
{
    var handPos = animationController.rightArm.position;
    var hits = Physics.OverlapSphere(handPos, 0.5f, enemyLayer);
    
    foreach (var hit in hits)
    {
        var target = hit.GetComponent<Health>();
        if (target) target.TakeDamage(attackDamage);
    }
}
```

---

## Physics Settings Checklist

### Main Rigidbody (Root GameObject)
- ✅ Mass: 10-20 (adjust for your alien size)
- ✅ Drag: 0-2 (controls friction)
- ✅ Angular Drag: 5 (prevents spinning)
- ✅ Interpolation: Interpolate (smooth visuals)
- ✅ Collision Detection: Continuous (prevents tunneling)
- ✅ Constraints: Freeze Rotation X, Z (stays upright)

### Colliders (Body Parts)
**Movement Collider (Capsule on root):**
- isTrigger: false
- Layer: Character
- Purpose: Solid physics collision

**Attack Colliders (Hands/Tail):**
- isTrigger: true
- Layer: Character
- Enabled: Only during attack animations
- Purpose: Hit detection

**Sensor Colliders (Head for vision):**
- isTrigger: true
- Layer: Sensor
- Purpose: Detection zones

---

## Common Issues & Solutions

### Issue: Colliders not detecting triggers
**Solution**: Ensure one object has a Rigidbody (you do!), both have colliders, one is trigger

### Issue: Alien falls through floor
**Solution**: 
- Check collision layers (Project Settings > Physics)
- Ensure Continuous collision detection
- Verify ground colliders are on correct layer

### Issue: Jittery movement
**Solution**: 
- Use `Rigidbody.MoveRotation()` instead of `transform.rotation` ✅ (already done)
- Use `Time.fixedDeltaTime` in physics calculations ✅ (already done)
- Enable interpolation ✅ (already done)

### Issue: Attack colliders always active
**Solution**: Enable/disable trigger colliders via animation events:
```csharp
public void EnableHandAttackCollider() 
{
    handAttackCollider.enabled = true;
}

public void DisableHandAttackCollider() 
{
    handAttackCollider.enabled = false;
}
```

---

## Layer Setup Recommendation

Create these layers in Unity:
- **Character** - Alien body (solid collision)
- **CharacterAttack** - Attack hitboxes (trigger)
- **Enemy** - Targets
- **Ground** - Terrain/floor
- **Sensor** - Detection zones

Configure collision matrix (Edit > Project Settings > Physics):
- Character ↔ Ground: ✅ Collide
- Character ↔ Enemy: ✅ Collide
- CharacterAttack ↔ Enemy: ✅ Trigger only
- CharacterAttack ↔ CharacterAttack: ❌ Ignore

---

## Next Steps (When Ready)

1. **Add hand attack colliders**: Small spheres on hand bones, set to trigger
2. **Add tail attack collider**: Capsule on tail tip, set to trigger
3. **Implement attack detection** in `XenoDroneAI.OnTriggerEnter()`
4. **Enable/disable via animation events** for precise attack timing
5. **Add attack cooldown** to prevent multi-hits

Your foundation is solid - the single Rigidbody approach will make all of this much easier!

