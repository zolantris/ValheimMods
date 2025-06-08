#region

  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using BepInEx;
  using Jotunn.Managers;
  using Newtonsoft.Json;
  using TMPro;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Compat;
  using ValheimVehicles.Components;
  using ValheimVehicles.ConsoleCommands;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Integrations;
  using ValheimVehicles.UI;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.Storage.Serialization;
  // Assume you want the shared scripts style.
  using Object = UnityEngine.Object;

#endregion

  namespace ValheimVehicles.Controllers;

  public static class VehicleStorageController
  {
    private static readonly string BaseFolderPath = Path.Combine(Paths.ConfigPath, $"{ValheimVehiclesPlugin.Author}-{ValheimVehiclesPlugin.ModName}");
    private static readonly string SavedVehiclesFolderPath = Path.Combine(BaseFolderPath, "SavedVehicles");

    static VehicleStorageController()
    {
      EnsureStorageFolderExists();
    }

    private static void EnsureStorageFolderExists()
    {
      if (!Directory.Exists(SavedVehiclesFolderPath))
      {
        Directory.CreateDirectory(SavedVehiclesFolderPath);
      }
    }

    // Data structure for stored vehicle
    [Serializable]
    public class StoredVehicleData
    {
      public string ModVersion;
      public string VehicleName;
      public VehicleSettings Settings;
      public List<StoredPieceData> Pieces;
    }

    [Serializable]
    public struct VehicleSettings
    {
      public VehicleVariant vehicleVariant;
    }


    [Serializable]
    public class StoredPieceData
    {
      public SerializableVector3 Position;
      public SerializableQuaternion Rotation;
      public string PrefabName;
      public int PrefabId;

      // optionals that might not exist
      public int? PrefabSwivelParentId;
      public int? PersistentId;
      public StoredSwivelCustomConfig? SwivelConfigData;
      public StoredSailData? SailData; // Optional component data
    }

    private static string GetVehicleFilePath(string vehicleName)
    {
      return Path.Combine(SavedVehiclesFolderPath, $"{vehicleName}.json");
    }

    public static string SelectedVehicle = "";

    public static void SetSelectedVehicle(string vehicleName)
    {
      if (!File.Exists(GetVehicleFilePath(vehicleName)))
      {
        SelectedVehicle = "";
        return;
      }

      SelectedVehicle = vehicleName;
    }

    public static VehicleVariant GetVehicleType(VehicleManager vehicle)
    {
      return vehicle.vehicleVariant;
    }

    public static void SaveClosestVehicle()
    {
      if (!Player.m_localPlayer) return;
      var closestShip = VehicleCommands.GetNearestVehicleManager();
      if (closestShip == null || closestShip.PiecesController == null) return;

      var pieces = new List<StoredPieceData>();

      foreach (var piece in closestShip.PiecesController.m_pieces)
      {
        if (piece == null) continue;

        var netView = piece.GetComponent<ZNetView>();
        if (!netView || netView.GetZDO() == null) continue;

        var pieceTransform = piece.transform;

        var relativeRotation = Quaternion.Euler(netView.GetZDO().GetVec3(VehicleZdoVars.MBRotationVecHash, pieceTransform.localRotation.eulerAngles));

        // must use MBPosition/rotation as swivels can nest within eachother causing local position to be different from parent or rotated or moved.
        var storedPieceData = new StoredPieceData
        {
          PrefabId = piece.GetInstanceID(),
          PrefabName = piece.GetPrefabName(),
          Position = new SerializableVector3(netView.GetZDO().GetVec3(VehicleZdoVars.MBPositionHash, pieceTransform.localPosition)),
          Rotation = new SerializableQuaternion(relativeRotation)
        };

        AddCustomPieceData(storedPieceData, piece.gameObject);

        pieces.Add(storedPieceData);
      }
      var vehicleType = GetVehicleType(closestShip);
      var localDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
      var vehicleName = string.Join("_", Player.m_localPlayer.GetPlayerName(), vehicleType.ToString(), localDate);

      var data = new StoredVehicleData
      {
        VehicleName = "", // we never want this value saved. We only use filename.
        ModVersion = ValheimRAFT_API.GetPluginVersion(),
        Pieces = pieces,
        Settings = new VehicleSettings
        {
          vehicleVariant = vehicleType
        }
      };

      SaveOrUpdateVehicle(vehicleName, data);
    }

    public static void LogAllVehicles(List<StoredVehicleData> allVehicles)
    {
      LoggerProvider.LogDebug($"Found ({allVehicles.Count} total vehicles. Vehicle Names: {allVehicles.Select(x => x.VehicleName).ToList()}) ");
    }

    public static void SpawnSelectedVehicle()
    {
      if (!VehicleCommands.CanRunCheatCommand()) return;
      var allVehicles = GetAllVehicles();
      if (allVehicles.Count <= 0)
      {
        LoggerProvider.LogDebug("No saved vehicles found");
        return;
      }

      var matchingVehicle = allVehicles.Find(x => x.VehicleName == SelectedVehicle);

      if (matchingVehicle == null)
      {
#if DEBUG
        LogAllVehicles(allVehicles);
#endif
        // we log all vehicle names here if the user allows debug logging
        if (SelectedVehicle != "")
        {
          LogAllVehicles(allVehicles);
        }

        LoggerProvider.LogMessage($"No saved vehicles found for {SelectedVehicle}");
        return;
      }

      SpawnVehicleFromData(matchingVehicle);
    }

    public static void AddCustomPieceData(StoredPieceData storedPieceData, GameObject piecePrefabInstance)
    {
      var netView = piecePrefabInstance.GetComponent<ZNetView>();
      if (!netView || netView.GetZDO() == null)
      {
        return;
      }

      if (piecePrefabInstance.name.StartsWith(PrefabNames.Tier1CustomSailName))
      {
        var sail = piecePrefabInstance.GetComponent<SailComponent>();
        if (sail != null && sail.m_nview != null)
        {
          var zdo = sail.m_nview.GetZDO();
          if (zdo != null)
          {
            storedPieceData.SailData = StoredSailDataExtensions.GetSerializableData(zdo, sail);
          }
        }
      }

      var swivel = piecePrefabInstance.GetComponent<SwivelComponentBridge>();
      if (swivel)
      {
        storedPieceData.SwivelConfigData = swivel.Config.SerializeToJson();
        storedPieceData.PersistentId = swivel.GetPersistentId();
      }
      var swivelParentId = netView.GetZDO().GetInt(VehicleZdoVars.SwivelParentId);
      if (swivelParentId != 0)
      {
        storedPieceData.PrefabSwivelParentId = swivelParentId;
      }
    }

    public static void InitPieceCustomData(StoredPieceData vehicleDataPiece, GameObject piecePrefabInstance)
    {
      if (vehicleDataPiece.PrefabName.StartsWith(PrefabNames.Tier1CustomSailName))
      {
        var sail = piecePrefabInstance.GetComponent<SailComponent>();
        if (sail != null && vehicleDataPiece.SailData != null && sail.m_nview != null && sail.m_nview.GetZDO() != null)
        {
          sail.ApplyLoadedSailData(vehicleDataPiece.SailData);

          // must save all data otherwise it will not be loaded on reload of the spawned vehicle.
          sail.SaveZdo();
        }
      }
    }

    private static List<StoredPieceData> SortSwivelPiecesInDependencyOrder(List<StoredPieceData> pieces)
    {
      var result = new List<StoredPieceData>();

      // 1. Group pieces into three categories
      var rootSwivels = new List<StoredPieceData>();
      var childSwivels = new List<StoredPieceData>();
      var otherPieces = new List<StoredPieceData>();

      // Dictionary to map swivel persistent IDs to their piece data
      var swivelIdToData = new Dictionary<int, StoredPieceData>();

      // First pass: categorize pieces and build the ID map
      foreach (var piece in pieces)
      {
        var isSwivel = piece.PrefabName == PrefabNames.SwivelPrefabName &&
                       piece.PersistentId.HasValue &&
                       piece.PersistentId.Value != 0;

        if (isSwivel)
        {
          swivelIdToData[piece.PersistentId.Value] = piece;

          if (!piece.PrefabSwivelParentId.HasValue || piece.PrefabSwivelParentId.Value == 0)
          {
            rootSwivels.Add(piece); // Root swivel (no parent)
          }
          else
          {
            childSwivels.Add(piece); // Child swivel (has a parent)
          }
        }
        else
        {
          otherPieces.Add(piece); // Non-swivel piece
        }
      }

      // 2. Add all root swivels first
      result.AddRange(rootSwivels);

      // 3. Build a dependency graph for child swivels
      var graph = new Dictionary<int, List<int>>();
      var processed = new HashSet<int>();

      // Initialize graph with all child swivel IDs
      foreach (var swivel in childSwivels)
      {
        if (swivel.PersistentId.HasValue)
        {
          graph[swivel.PersistentId.Value] = new List<int>();
        }
      }

      // Build the dependency relationships
      foreach (var swivel in childSwivels)
      {
        if (swivel.PersistentId.HasValue && swivel.PrefabSwivelParentId.HasValue)
        {
          var childId = swivel.PersistentId.Value;
          var parentId = swivel.PrefabSwivelParentId.Value;

          // Add parent as dependency for the child
          if (graph.ContainsKey(childId))
          {
            graph[childId].Add(parentId);
          }
        }
      }

      // 4. Topological sort for child swivels
      void AddSwivel(int swivelId)
      {
        // If already processed, skip
        if (processed.Contains(swivelId)) return;

        // Process dependencies first (parents)
        if (graph.TryGetValue(swivelId, out var dependencies))
        {
          foreach (var depId in dependencies)
          {
            AddSwivel(depId);
          }
        }

        // Add this swivel if not already processed
        if (!processed.Contains(swivelId) && swivelIdToData.TryGetValue(swivelId, out var swivelData))
        {
          result.Add(swivelData);
          processed.Add(swivelId);
        }
      }

      // Process all child swivels
      foreach (var swivel in childSwivels)
      {
        if (swivel.PersistentId.HasValue)
        {
          AddSwivel(swivel.PersistentId.Value);
        }
      }

      // 5. Add any remaining child swivels that weren't added due to circular references
      foreach (var swivel in childSwivels)
      {
        if (swivel.PersistentId.HasValue && !processed.Contains(swivel.PersistentId.Value))
        {
          result.Add(swivel);
          LoggerProvider.LogWarning($"Possible circular reference in swivel hierarchy for {swivel.PrefabName} with ID {swivel.PersistentId.Value}");
        }
      }

      // 6. Add all other non-swivel pieces last
      result.AddRange(otherPieces);

      return result;
    }


    /// <summary>
    /// Todo take a position so we can spawn this further from player.
    /// </summary>
    public static void SpawnVehicleFromData(StoredVehicleData vehicleData)
    {
      if (!Player.m_localPlayer || PrefabManager.Instance == null) return;
      var playerTransform = Player.m_localPlayer.transform;
      var position = playerTransform.position + playerTransform.forward * 3f;

      var vehiclePrefab = GetVehicleTypeFromVehicleData(vehicleData);

      GameObject vehiclePrefabInstance;
      try
      {
        VehicleManager.CanInitHullPiece = false;
        vehiclePrefabInstance = Object.Instantiate(vehiclePrefab, position, Quaternion.identity);
        if (!vehiclePrefabInstance) return;
      }
      catch (Exception e)
      {
        VehicleManager.CanInitHullPiece = true;
        return;
      }
      VehicleManager.CanInitHullPiece = true;

      if (vehiclePrefabInstance == null) return;


      var vehicleShip = vehiclePrefabInstance.GetComponent<VehicleManager>();
      if (vehicleShip == null) return;
      if (vehicleShip.PiecesController == null)
      {
        LoggerProvider.LogError("Somehow have a null piece controller on a valid vehicle");

        // destroy this component if it's unhealthy.
        var nv = vehicleShip.GetComponent<ZNetView>();
        if (nv != null) nv.Destroy();
        return;
      }

      // prevents accidents where a swivel could not be initialized yet it must be parenting an item or even a nested swivel.
      vehicleData.Pieces = SortSwivelPiecesInDependencyOrder(vehicleData.Pieces);

      var piecesWithoutSwivelParents = vehicleData.Pieces.Where(x => x.PrefabSwivelParentId == null).ToList();
      var piecesWithSwivelParents = vehicleData.Pieces.Where(x => x.PrefabSwivelParentId != null).ToList();
      var swivelPieces = vehicleData.Pieces.Select(x => x.PersistentId != null && x.PersistentId != 0 && PrefabNames.SwivelPrefabName == x.PrefabName).ToList();

      var piecesToSwivelParentIds = new Dictionary<int, List<StoredPieceData>>();

      // old persistentId to new swivelPiece.
      var instantiatedSwivelPieces = new Dictionary<int, SwivelComponentBridge>();

      foreach (var pieceWithSwivelParent in piecesWithSwivelParents)
      {
        if (!pieceWithSwivelParent.PrefabSwivelParentId.HasValue) continue;
        if (piecesToSwivelParentIds.TryGetValue(pieceWithSwivelParent.PrefabSwivelParentId.Value, out var swivelChildren))
        {
          swivelChildren.Add(pieceWithSwivelParent);
        }
        else
        {
          piecesToSwivelParentIds[pieceWithSwivelParent.PrefabSwivelParentId.Value] = new List<StoredPieceData>
          {
            pieceWithSwivelParent
          };
        }
      }


      foreach (var vehicleDataPiece in vehicleData.Pieces)
      {
        var piecePrefab = PrefabManager.Instance.GetPrefab(vehicleDataPiece.PrefabName);
        if (piecePrefab == null)
        {
          LoggerProvider.LogMessage($"Could not find prefab piece by name. {vehicleDataPiece.PrefabName}. Skipping piece.");
          continue;
        }
        try
        {
          var parentTransform = vehicleShip.transform;
          SwivelComponentBridge? swivelComponentInstance = null;
          if (vehicleDataPiece.PrefabSwivelParentId.HasValue && instantiatedSwivelPieces.TryGetValue(vehicleDataPiece.PrefabSwivelParentId.Value, out swivelComponentInstance))
          {
            parentTransform = swivelComponentInstance.transform;
          }

          // todo parent might need to be dynamic. But setting to vehicle should align with swivels which first modify the position based on the top level parent. Then reparent into the nested one.
          var piecePrefabInstance = Object.Instantiate(piecePrefab, parentTransform.position + vehicleDataPiece.Position.ToVector3(), parentTransform.rotation * vehicleDataPiece.Rotation.ToQuaternion(), parentTransform);

          if (vehicleDataPiece.PrefabName == PrefabNames.SwivelPrefabName && vehicleDataPiece.PersistentId.HasValue && vehicleDataPiece.SwivelConfigData != null)
          {
            var swivel = piecePrefabInstance.GetComponent<SwivelComponentBridge>();
            if (!swivel)
            {
              LoggerProvider.LogError($"Error finding swivel component on piece {piecePrefab.name}. This piece should have been instantiated before this child however it was not found.");
              continue;
            }
            // todo might have to deserialize async/after a couple frames. See below for coroutine.
            swivel.Config.DeserializeFromJson(vehicleDataPiece.SwivelConfigData);

            if (swivel.m_nview == null)
            {
              swivel.WaitForZNetView((nv) =>
              {
                swivel.prefabConfigSync.Save(nv.GetZDO());
              });
            }
            else
            {
              swivel.prefabConfigSync.Save(swivel.m_nview.GetZDO());
            }
            instantiatedSwivelPieces.Add(vehicleDataPiece.PersistentId.Value, swivel);
          }

          // all custom checks done here.
          InitPieceCustomData(vehicleDataPiece, piecePrefabInstance);

          if (vehicleDataPiece.PrefabSwivelParentId.HasValue && swivelComponentInstance != null)
          {
            // if (!instantiatedSwivelPieces.TryGetValue(vehicleDataPiece.PrefabSwivelParentId.Value, out var swivelComponentInstance))
            // {
            //   LoggerProvider.LogError($"Error finding nested swivelComponent. This component should have been instantiated before this child however it was not found. prefab: {vehicleDataPiece.PrefabName} swivelId: {vehicleDataPiece.PrefabSwivelParentId}");
            // }
            // else
            // {
            swivelComponentInstance.AddNewPiece(piecePrefabInstance);
            // }
          }
          else
          {
            vehicleShip.PiecesController.AddNewPiece(piecePrefabInstance);
          }
        }
        catch (Exception e)
        {
          LoggerProvider.LogError($"Failed to add piece {piecePrefab.name} to vehicle {vehiclePrefabInstance.name} \n {e}");
        }
      }

      vehicleShip.PiecesController.StartActivatePendingPieces();
    }

    /// <summary>
    /// Saves or updates a vehicle entry.
    /// </summary>
    public static void SaveOrUpdateVehicle(string vehicleName, StoredVehicleData vehicleData)
    {
      try
      {
        var path = GetVehicleFilePath(vehicleName);
        var json = JsonConvert.SerializeObject(vehicleData, Formatting.Indented);
        File.WriteAllText(path, json);
        LoggerProvider.LogMessage($"Saved {vehicleName} to {path}");
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"Failed to save vehicle '{vehicleName}': {ex}");
      }

      RefreshVehicleSelectionGui(VehicleGui.VehicleSelectDropdown);
    }

    public static void RefreshVehicleSelectionGui(TMP_Dropdown dropdown)
    {
      if (dropdown == null) return;

      dropdown.ClearOptions();

      List<TMP_Dropdown.OptionData> options = new();

      var vehicleOptions = GetAllVehicles()
        .Select(x => new TMP_Dropdown.OptionData(x.VehicleName))
        .ToList();

      if (vehicleOptions.Count > 0)
      {
        options.Add(new TMP_Dropdown.OptionData("None"));
        options.AddRange(vehicleOptions);
      }

      if (options.Count == 0)
      {
        options.Add(new TMP_Dropdown.OptionData("No Vehicles"));
      }

      DropdownHelpers.SetupOptionsAndSelectFirst(dropdown, options);
    }

    /// <summary>
    /// Deletes a saved vehicle entry.
    /// </summary>
    public static void DeleteVehicle(string vehicleName)
    {
      try
      {
        var path = GetVehicleFilePath(vehicleName);
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to delete vehicle '{vehicleName}': {ex}");
      }
    }

    public static GameObject GetVehicleTypeFromVehicleData(StoredVehicleData vehicleData)
    {
      switch (vehicleData.Settings.vehicleVariant)
      {
        case VehicleVariant.Land:
          return PrefabManager.Instance.GetPrefab(PrefabNames.LandVehicle);
        case VehicleVariant.Air:
          return PrefabManager.Instance.GetPrefab(PrefabNames.AirVehicle);
        case VehicleVariant.Water:
        case VehicleVariant.Sub:
        case VehicleVariant.All:
        default:
          return PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleShip);
      }
    }

    /// <summary>
    /// Loads a single saved vehicle.
    /// </summary>
    public static StoredVehicleData? GetVehicle(string vehicleName)
    {
      try
      {
        var path = GetVehicleFilePath(vehicleName);
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<StoredVehicleData>(json);
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"Failed to load vehicle '{vehicleName}': {ex}");
        return null;
      }
    }

    public static void RenderAvailableVehicles()
    {
      var allVehicles = GetAllVehicles();
      LoggerProvider.LogDebug(allVehicles.ToString());
    }

    private static readonly List<StoredVehicleData> _allVehicles = new();

    /// <summary>
    /// Loads all saved vehicles.
    /// </summary>
    public static List<StoredVehicleData> GetAllVehicles()
    {
      _allVehicles.Clear();

      try
      {
        if (!Directory.Exists(SavedVehiclesFolderPath))
          return _allVehicles;

        var files = Directory.GetFiles(SavedVehiclesFolderPath, "*.json");
        foreach (var file in files)
        {
          var json = File.ReadAllText(file);
          var vehicle = JsonConvert.DeserializeObject<StoredVehicleData>(json);
          if (vehicle != null)
          {
            // The vehicle name should match the file name. This will prevent problems.
            var fileTextName = file.Replace(SavedVehiclesFolderPath, "").Replace("\\", "").Replace(".json", "");

            // always use the filename as vehicle name.
            // todo might want to sanitize strings to prevent issues.
            vehicle.VehicleName = fileTextName;

            _allVehicles.Add(vehicle);
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to load vehicles: {ex}");
      }

      return _allVehicles;
    }
  }