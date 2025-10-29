# Animation Realism System - Architecture

## Problem: Animator vs Procedural Animation Conflicts

When working with Unity's Animator system, procedural modifications to bones
must be carefully orchestrated to avoid conflicts. The Animator evaluates
animations in a specific order, and modifying bones at the wrong time causes
jittery, broken animations.

## Solution: Unity's Animation Pipeline Order

Unity processes animations in this order each frame:

1. **Update()** - Logic updates, set Animator parameters
2. **Animator Evaluation** - Animator calculates bone positions/rotations from
   animation clips
3. **LateUpdate()** - Post-animation logic
4. **OnAnimatorIK()** - Apply IK and procedural modifications
5. **Final Rendering** - Bones are locked, rendered to screen

## Implementation

### State-Based Realism Features

We use **conditional flags** to control when each procedural effect is active:

#### 1. **Breathing** (Idle/Sleep only)

- **Active when**: Not attacking, not jumping, moveSpeed < 0.1
- **Effect**: Subtle chest expansion/contraction via `spine02.localScale`
- **Applied in**: `OnAnimatorIK()`

#### 2. **Tail Physics** (Movement only)

- **Active when**: Not jumping, moveSpeed > 0.1
- **Effect**: Spring-based physics that lags behind body movement
- **Key**: Stores animated position, then applies procedural offset
- **Applied in**: `OnAnimatorIK()`

#### 3. **Head Tracking** (Idle/Slow Movement)

- **Active when**: Not attacking, not jumping, not sleeping, moveSpeed < 0.5
- **Effect**: Smoothly rotates neck bones toward target
- **Applied in**: `OnAnimatorIK()`

### Code Structure

```csharp
private void LateUpdate()
{
    // 1. Update state flags based on animation
    UpdateRealismFeatureStates();
    
    // 2. Apply neck rotations (used by neck bones)
    if (neckPivot && neckUpDown)
    {
        neckPivot.localRotation = Quaternion.Euler(neckPivotAngle);
        neckUpDown.localRotation = Quaternion.Euler(neckUpDownAngle);
    }
}

private void UpdateRealismFeatureStates()
{
    var moveSpeed = animator.GetFloat(MoveSpeed);
    var isAttacking = animator.GetBool(Attack);
    var isJumping = animator.GetBool(JumpTrigger);

    // Only enable breathing during idle
    _isBreathingActive = enableBreathing && 
                        !isAttacking && 
                        !isJumping && 
                        moveSpeed < 0.1f;

    // Only enable tail physics during movement
    _isTailPhysicsActive = enableTailPhysics && 
                          !isJumping && 
                          moveSpeed > 0.1f;

    // Only enable head tracking when not busy
    _isHeadTrackingActive = !isAttacking && 
                           !isJumping && 
                           !OwnerAI.IsSleeping() &&
                           moveSpeed < 0.5f;
}

// Called AFTER animator evaluation, BEFORE rendering
private void OnAnimatorIK(int layerIndex)
{
    if (_isBreathingActive)
        UpdateBreathing();

    if (_isTailPhysicsActive)
        UpdateTailPhysics();

    if (_isHeadTrackingActive && OwnerAI?.PrimaryTarget != null)
        PointHeadTowardTarget(OwnerAI.PrimaryTarget);
}
```

## Key Principles

### ✅ DO:

- Use `OnAnimatorIK()` for procedural bone modifications
- Check animation state before applying effects
- Blend smoothly between animated and procedural states
- Store the animated position before modifying (for tail physics)

### ❌ DON'T:

- Modify bones in `LateUpdate()` when Animator is active
- Apply procedural effects during scripted animations (attacks, jumps)
- Fight the animator - disable it if you need full control

## Tail Physics Technical Details

The tail physics is a **spring-damper system** that works WITH the animator:

```csharp
private void UpdateTailPhysics()
{
    // Calculate body movement
    var rootMovement = transform.position - lastRootPosition;
    lastRootPosition = transform.position;

    foreach (var joint in tailJoints)
    {
        // 1. Store animated target (from animator)
        var animTargetPos = joint.position;

        // 2. Apply inertia (tail lags behind)
        tailVelocities[i] -= rootMovement * tailInertia;

        // 3. Spring back toward animated position
        var toTarget = animTargetPos - joint.position;
        tailVelocities[i] += toTarget * tailStiffness * Time.deltaTime;

        // 4. Apply damping (reduce oscillation)
        tailVelocities[i] *= 1f - tailDamping * Time.deltaTime;

        // 5. Update position
        joint.position += tailVelocities[i] * Time.deltaTime;
    }
}
```

This creates a realistic "follow-through" effect where the tail drags slightly
behind the body during movement, while still respecting the animation's overall
motion.

## Benefits

1. **No Animation Conflicts** - Animator controls base pose, we add subtle
   enhancements
2. **Conditional Application** - Effects only apply when appropriate
3. **Performance** - Effects disabled during complex animations
4. **Seamless Blending** - Smooth transitions between animated/procedural states
5. **Creature-Specific** - Works with complex skeletons (tail, spine, neck)

## Configuration

All features can be toggled and tuned via inspector:

- `enableBreathing` - Toggle breathing effect
- `breathingSpeed` - Breaths per second
- `breathingIntensity` - Scale variation amount
- `enableTailPhysics` - Toggle tail physics
- `tailStiffness` - How quickly tail follows body
- `tailDamping` - Reduces oscillation
- `tailInertia` - How much tail lags behind

