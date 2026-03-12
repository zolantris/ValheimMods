using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts.Validation;
using ValheimVehicles.UI;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class GuiConfig : BepInExBaseConfig<GuiConfig>
{
  public const string SectionKey = "Gui";
  public static ConfigEntry<Vector2> SwivelPanelLocation = null!;
  public static ConfigEntry<Vector2> VehicleCommandsPanelLocation = null!;
  // public static ConfigEntry<Vector2> VehicleConfigPanelLocation = null!;
  // public static ConfigEntry<Vector2> SailPanelLocation = null!;

  public const float DefaultPanelWidth = 500f;
  public const float DefaultPanelHeight = 500f;

  public const string ProtectedScreenValue = "This is a protected value and will not allow panels off screen.";

  public static void EnsurePanelInScreenBounds(Vector2 inputCoordinates, ConfigEntry<Vector2> configEntry)
  {
    if (ZNet.instance == null)
    {
      return;
    }

    var nextCoordinates = inputCoordinates;
    var hasMutated = false;

    var maxX = Mathf.Max(10f, Screen.width - DefaultPanelWidth);
    var maxY = Mathf.Max(10f, Screen.height - DefaultPanelHeight);

    if (inputCoordinates.x < 10f)
    {
      nextCoordinates.x = 10f;
      hasMutated = true;
    }
    else if (inputCoordinates.x > maxX)
    {
      nextCoordinates.x = maxX;
      hasMutated = true;
    }

    if (inputCoordinates.y < 10f)
    {
      nextCoordinates.y = 10f;
      hasMutated = true;
    }
    else if (inputCoordinates.y > maxY)
    {
      nextCoordinates.y = maxY;
      hasMutated = true;
    }

    if (hasMutated)
    {
      configEntry.Value = nextCoordinates;
    }
  }

  private static void CreatePanelLocationConfig(ConfigFile config, string key, string description, out ConfigEntry<Vector2> configEntry)
  {
    var entry = config.BindUnique(
      SectionKey,
      key,
      new Vector2(0.5f, 0.5f),
      ConfigHelpers.CreateConfigDescription(description, false, false)
    );

    void ClampEntry()
    {
      EnsurePanelInScreenBounds(entry.Value, entry);
    }

    // Clamp the stored position once before listeners are attached.
    ClampEntry();

    entry.SettingChanged += (_, _) => ClampEntry();

    // If the client gets resized, clamp persisted panel position to the new bounds.
    ScreenSizeWatcher.OnScreenSizeChanged += (_) => ClampEntry();

    configEntry = entry;
  }

  public override void OnBindConfig(ConfigFile config)
  {
    ShouldSkipSyncOnBind = true;
    CreatePanelLocationConfig(config, "SwivelPanelLocation", $"SwivelPanel screen location. {ProtectedScreenValue}", out SwivelPanelLocation);
    CreatePanelLocationConfig(config, "VehicleCommandsPanelLocation", $"VehicleCommands panel screen location. {ProtectedScreenValue}", out VehicleCommandsPanelLocation);
#if DEBUG
    // CreatePanelLocationConfig(config, "SailPanelLocation", $"SailPanel screen location. {ProtectedScreenValue}", out SailPanelLocation);
    // CreatePanelLocationConfig(config, "VehicleConfigPanelLocation", $"VehicleConfig panel screen location. {ProtectedScreenValue}", out VehicleConfigPanelLocation);
#endif
  }
}