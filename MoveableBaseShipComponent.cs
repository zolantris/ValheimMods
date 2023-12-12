// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.MoveableBaseShipComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimRAFT.Util;

namespace ValheimRAFT
{
  public class MoveableBaseShipComponent : MonoBehaviour
  {
    internal MoveableBaseRootComponent m_baseRoot;
    internal Rigidbody m_rigidbody;
    internal Ship m_ship;
    internal ZNetView m_nview;
    internal GameObject m_baseRootObject;
    internal ZSyncTransform m_zsync;
    public float m_targetHeight;
    public float m_balanceForce = 0.03f;
    public float m_liftForce = 20f;
    public MoveableBaseShipComponent.MBFlags m_flags;

    public void Awake()
    {
      Ship component = ((Component)this).GetComponent<Ship>();
      this.m_nview = ((Component)this).GetComponent<ZNetView>();
      this.m_zsync = ((Component)this).GetComponent<ZSyncTransform>();
      this.m_ship = ((Component)this).GetComponent<Ship>();
      GameObject gameObject = new GameObject();
      ((Object)gameObject).name = "MoveableBase";
      gameObject.layer = 0;
      this.m_baseRootObject = gameObject;
      this.m_baseRoot = this.m_baseRootObject.AddComponent<MoveableBaseRootComponent>();
      this.m_nview.Register<bool>("SetAnchor",
        (Action<long, bool>)((sender, state) => this.RPC_SetAnchor(sender, state)));
      this.m_nview.Register<bool>("SetVisual",
        (Action<long, bool>)((sender, state) => this.RPC_SetVisual(sender, state)));
      this.m_baseRoot.m_moveableBaseShip = this;
      this.m_baseRoot.m_nview = this.m_nview;
      this.m_baseRoot.m_ship = component;
      this.m_baseRoot.m_id = ZDOPersistantID.Instance.GetOrCreatePersistantID(this.m_nview.m_zdo);
      this.m_rigidbody = ((Component)this).GetComponent<Rigidbody>();
      this.m_baseRoot.m_syncRigidbody = this.m_rigidbody;
      this.m_rigidbody.mass = 1000f;
      this.m_baseRootObject.transform.SetParent((Transform)null);
      this.m_baseRootObject.transform.position = ((Component)this).transform.position;
      this.m_baseRootObject.transform.rotation = ((Component)this).transform.rotation;
      ((Component)((Component)component).transform.Find("ship/visual/mast"))?.gameObject
        .SetActive(false);
      ((Component)((Component)component).transform.Find("ship/colliders/log"))?.gameObject
        .SetActive(false);
      ((Component)((Component)component).transform.Find("ship/colliders/log (1)"))?.gameObject
        .SetActive(false);
      ((Component)((Component)component).transform.Find("ship/colliders/log (2)"))?.gameObject
        .SetActive(false);
      ((Component)((Component)component).transform.Find("ship/colliders/log (3)"))?.gameObject
        .SetActive(false);
      this.UpdateVisual();
      this.m_baseRoot.m_onboardcollider =
        ((IEnumerable<BoxCollider>)((Component)((Component)this).transform)
          .GetComponentsInChildren<BoxCollider>()).FirstOrDefault<BoxCollider>(
          (Func<BoxCollider, bool>)(k =>
            ((k).gameObject).name == "OnboardTrigger"));
      ((Component)this.m_baseRoot.m_onboardcollider).transform.localScale = new Vector3(1f, 1f, 1f);
      this.m_baseRoot.m_floatcollider = component.m_floatCollider;
      ((Component)this.m_baseRoot.m_floatcollider).transform.localScale = new Vector3(1f, 1f, 1f);
      this.m_baseRoot.m_blockingcollider =
        ((Component)((Component)component).transform.Find("ship/colliders/Cube"))
        .GetComponentInChildren<BoxCollider>();
      ((Component)this.m_baseRoot.m_blockingcollider).transform.localScale =
        new Vector3(1f, 1f, 1f);
      ((Component)this.m_baseRoot.m_blockingcollider).gameObject.layer =
        ValheimRAFT.ValheimRaftEntrypoint.CustomRaftLayer;
      ((Component)((Component)this.m_baseRoot.m_blockingcollider).transform.parent).gameObject
        .layer = ValheimRAFT.ValheimRaftEntrypoint.CustomRaftLayer;
      ZLog.Log((object)string.Format("Activating MBRoot: {0}", (object)this.m_baseRoot.m_id));
      this.m_baseRoot.ActivatePendingPiecesCoroutine();
      this.FirstTimeCreation();
    }

    public void UpdateVisual()
    {
      if (this.m_nview.m_zdo == null)
        return;
      this.m_flags =
        (MoveableBaseShipComponent.MBFlags)this.m_nview.m_zdo.GetInt("MBFlags", (int)this.m_flags);
      ((Component)((Component)this).transform.Find("ship/visual")).gameObject.SetActive(
        !this.m_flags.HasFlag((Enum)MoveableBaseShipComponent.MBFlags.HideMesh));
      ((Component)((Component)this).transform.Find("interactive")).gameObject.SetActive(
        !this.m_flags.HasFlag((Enum)MoveableBaseShipComponent.MBFlags.HideMesh));
    }

