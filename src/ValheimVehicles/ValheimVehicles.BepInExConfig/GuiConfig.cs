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

    if (inputCoordinates.x < 10)
    {
      nextCoordinates.x = 10;
      hasMutated = true;
    }
    if (inputCoordinates.y < 10)
    {
      nextCoordinates.y = 10;
      hasMutated = true;
    }

    if (inputCoordinates.x > Screen.width - 10)
    {
      nextCoordinates.x = Screen.width - DefaultPanelWidth;
      hasMutated = true;
    }

    if (inputCoordinates.y > Screen.height - DefaultPanelHeight)
    {
      nextCoordinates.y = Screen.height - DefaultPanelHeight;
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

    entry.SettingChanged += (_, _) =>
      EnsurePanelInScreenBounds(entry.Value, entry);

    // If you resize valheim client it will not update this property and cause problems.
    ScreenSizeWatcher.OnScreenSizeChanged += (_) =>
    {
      if (entry != null && entry.Value != Vector2.zero)
      {
        EnsurePanelInScreenBounds(entry.Value, entry);
      }
    };

    EnsurePanelInScreenBounds(entry.Value, entry);

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