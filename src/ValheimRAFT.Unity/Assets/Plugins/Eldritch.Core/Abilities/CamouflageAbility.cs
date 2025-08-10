using System;
using System.Collections;
using UnityEngine;
using Zolantris.Shared;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core.Abilities
{
  [Serializable]
  public class CamouflageAbilityConfig
  {
    public float cooldown = 25f;
    public float duration = 10f;
  }

  public interface IAbilityBase
  {
    public void OnActivate(); // public
    public void OnDeactivate(); // public
    public void Activate(); // method clal from other apis
    public void Deactivate(); // method call from other apis
  }

  public class CamouflageAbility : IAbilityBase
  {
    private CoroutineHandle abilityRoutine;
    private CamouflageAbilityConfig config;
    private float cooldown = 1f;
    private float currentDuration = 0f;
    private float currentCooldown = 0f;
    private XenoAnimationController animationController;
    private SkinnedMeshRenderer skinnedMeshRenderer => animationController.xenoSkinnedMeshRenderer;
    private bool _lastCamouflageState = false;

    private Material camouflageMaterial;
    private Material bodyMaterial;
    private Material headMaterial;
    private MonoBehaviour monoBehavior;

    public CamouflageAbility(MonoBehaviour mb, CamouflageAbilityConfig camouflageAbilityConfig, Material camouflageMat, XenoAnimationController xenoAnimationController)
    {
      monoBehavior = mb;
      config = camouflageAbilityConfig;
      abilityRoutine = new CoroutineHandle(mb);
      camouflageMaterial = camouflageMat;
      animationController = xenoAnimationController;
    }

    public IEnumerator RunAbility()
    {
      currentDuration = config.duration;
      currentCooldown = config.cooldown;



      while (currentDuration > 0)
      {
        currentCooldown -= Time.fixedDeltaTime;
        currentDuration -= Time.fixedDeltaTime;
        yield return new WaitForFixedUpdate();
      }

      OnDeactivate();

      while (currentCooldown > 0)
      {
        currentCooldown -= Time.fixedDeltaTime;
        yield return new WaitForFixedUpdate();
      }

      currentCooldown = 0f;
    }

    public void OnDeactivate()
    {
      currentDuration = 0f;
      if (!abilityRoutine.IsRunning)
      {
        currentCooldown = 0f;
      }

      DeactivateCamouflage();
    }

    public void OnActivate()
    {
      if (abilityRoutine.IsRunning) return;
      ActivateCamouflage();
      abilityRoutine.Start(RunAbility());
    }

    /// <summary>
    /// Runs the ability if it is not running.
    /// </summary>
    public void Activate()
    {
      OnActivate();
    }

    /// <summary>
    /// Does not cancel coroutine.
    /// </summary>
    public void Deactivate()
    {
      OnDeactivate();
    }

    // --- CAMOUFLAGE ---
    public void ActivateCamouflage()
    {
      if (skinnedMeshRenderer == null) return;
      _lastCamouflageState = true;
      if (camouflageMaterial)
      {
        var mats = new Material[skinnedMeshRenderer.materials.Length];
        for (var i = 0; i < mats.Length; i++)
        {
          if (bodyMaterial == null && i == 0)
          {
            bodyMaterial = skinnedMeshRenderer.materials[i];
          }
          if (headMaterial == null && i == 1)
          {
            headMaterial = skinnedMeshRenderer.materials[i];
          }
          mats[i] = camouflageMaterial;
        }
        skinnedMeshRenderer.materials = mats;
      }
    }
    public void DeactivateCamouflage()
    {
      if (!_lastCamouflageState || skinnedMeshRenderer == null) return;
      if (bodyMaterial && headMaterial && skinnedMeshRenderer.materials.Length == 2)
      {
        Material[] mats = { bodyMaterial, headMaterial };
        skinnedMeshRenderer.materials = mats;
      }
      _lastCamouflageState = false;
    }
  }
}