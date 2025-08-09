using Zolantris.Shared;
namespace Eldritch.Core
{
  using System;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.AI;

  public class ValheimPathfinding : MonoBehaviour
  {
    public class NavMeshTile
    {
      public Vector3Int m_tile;

      public Vector3 m_center;

      public float m_pokeTime = -1000f;

      public float m_buildTime = -1000f;

      public NavMeshData m_data;

      public NavMeshDataInstance m_instance;

      public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links1 = new();

      public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links2 = new();
    }

    public enum AgentType
    {
      Humanoid = 1,
      TrollSize,
      HugeSize,
      HorseSize,
      HumanoidNoSwim,
      HumanoidAvoidWater,
      Fish,
      HumanoidBig,
      BigFish,
      GoblinBruteSize,
      HumanoidBigNoSwim,
      Abomination,
      SeekerQueen
    }

    public enum AreaType
    {
      Default,
      NotWalkable,
      Jump,
      Water
    }

    public class AgentSettings
    {
      public AgentType m_agentType;

      public NavMeshBuildSettings m_build;

      public bool m_canWalk = true;

      public bool m_avoidWater;

      public bool m_canSwim = true;

      public float m_swimDepth;

      public int m_areaMask = -1;

      public AgentSettings(AgentType type)
      {
        m_agentType = type;
        m_build = NavMesh.CreateSettings();
      }
    }

    public List<Vector3> tempPath = new();

    public List<Vector3> optPath = new();

    public List<Vector3> tempStitchPoints = new();

    public RaycastHit[] tempHitArray = new RaycastHit[255];

    public static ValheimPathfinding m_instance;

    public LayerMask m_layers;

    public LayerMask m_waterLayers;

    public Dictionary<Vector3Int, NavMeshTile> m_tiles = new();

    public float m_tileSize = 32f;

    public float m_defaultCost = 1f;

    public float m_waterCost = 4f;

    public float m_linkCost = 10f;

    public float m_linkWidth = 1f;

    public float m_updateInterval = 5f;

    public float m_tileTimeout = 30f;

    public const float m_tileHeight = 6000f;

    public const float m_tileY = 2500f;

    public float m_updatePathfindingTimer;

    public Queue<Vector3Int> m_queuedAreas = new();

    public Queue<NavMeshLinkInstance> m_linkRemoveQueue = new();

    public Queue<NavMeshDataInstance> m_tileRemoveQueue = new();

    public Vector3Int m_cachedTileID = new(-9999999, -9999999, -9999999);

    public NavMeshTile m_cachedTile;

    public List<AgentSettings> m_agentSettings = new();

    public AsyncOperation m_buildOperation;

    public NavMeshTile m_buildTile;

    public List<KeyValuePair<NavMeshTile, NavMeshTile>> m_edgeBuildQueue = new();

    public NavMeshPath m_path;

    public static ValheimPathfinding instance => m_instance;

    public void Awake()
    {
      m_instance = this;
      SetupAgents();
      m_path = new NavMeshPath();
    }

    #region NEW CODE

    public bool TrySnapToNav(Vector3 center, AgentType agentType, out Vector3 snapped)
    {
      var p = center;
      var ok = SnapToNavMesh(ref p, true, GetSettings(agentType));
      snapped = p;
      return ok;
    }

    public bool IsPositionOnNavMesh(Vector3 center, float radius, AgentType agentType, out Vector3 snapped)
    {
      if (FindValidPoint(out var p, center, radius, agentType))
      {
        snapped = p;
        return true;
      }
      snapped = center;
      return false;
    }

    #endregion

    public void ClearAgentSettings()
    {
      var list = new List<NavMeshBuildSettings>();
      for (var i = 0; i < NavMesh.GetSettingsCount(); i++)
      {
        list.Add(NavMesh.GetSettingsByIndex(i));
      }
      foreach (var item in list)
      {
        if (item.agentTypeID != 0)
        {
          NavMesh.RemoveSettings(item.agentTypeID);
        }
      }
    }

    public void OnDestroy()
    {
      foreach (var value in m_tiles.Values)
      {
        ClearLinks(value);
        if ((bool)value.m_data)
        {
          NavMesh.RemoveNavMeshData(value.m_instance);
        }
      }
      m_tiles.Clear();
      DestroyAllLinks();
    }

