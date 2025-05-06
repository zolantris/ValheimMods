using UnityEngine;
using System.Collections.Generic;
using ValheimVehicles.SharedScripts;

/// <summary>
/// Mostly for characters but could apply to inanimate objects and other layers.
/// </summary>
public class NearestTargetScanManager : SingletonBehaviour<NearestTargetScanManager>
{
  [SerializeField] public float scanRadius = 10f;
  [SerializeField] private string characterLayerName = "character";
  [SerializeField] private float scanInterval = 1f;

  private readonly Collider[] _scanBuffer = new Collider[128];
  private readonly List<Character> _nearbyCharacters = new();

  public IReadOnlyList<Character> CachedCharacters => _nearbyCharacters;

  private float _lastScanTime;
  private int _layerMask;

  public override void Awake()
  {
    Instance = this;
    _layerMask = 1 << LayerMask.NameToLayer(characterLayerName);
  }

  private void FixedUpdate()
  {
    if (ZNet.instance == null || ZNetScene.instance == null || Game.instance == null) return;
    if (Time.time - _lastScanTime >= scanInterval)
    {
      RefreshNearestTargetsToCurrentPlayer();
    }
  }

  private void RefreshNearestTargetsToCurrentPlayer()
  {
    if (Player.m_localPlayer == null) return;
    _nearbyCharacters.Clear();

    var count = Physics.OverlapSphereNonAlloc(Player.m_localPlayer.transform.position, scanRadius, _scanBuffer, _layerMask);
    for (var i = 0; i < count; i++)
    {
      if (_scanBuffer[i].TryGetComponent(out Character c))
      {
        _nearbyCharacters.Add(c);
      }
    }

    _lastScanTime = Time.time;
  }

  private void Refresh()
  {
    // _nearbyCharacters.Clear();
    //
    // var count = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, _scanBuffer, _layerMask);
    // for (var i = 0; i < count; i++)
    // {
    //   if (_scanBuffer[i].TryGetComponent(out Character c))
    //   {
    //     _nearbyCharacters.Add(c);
    //   }
    // }
    //
    // _lastScanTime = Time.time;
  }
}