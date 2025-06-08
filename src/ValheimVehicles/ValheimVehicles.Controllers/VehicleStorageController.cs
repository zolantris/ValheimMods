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

        // must use MBPosition/rotation as swivels can nest within eachother causing local position to be different from parent or rotated or moved.
        var storedPieceData = new StoredPieceData
        {
          PrefabId = piece.GetInstanceID(),
          PrefabName = piece.GetPrefabName(),
          Position = new SerializableVector3(netView.GetZDO().GetVec3(VehicleZdoVars.MBPositionHash, pieceTransform.localPosition)),
          Rotation = new SerializableQuaternion(Quaternion.Euler(netView.GetZDO().GetVec3(VehicleZdoVars.MBRotationVecHash, pieceTransform.localRotation.eulerAngles)))
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
      // Dictionary to map swivel persistent IDs to their pieces
      var swivelIdToPiece = new Dictionary<int, StoredPieceData>();

      // First, identify all swivel pieces and map them by their persistent ID
      foreach (var piece in pieces)
      {
        if (piece.PersistentId.HasValue && piece.PersistentId.Value != 0 &&
            piece.PrefabName == PrefabNames.SwivelPrefabName)
        {
          swivelIdToPiece[piece.PersistentId.Value] = piece;
        }
      }

      // Create a dependency graph - which swivel depends on which
      var dependencies = new Dictionary<StoredPieceData, List<StoredPieceData>>();

      // Identify pieces that are attached to swivels and build dependency tree
      foreach (var piece in pieces)
      {
        if (piece.PrefabSwivelParentId.HasValue &&
            swivelIdToPiece.TryGetValue(piece.PrefabSwivelParentId.Value, out var parentSwivel))
        {
          // This piece depends on a swivel
          if (!dependencies.TryGetValue(parentSwivel, out var dependentList))
          {
            dependentList = new List<StoredPieceData>();
            dependencies[parentSwivel] = dependentList;
          }

          dependentList.Add(piece);

          // If this piece is also a swivel, create the dependency link
          if (piece.PersistentId.HasValue && piece.PersistentId.Value != 0 &&
              piece.PrefabName == PrefabNames.SwivelPrefabName)
          {
            // The parent swivel should be processed before this swivel
            if (!dependencies.TryGetValue(piece, out var parentDependencies))
            {
              parentDependencies = new List<StoredPieceData>();
              dependencies[piece] = parentDependencies;
            }

            if (!parentDependencies.Contains(parentSwivel))
            {
              parentDependencies.Add(parentSwivel);
            }
          }
        }
      }

      // Perform topological sort to get the correct processing order
      var sortedPieces = new List<StoredPieceData>();
      var visited = new HashSet<StoredPieceData>();
      var temporaryMark = new HashSet<StoredPieceData>();

      // Helper function for depth-first search
      void Visit(StoredPieceData piece)
      {
        if (temporaryMark.Contains(piece))
        {
          // Circular dependency detected - this is a problem, but we'll handle it gracefully
          LoggerProvider.LogWarning($"Circular dependency detected in swivel hierarchy for piece {piece.PrefabName}, ID: {piece.PersistentId}");
          return;
        }

        if (visited.Contains(piece))
          return;

        temporaryMark.Add(piece);

        // Visit all dependencies first (these are the swivels that should be processed BEFORE this one)
        if (dependencies.TryGetValue(piece, out var deps))
        {
          foreach (var dep in deps)
          {
            Visit(dep);
          }
        }

        temporaryMark.Remove(piece);
        visited.Add(piece);
        sortedPieces.Add(piece);
      }

      // Start with all swivel pieces
      foreach (var swivelPiece in swivelIdToPiece.Values)
      {
        if (!visited.Contains(swivelPiece))
        {
          Visit(swivelPiece);
        }
      }

      // Add any remaining non-swivel pieces
      foreach (var piece in pieces)
      {
        if (!visited.Contains(piece))
        {
          sortedPieces.Add(piece);
        }
      }

      return sortedPieces;
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
          LoggerProvider.LogMessage($"Could not find prefab piece by name. {vehicleDataPiece.PrefabName}");
          continue;
        }
        try
        {
          // todo parent might need to be dynamic. But setting to vehicle should align with swivels which first modify the position based on the top level parent. Then reparent into the nested one.
          var piecePrefabInstance = Object.Instantiate(piecePrefab, vehicleShip.PiecesController.transform.position + vehicleDataPiece.Position.ToVector3(), vehicleDataPiece.Rotation.ToQuaternion(), vehicleShip.PiecesController.transform);

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

          if (vehicleDataPiece.PrefabSwivelParentId.HasValue)
          {
            if (!instantiatedSwivelPieces.TryGetValue(vehicleDataPiece.PrefabSwivelParentId.Value, out var swivelComponentInstance))
            {
              LoggerProvider.LogError($"Error finding nested swivelComponent. This component should have been instantiated before this child however it was not found. prefab: {vehicleDataPiece.PrefabName} swivelId: {vehicleDataPiece.PrefabSwivelParentId}");
            }
            else
            {
              swivelComponentInstance.AddNewPiece(piecePrefabInstance);
            }
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