    public AgentSettings AddAgent(AgentType type, AgentSettings copy = null)
    {
      while ((int)(type + 1) > m_agentSettings.Count)
      {
        m_agentSettings.Add(null);
      }
      var agentSettings = new AgentSettings(type);
      if (copy != null)
      {
        agentSettings.m_build.agentHeight = copy.m_build.agentHeight;
        agentSettings.m_build.agentClimb = copy.m_build.agentClimb;
        agentSettings.m_build.agentRadius = copy.m_build.agentRadius;
        agentSettings.m_build.agentSlope = copy.m_build.agentSlope;
      }
      m_agentSettings[(int)type] = agentSettings;
      return agentSettings;
    }

    public void SetupAgents()
    {
      ClearAgentSettings();
      var agentSettings = AddAgent(AgentType.Humanoid);
      agentSettings.m_build.agentHeight = 1.8f;
      agentSettings.m_build.agentClimb = 0.3f;
      agentSettings.m_build.agentRadius = 0.4f;
      agentSettings.m_build.agentSlope = 85f;
      AddAgent(AgentType.HumanoidNoSwim, agentSettings).m_canSwim = false;
      var agentSettings2 = AddAgent(AgentType.HumanoidBig, agentSettings);
      agentSettings2.m_build.agentHeight = 2.5f;
      agentSettings2.m_build.agentClimb = 0.3f;
      agentSettings2.m_build.agentRadius = 0.5f;
      agentSettings2.m_build.agentSlope = 85f;
      var agentSettings3 = AddAgent(AgentType.HumanoidBigNoSwim);
      agentSettings3.m_build.agentHeight = 2.5f;
      agentSettings3.m_build.agentClimb = 0.3f;
      agentSettings3.m_build.agentRadius = 0.5f;
      agentSettings3.m_build.agentSlope = 85f;
      agentSettings3.m_canSwim = false;
      AddAgent(AgentType.HumanoidAvoidWater, agentSettings).m_avoidWater = true;
      var agentSettings4 = AddAgent(AgentType.TrollSize);
      agentSettings4.m_build.agentHeight = 7f;
      agentSettings4.m_build.agentClimb = 0.6f;
      agentSettings4.m_build.agentRadius = 1f;
      agentSettings4.m_build.agentSlope = 85f;
      var agentSettings5 = AddAgent(AgentType.Abomination);
      agentSettings5.m_build.agentHeight = 5f;
      agentSettings5.m_build.agentClimb = 0.6f;
      agentSettings5.m_build.agentRadius = 1.5f;
      agentSettings5.m_build.agentSlope = 85f;
      var agentSettings6 = AddAgent(AgentType.SeekerQueen);
      agentSettings6.m_build.agentHeight = 7f;
      agentSettings6.m_build.agentClimb = 0.6f;
      agentSettings6.m_build.agentRadius = 1.5f;
      agentSettings6.m_build.agentSlope = 85f;
      var agentSettings7 = AddAgent(AgentType.GoblinBruteSize);
      agentSettings7.m_build.agentHeight = 3.5f;
      agentSettings7.m_build.agentClimb = 0.3f;
      agentSettings7.m_build.agentRadius = 0.8f;
      agentSettings7.m_build.agentSlope = 85f;
      var agentSettings8 = AddAgent(AgentType.HugeSize);
      agentSettings8.m_build.agentHeight = 10f;
      agentSettings8.m_build.agentClimb = 0.6f;
      agentSettings8.m_build.agentRadius = 2f;
      agentSettings8.m_build.agentSlope = 85f;
      var agentSettings9 = AddAgent(AgentType.HorseSize);
      agentSettings9.m_build.agentHeight = 2.5f;
      agentSettings9.m_build.agentClimb = 0.3f;
      agentSettings9.m_build.agentRadius = 0.8f;
      agentSettings9.m_build.agentSlope = 85f;
      var agentSettings10 = AddAgent(AgentType.Fish);
      agentSettings10.m_build.agentHeight = 0.5f;
      agentSettings10.m_build.agentClimb = 1f;
      agentSettings10.m_build.agentRadius = 0.5f;
      agentSettings10.m_build.agentSlope = 90f;
      agentSettings10.m_canSwim = true;
      agentSettings10.m_canWalk = false;
      agentSettings10.m_swimDepth = 0.4f;
      agentSettings10.m_areaMask = 12;
      var agentSettings11 = AddAgent(AgentType.BigFish);
      agentSettings11.m_build.agentHeight = 1.5f;
      agentSettings11.m_build.agentClimb = 1f;
      agentSettings11.m_build.agentRadius = 1f;
      agentSettings11.m_build.agentSlope = 90f;
      agentSettings11.m_canSwim = true;
      agentSettings11.m_canWalk = false;
      agentSettings11.m_swimDepth = 1.5f;
      agentSettings11.m_areaMask = 12;
      NavMesh.SetAreaCost(0, m_defaultCost);
      NavMesh.SetAreaCost(3, m_waterCost);
    }