    public void OnDestroy()
    {
      if (!m_baseRoot)
        return;
      this.m_baseRoot.CleanUp();
      Destroy(m_baseRoot.gameObject);
    }

    private void FirstTimeCreation()
    {
      if (this.m_baseRoot.GetPieceCount() != 0)
        return;
      GameObject prefab = ZNetScene.instance.GetPrefab("wood_floor");
      for (float num1 = -1f; (double)num1 < 1.0099999904632568; num1 += 2f)
      {
        for (float num2 = -2f; (double)num2 < 2.0099999904632568; num2 += 2f)
        {
          Vector3 vector3 =
            ((Component)this).transform.TransformPoint(new Vector3(num1, 0.45f, num2));
          this.m_baseRoot.AddNewPiece(Object
            .Instantiate<GameObject>(prefab, vector3, ((Component)this).transform.rotation)
            .GetComponent<ZNetView>());
        }
      }
    }

    internal void Accend()
    {
      if (this.m_flags.HasFlag((Enum)MoveableBaseShipComponent.MBFlags.IsAnchored))
        this.SetAnchor(false);
      if (!ValheimRAFT.ValheimRAFT.Instance.AllowFlight.Value)
      {
        this.m_targetHeight = 0.0f;
      }
      else
      {
        if (!Object.op_Implicit((Object)this.m_baseRoot) ||
            !Object.op_Implicit((Object)this.m_baseRoot.m_floatcollider))
          return;
        this.m_targetHeight =
          Mathf.Clamp(((Component)this.m_baseRoot.m_floatcollider).transform.position.y + 1f,
            ZoneSystem.instance.m_waterLevel, 200f);
      }

      this.m_nview.m_zdo.Set("MBTargetHeight", this.m_targetHeight);
    }

    internal void Descent()
    {
      if (this.m_flags.HasFlag((Enum)MoveableBaseShipComponent.MBFlags.IsAnchored))
        this.SetAnchor(false);
      float targetHeight = this.m_targetHeight;
      if (!ValheimRAFT.ValheimRAFT.Instance.AllowFlight.Value)
      {
        this.m_targetHeight = 0.0f;
      }
      else
      {
        if (!Object.op_Implicit((Object)this.m_baseRoot) ||
            !Object.op_Implicit((Object)this.m_baseRoot.m_floatcollider))
          return;
        this.m_targetHeight =
          Mathf.Clamp(((Component)this.m_baseRoot.m_floatcollider).transform.position.y - 1f,
            ZoneSystem.instance.m_waterLevel, 200f);
        if ((double)((Component)this.m_baseRoot.m_floatcollider).transform.position.y - 1.0 <=
            (double)ZoneSystem.instance.m_waterLevel)
          this.m_targetHeight = 0.0f;
      }

      this.m_nview.m_zdo.Set("MBTargetHeight", this.m_targetHeight);
    }

    internal void UpdateStats(bool flight)
    {
      if (!m_rigidbody ||
          !m_baseRoot || this.m_baseRoot.m_statsOverride)
        return;
      this.m_rigidbody.mass = 3000f;
      this.m_rigidbody.angularDrag = flight ? 1f : 0.0f;
      this.m_rigidbody.drag = flight ? 1f : 0.0f;
      if (m_ship)
      {
        this.m_ship.m_angularDamping = flight ? 5f : 0.8f;
        this.m_ship.m_backwardForce = 1f;
        this.m_ship.m_damping = flight ? 5f : 0.35f;
        this.m_ship.m_dampingSideway = flight ? 3f : 0.3f;
        this.m_ship.m_force = 3f;
        this.m_ship.m_forceDistance = 5f;
        this.m_ship.m_sailForceFactor = flight ? 0.2f : 0.05f;
        this.m_ship.m_stearForce = flight ? 0.2f : 1f;
        this.m_ship.m_stearVelForceFactor = 1.3f;
        this.m_ship.m_waterImpactDamage = 0.0f;
        ImpactEffect component = ((Component)this.m_ship).GetComponent<ImpactEffect>();
        if (component)
        {
          component.m_interval = 0.1f;
          component.m_minVelocity = 0.1f;
          component.m_damages.m_damage = 100f;
        }
      }
    }

    internal void SetAnchor(bool state) => this.m_nview.InvokeRPC(nameof(SetAnchor), new object[1]
    {
      (object)state
    });

    public void RPC_SetAnchor(long sender, bool state)
    {
      this.m_flags = state
        ? this.m_flags | MoveableBaseShipComponent.MBFlags.IsAnchored
        : this.m_flags & ~MoveableBaseShipComponent.MBFlags.IsAnchored;
      this.m_nview.m_zdo.Set("MBFlags", (int)this.m_flags);
    }

    internal void SetVisual(bool state) => this.m_nview.InvokeRPC(nameof(SetVisual), new object[1]
    {
      (object)state
    });

    public void RPC_SetVisual(long sender, bool state)
    {
      this.m_flags = state
        ? this.m_flags | MoveableBaseShipComponent.MBFlags.HideMesh
        : this.m_flags & ~MoveableBaseShipComponent.MBFlags.HideMesh;
      this.m_nview.m_zdo.Set("MBFlags", (int)this.m_flags);
      this.UpdateVisual();
    }

    [Flags]
    public enum MBFlags
    {
      None = 0,
      IsAnchored = 1,
      HideMesh = 2,
    }
  }
}