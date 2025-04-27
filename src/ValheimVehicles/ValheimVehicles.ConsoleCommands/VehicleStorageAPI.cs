using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts; // Assume you want the shared scripts style.
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TMPro;
using UnityEngine.UI;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.GUI;
using ValheimVehicles.Components;
using Object = UnityEngine.Object;

namespace ValheimVehicles.API;

public static class VehicleStorageAPI
{
  private static readonly string BaseFolderPath = Path.Combine(Paths.ConfigPath, $"{ValheimVehiclesPlugin.Author}-{ValheimVehiclesPlugin.ModName}");
  private static readonly string SavedVehiclesFolderPath = Path.Combine(BaseFolderPath, "SavedVehicles");

  static VehicleStorageAPI()
  {
    EnsureStorageFolderExists();
  }

  // This will handle all enums and fallback to ALL if there is an unknown value.
  [JsonConverter(typeof(SafeVehicleTypeEnumConverter))]
  public enum VehicleTypeEnum
  {
    Water,
    Sub,
    Land,
    All,
    Air
  }

  [Serializable]
  public struct SerializableVector3
  {
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    public SerializableVector3(Vector3 v)
    {
      x = v.x;
      y = v.y;
      z = v.z;
    }

    public readonly Vector3 ToVector3()
    {
      return new Vector3(x, y, z);
    }
  }

  [Serializable]
  public struct SerializableQuaternion
  {
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(float x, float y, float z, float w)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = w;
    }

    public SerializableQuaternion(Quaternion q)
    {
      x = q.x;
      y = q.y;
      z = q.z;
      w = q.w;
    }

    public readonly Quaternion ToQuaternion()
    {
      return new Quaternion(x, y, z, w);
    }
  }

  public class SafeVehicleTypeEnumConverter : JsonConverter<VehicleTypeEnum>
  {
    public override void WriteJson(JsonWriter writer, VehicleTypeEnum value, JsonSerializer serializer)
    {
      writer.WriteValue(value.ToString());
    }

    public override VehicleTypeEnum ReadJson(JsonReader reader, Type objectType, VehicleTypeEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.String)
      {
        var enumText = reader.Value?.ToString();
        if (!string.IsNullOrEmpty(enumText))
        {
          // Manual mapping without TryParse to avoid recursion
          foreach (var name in Enum.GetNames(typeof(VehicleTypeEnum)))
          {
            if (string.Equals(name, enumText, StringComparison.OrdinalIgnoreCase))
            {
              return (VehicleTypeEnum)Enum.Parse(typeof(VehicleTypeEnum), name);
            }
          }
        }
      }

      // fallback to All
      return VehicleTypeEnum.All;
    }
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
    public VehicleTypeEnum VehicleType;
  }

  [Serializable]
  public struct StoredPieceData
  {
    public SerializableVector3 Position;
    public SerializableQuaternion Rotation;
    public string PrefabName;
    public int PrefabId;
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

  public static VehicleTypeEnum GetVehicleType(VehicleShip vehicle)
  {
    return vehicle.IsLandVehicle ? VehicleTypeEnum.Land : VehicleTypeEnum.All;
  }

  public static void SaveClosestVehicle()
  {
    if (!Player.m_localPlayer) return;
    var closestShip = VehicleCommands.GetNearestVehicleShip(Player.m_localPlayer.transform.position);
    if (closestShip == null || closestShip.PiecesController == null) return;

    var pieces = new List<StoredPieceData>();

    foreach (var piece in closestShip.PiecesController.m_nviewPieces)
    {
      if (piece == null) continue;
      var pieceTransform = piece.transform;
      var storedPieceData = new StoredPieceData
      {
        PrefabId = piece.GetInstanceID(),
        PrefabName = piece.GetPrefabName(),
        Position = new SerializableVector3(pieceTransform.localPosition),
        Rotation = new SerializableQuaternion(pieceTransform.localRotation)
      };
      pieces.Add(storedPieceData);
    }
    var vehicleType = GetVehicleType(closestShip);
    var localDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    var vehicleName = string.Join("_", Player.m_localPlayer.GetPlayerName(), vehicleType.ToString(), localDate);

    var data = new StoredVehicleData
    {
      VehicleName = "", // we never want this value saved. We only use filename.
      ModVersion = ValheimRaftPlugin.Version,
      Pieces = pieces,
      Settings = new VehicleSettings
      {
        VehicleType = vehicleType
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
      VehicleShip.CanInitHullPiece = false;
      vehiclePrefabInstance = Object.Instantiate(vehiclePrefab, position, Quaternion.identity);
      if (!vehiclePrefabInstance) return;
    }
    catch (Exception e)
    {
      VehicleShip.CanInitHullPiece = true;
      return;
    }
    VehicleShip.CanInitHullPiece = true;

    if (vehiclePrefabInstance == null) return;


    var vehicleShip = vehiclePrefabInstance.GetComponent<VehicleShip>();
    if (vehicleShip == null) return;
    if (vehicleShip.PiecesController == null)
    {
      LoggerProvider.LogError("Somehow have a null piece controller on a valid vehicle");

      // destroy this component if it's unhealthy.
      var nv = vehicleShip.GetComponent<ZNetView>();
      if (nv != null) nv.Destroy();
      return;
    }

    var prefabsToAdd = new List<GameObject>();

    foreach (var vehicleDataPiece in vehicleData.Pieces)
    {
      var piecePrefab = PrefabManager.Instance.GetPrefab(vehicleDataPiece.PrefabName);
      if (piecePrefab == null)
      {
        LoggerProvider.LogMessage($"Could not find prefab piece by name. {vehicleDataPiece.PrefabName}");
        continue;
      }
      // todo might need to delay adding to parent
      var piecePrefabInstance = Object.Instantiate(piecePrefab, vehicleShip.PiecesController.transform.position + vehicleDataPiece.Position.ToVector3(), vehicleDataPiece.Rotation.ToQuaternion(), vehicleShip.PiecesController.transform);
      prefabsToAdd.Add(piecePrefabInstance);
    }

    foreach (var pieceInstance in prefabsToAdd)
    {
      vehicleShip.PiecesController.AddNewPiece(pieceInstance);
    }

    vehicleShip.PiecesController.StartActivatePendingPieces();

    prefabsToAdd.Clear();
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
      LoggerProvider.LogError($"[VehicleStorageAPI] Failed to save vehicle '{vehicleName}': {ex}");
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
      Debug.LogError($"[VehicleStorageAPI] Failed to delete vehicle '{vehicleName}': {ex}");
    }
  }

  public static GameObject GetVehicleTypeFromVehicleData(StoredVehicleData vehicleData)
  {
    switch (vehicleData.Settings.VehicleType)
    {
      case VehicleTypeEnum.Land:
        return PrefabManager.Instance.GetPrefab(PrefabNames.LandVehicle);
      case VehicleTypeEnum.Air:
        return PrefabManager.Instance.GetPrefab(PrefabNames.AirVehicle);
      case VehicleTypeEnum.Water:
      case VehicleTypeEnum.Sub:
      case VehicleTypeEnum.All:
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
      Debug.LogError($"[VehicleStorageAPI] Failed to load vehicle '{vehicleName}': {ex}");
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
          // Vehicle name should match the file name. This will prevent problems.
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
      Debug.LogError($"[VehicleStorageAPI] Failed to load vehicles: {ex}");
    }

    return _allVehicles;
  }
}