    public AgentSettings GetSettings(AgentType agentType)
    {
      return m_agentSettings[(int)agentType];
    }

    public int GetAgentID(AgentType agentType)
    {
      return GetSettings(agentType).m_build.agentTypeID;
    }

    public void Update()
    {
      if (!IsBuilding())
      {
        m_updatePathfindingTimer += Time.deltaTime;
        if (m_updatePathfindingTimer > 0.1f)
        {
          m_updatePathfindingTimer = 0f;
          UpdatePathfinding();
        }
        if (!IsBuilding())
        {
          DestroyQueuedNavmeshData();
        }
      }
    }

    public void DestroyAllLinks()
    {
      while (m_linkRemoveQueue.Count > 0 || m_tileRemoveQueue.Count > 0)
      {
        DestroyQueuedNavmeshData();
      }
    }

    public void DestroyQueuedNavmeshData()
    {
      if (m_linkRemoveQueue.Count > 0)
      {
        var num = Mathf.Min(m_linkRemoveQueue.Count, Mathf.Max(25, m_linkRemoveQueue.Count / 40));
        for (var i = 0; i < num; i++)
        {
          NavMesh.RemoveLink(m_linkRemoveQueue.Dequeue());
        }
      }
      else if (m_tileRemoveQueue.Count > 0)
      {
        NavMesh.RemoveNavMeshData(m_tileRemoveQueue.Dequeue());
      }
    }

    public void UpdatePathfinding()
    {
      Buildtiles();
      TimeoutTiles();
    }

    public bool HavePath(Vector3 from, Vector3 to, AgentType agentType)
    {
      return GetPath(from, to, null, agentType, true, false, true);
    }

    public bool FindValidPoint(out Vector3 point, Vector3 center, float range, AgentType agentType)
    {
      PokePoint(center, agentType);
      var settings = GetSettings(agentType);
      var filter = default(NavMeshQueryFilter);
      filter.agentTypeID = (int)settings.m_agentType;
      filter.areaMask = settings.m_areaMask;
      if (NavMesh.SamplePosition(center, out var hit, range, filter))
      {
        point = hit.position;
        return true;
      }
      point = center;
      return false;
    }

    public bool IsUnderTerrain(Vector3 p)
    {
      if (ValheimZoneSystemStub.instance.GetGroundHeight(p, out var height) && p.y < height - 1f)
      {
        return true;
      }
      return false;
    }

    public bool GetPath(Vector3 from, Vector3 to, List<Vector3> path, AgentType agentType, bool requireFullPath = false, bool cleanup = true, bool havePath = false)
    {
      path?.Clear();
      PokeArea(from, agentType);
      PokeArea(to, agentType);
      var settings = GetSettings(agentType);
      if (!SnapToNavMesh(ref from, true, settings))
      {
        return false;
      }
      if (!SnapToNavMesh(ref to, !havePath, settings))
      {
        return false;
      }
      var filter = default(NavMeshQueryFilter);
      filter.agentTypeID = settings.m_build.agentTypeID;
      filter.areaMask = settings.m_areaMask;
      if (NavMesh.CalculatePath(from, to, filter, m_path))
      {
        if (m_path.status == NavMeshPathStatus.PathPartial)
        {
          if (IsUnderTerrain(m_path.corners[0]) || IsUnderTerrain(m_path.corners[m_path.corners.Length - 1]))
          {
            return false;
          }
          if (requireFullPath)
          {
            return false;
          }
        }
        if (path != null)
        {
          path.AddRange(m_path.corners);
          if (cleanup)
          {
            CleanPath(path, settings);
          }
        }
        return true;
      }
      return false;
    }

    public void CleanPath(List<Vector3> basePath, AgentSettings settings)
    {
      if (basePath.Count <= 2)
      {
        return;
      }
      var filter = default(NavMeshQueryFilter);
      filter.agentTypeID = settings.m_build.agentTypeID;
      filter.areaMask = settings.m_areaMask;
      var num = 0;
      optPath.Clear();
      optPath.Add(basePath[num]);
      do
      {
        num = FindNextNode(basePath, filter, num);
        optPath.Add(basePath[num]);
      } while (num < basePath.Count - 1);
      tempPath.Clear();
      tempPath.Add(optPath[0]);
      for (var i = 1; i < optPath.Count - 1; i++)
      {
        var vector = optPath[i - 1];
        var vector2 = optPath[i];
        var vector3 = optPath[i + 1];
        var normalized = (vector3 - vector2).normalized;
        var normalized2 = (vector2 - vector).normalized;
        var vector4 = vector2 - (normalized + normalized2).normalized * Vector3.Distance(vector2, vector) * 0.33f;
        vector4.y = (vector2.y + vector.y) * 0.5f;
        var normalized3 = (vector4 - vector2).normalized;
        if (!NavMesh.Raycast(vector2 + normalized3 * 0.1f, vector4, out var hit, filter) && !NavMesh.Raycast(vector4, vector, out hit, filter))
        {
          tempPath.Add(vector4);
        }
        tempPath.Add(vector2);
        var vector5 = vector2 + (normalized + normalized2).normalized * Vector3.Distance(vector2, vector3) * 0.33f;
        vector5.y = (vector2.y + vector3.y) * 0.5f;
        var normalized4 = (vector5 - vector2).normalized;
        if (!NavMesh.Raycast(vector2 + normalized4 * 0.1f, vector5, out hit, filter) && !NavMesh.Raycast(vector5, vector3, out hit, filter))
        {
          tempPath.Add(vector5);
        }
      }
      tempPath.Add(optPath[optPath.Count - 1]);
      basePath.Clear();
      basePath.AddRange(tempPath);
    }

    public int FindNextNode(List<Vector3> path, NavMeshQueryFilter filter, int start)
    {
      for (var i = start + 2; i < path.Count; i++)
      {
        if (NavMesh.Raycast(path[start], path[i], out _, filter))
        {
          return i - 1;
        }
      }
      return path.Count - 1;
    }

    public bool SnapToNavMesh(ref Vector3 point, bool extendedSearchArea, AgentSettings settings)
    {
      if (ValheimZoneSystemStub.instance != null)
      {
        if (ValheimZoneSystemStub.instance.GetGroundHeight(point, out var height) && point.y < height)
        {
          point.y = height;
        }
        if (settings.m_canSwim)
        {
          point.y = Mathf.Max(30f - settings.m_swimDepth, point.y);
        }
      }
      var filter = default(NavMeshQueryFilter);
      filter.agentTypeID = settings.m_build.agentTypeID;
      filter.areaMask = settings.m_areaMask;
      NavMeshHit hit;
      if (extendedSearchArea)
      {
        if (NavMesh.SamplePosition(point, out hit, 1.5f, filter))
        {
          point = hit.position;
          return true;
        }
        if (NavMesh.SamplePosition(point, out hit, 3f, filter))
        {
          point = hit.position;
          return true;
        }
        if (NavMesh.SamplePosition(point, out hit, 6f, filter))
        {
          point = hit.position;
          return true;
        }
        if (NavMesh.SamplePosition(point, out hit, 12f, filter))
        {
          point = hit.position;
          return true;
        }
      }
      else if (NavMesh.SamplePosition(point, out hit, 1f, filter))
      {
        point = hit.position;
        return true;
      }

      NavMeshHit dbg;
      filter = new NavMeshQueryFilter { agentTypeID = settings.m_build.agentTypeID, areaMask = settings.m_areaMask };
      if (NavMesh.SamplePosition(point, out dbg, 4000f, filter))
      {
        point = dbg.position;
        Debug.LogWarning("[ValheimPF] Large-radius snap succeeded; check tile Y/height settings.");
        return true;
      }
      if (NavMesh.SamplePosition(point, out dbg, 4000f, NavMesh.AllAreas))
      {
        point = dbg.position;
        Debug.LogWarning("[ValheimPF] AllAreas snap succeeded; your areaMask/agentType likely filters out land.");
        return true;
      }

      return false;
    }

    public void TimeoutTiles()
    {
      var realtimeSinceStartup = Time.realtimeSinceStartup;
      foreach (var tile in m_tiles)
      {
        if (realtimeSinceStartup - tile.Value.m_pokeTime > m_tileTimeout)
        {
          ClearLinks(tile.Value);
          if (tile.Value.m_instance.valid)
          {
            m_tileRemoveQueue.Enqueue(tile.Value.m_instance);
          }
          m_tiles.Remove(tile.Key);
          break;
        }
      }
    }

    public void PokeArea(Vector3 point, AgentType agentType)
    {
      var tile = GetTile(point, agentType);
      PokeTile(tile);
      for (var i = -1; i <= 1; i++)
      {
        for (var j = -1; j <= 1; j++)
        {
          if (j != 0 || i != 0)
          {
            var tileID = new Vector3Int(tile.x + j, tile.y + i, tile.z);
            PokeTile(tileID);
          }
        }
      }
    }

    public void PokePoint(Vector3 point, AgentType agentType)
    {
      var tile = GetTile(point, agentType);
      PokeTile(tile);
    }

    public void PokeTile(Vector3Int tileID)
    {
      GetNavTile(tileID).m_pokeTime = Time.realtimeSinceStartup;
    }

    public void Buildtiles()
    {
      if (UpdateAsyncBuild())
      {
        return;
      }
      NavMeshTile navMeshTile = null;
      var num = 0f;
      foreach (var tile in m_tiles)
      {
        var num2 = tile.Value.m_pokeTime - tile.Value.m_buildTime;
        if (num2 > m_updateInterval && (navMeshTile == null || num2 > num))
        {
          navMeshTile = tile.Value;
          num = num2;
        }
      }
      if (navMeshTile != null)
      {
        BuildTile(navMeshTile);
        navMeshTile.m_buildTime = Time.realtimeSinceStartup;
      }
    }

    public void BuildTile(NavMeshTile tile)
    {
      _ = DateTime.Now;
      var list = new List<NavMeshBuildSource>();
      var markups = new List<NavMeshBuildMarkup>();
      var z = (AgentType)tile.m_tile.z;
      var settings = GetSettings(z);
      var includedWorldBounds = new Bounds(tile.m_center, new Vector3(m_tileSize, 6000f, m_tileSize));
      var localBounds = new Bounds(Vector3.zero, new Vector3(m_tileSize, 6000f, m_tileSize));
      var defaultArea = !settings.m_canWalk ? 1 : 0;
      NavMeshBuilder.CollectSources(includedWorldBounds, m_layers.value, NavMeshCollectGeometry.PhysicsColliders, defaultArea, markups, list);
      if (settings.m_avoidWater)
      {
        var list2 = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(includedWorldBounds, m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 1, markups, list2);
        foreach (var item in list2)
        {
          var current = item;
          current.transform *= Matrix4x4.Translate(Vector3.down * 0.2f);
          list.Add(current);
        }
      }
      else if (settings.m_canSwim)
      {
        var list3 = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(includedWorldBounds, m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 3, markups, list3);
        if (settings.m_swimDepth != 0f)
        {
          foreach (var item2 in list3)
          {
            var current2 = item2;
            current2.transform *= Matrix4x4.Translate(Vector3.down * settings.m_swimDepth);
            list.Add(current2);
          }
        }
        else
        {
          list.AddRange(list3);
        }
      }
      if (tile.m_data == null)
      {
        tile.m_data = new NavMeshData();
        tile.m_data.position = tile.m_center;
      }
      m_buildOperation = NavMeshBuilder.UpdateNavMeshDataAsync(tile.m_data, settings.m_build, list, localBounds);
      m_buildTile = tile;
    }

    public bool IsBuilding()
    {
      if (m_buildOperation != null)
      {
        return !m_buildOperation.isDone;
      }
      return false;
    }

    public bool UpdateAsyncBuild()
    {
      if (m_buildOperation == null)
      {
        return false;
      }
      if (!m_buildOperation.isDone)
      {
        return true;
      }
      if (!m_buildTile.m_instance.valid)
      {
        m_buildTile.m_instance = NavMesh.AddNavMeshData(m_buildTile.m_data);
      }
      RebuildLinks(m_buildTile);
      m_buildOperation = null;
      m_buildTile = null;
      return true;
    }

    public void ClearLinks(NavMeshTile tile)
    {
      ClearLinks(tile.m_links1);
      ClearLinks(tile.m_links2);
    }

    public void ClearLinks(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
    {
      foreach (var link in links)
      {
        m_linkRemoveQueue.Enqueue(link.Value);
      }
      links.Clear();
    }

    public void RebuildLinks(NavMeshTile tile)
    {
      var z = (AgentType)tile.m_tile.z;
      var settings = GetSettings(z);
      var num = m_tileSize / 2f;
      ConnectAlongEdge(tile.m_links1, tile.m_center + new Vector3(num, 0f, num), tile.m_center + new Vector3(num, 0f, 0f - num), m_linkWidth, settings);
      ConnectAlongEdge(tile.m_links2, tile.m_center + new Vector3(0f - num, 0f, num), tile.m_center + new Vector3(num, 0f, num), m_linkWidth, settings);
    }

    public void ConnectAlongEdge(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links, Vector3 p0, Vector3 p1, float step, AgentSettings settings)
    {
      var normalized = (p1 - p0).normalized;
      var vector = Vector3.Cross(Vector3.up, normalized);
      var num = Vector3.Distance(p0, p1);
      var canSwim = settings.m_canSwim;
      tempStitchPoints.Clear();
      for (var num2 = step / 2f; num2 <= num; num2 += step)
      {
        var p2 = p0 + normalized * num2;
        FindGround(p2, canSwim, tempStitchPoints, settings);
      }
      if (CompareLinks(tempStitchPoints, links))
      {
        return;
      }
      ClearLinks(links);
      foreach (var tempStitchPoint in tempStitchPoints)
      {
        var link = default(NavMeshLinkData);
        link.startPosition = tempStitchPoint - vector * 0.1f;
        link.endPosition = tempStitchPoint + vector * 0.1f;
        link.width = step;
        link.costModifier = m_linkCost;
        link.bidirectional = true;
        link.agentTypeID = settings.m_build.agentTypeID;
        link.area = 2;
        var value = NavMesh.AddLink(link);
        if (value.valid)
        {
          links.Add(new KeyValuePair<Vector3, NavMeshLinkInstance>(tempStitchPoint, value));
        }
      }
    }

    public bool CompareLinks(List<Vector3> tempStitchPoints, List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
    {
      if (tempStitchPoints.Count != links.Count)
      {
        return false;
      }
      for (var i = 0; i < tempStitchPoints.Count; i++)
      {
        if (tempStitchPoints[i] != links[i].Key)
        {
          return false;
        }
      }
      return true;
    }

    public bool SnapToNearestGround(Vector3 p, out Vector3 pos, float range)
    {
      if (Physics.Raycast(p + Vector3.up, Vector3.down, out var hitInfo, range + 1f, m_layers.value | m_waterLayers.value))
      {
        pos = hitInfo.point;
        return true;
      }
      if (Physics.Raycast(p + Vector3.up * range, Vector3.down, out hitInfo, range, m_layers.value | m_waterLayers.value))
      {
        pos = hitInfo.point;
        return true;
      }
      pos = p;
      return false;
    }

    public void FindGround(Vector3 p, bool testWater, List<Vector3> hits, AgentSettings settings)
    {
      p.y = 6000f;
      var layerMask = testWater ? m_layers.value | m_waterLayers.value : m_layers.value;
      var agentHeight = settings.m_build.agentHeight;
      var y = p.y;
      var num = Physics.RaycastNonAlloc(p, Vector3.down, tempHitArray, 10000f, layerMask);
      for (var i = 0; i < num; i++)
      {
        var point = tempHitArray[i].point;
        if (!(Mathf.Abs(point.y - y) < agentHeight))
        {
          y = point.y;
          if ((1 << tempHitArray[i].collider.gameObject.layer & (int)m_waterLayers) != 0)
          {
            point.y -= settings.m_swimDepth;
          }
          hits.Add(point);
        }
      }
    }

    public NavMeshTile GetNavTile(Vector3 point, AgentType agent)
    {
      var tile = GetTile(point, agent);
      return GetNavTile(tile);
    }

    public NavMeshTile GetNavTile(Vector3Int tile)
    {
      if (tile == m_cachedTileID)
      {
        return m_cachedTile;
      }
      if (m_tiles.TryGetValue(tile, out var value))
      {
        m_cachedTileID = tile;
        m_cachedTile = value;
        return value;
      }
      value = new NavMeshTile();
      value.m_tile = tile;
      value.m_center = GetTilePos(tile);
      m_tiles.Add(tile, value);
      m_cachedTileID = tile;
      m_cachedTile = value;
      return value;
    }

    public Vector3Int GetTile(Vector3 point, AgentType agent)
    {
      var x = Mathf.FloorToInt((point.x + m_tileSize / 2f) / m_tileSize);
      var y = Mathf.FloorToInt((point.z + m_tileSize / 2f) / m_tileSize);
      return new Vector3Int(x, y, (int)agent);
    }

    public Vector3 GetTilePos(Vector3Int id)
    {
      return new Vector3((float)id.x * m_tileSize, 0f, (float)id.y * m_tileSize);
    }
  }

}