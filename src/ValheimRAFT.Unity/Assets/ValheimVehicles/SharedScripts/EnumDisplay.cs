// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using ValheimVehicles.SharedScripts.UI;

namespace ValheimVehicles.SharedScripts.Helpers
{
  public static class EnumDisplay
  {
    public static string[] GetSwivelModeNames()
    {
      return new[]
      {
        SwivelUIPanelStrings.SwivelMode_None,
        SwivelUIPanelStrings.SwivelMode_Rotate,
        SwivelUIPanelStrings.SwivelMode_Move,
        SwivelUIPanelStrings.SwivelMode_TargetWind,
#if DEBUG
        SwivelUIPanelStrings.SwivelMode_TargetEnemy
#endif
      };
    }

    public static string[] GetMotionStateNames()
    {
      return new[]
      {
        ModTranslations.Mechanism_Swivel_MotionState_AtStart ?? "At Start",
        ModTranslations.Mechanism_Swivel_MotionState_ToStart ?? "To Start",
        ModTranslations.Mechanism_Swivel_MotionState_AtTarget ?? "At Target",
        ModTranslations.Mechanism_Swivel_MotionState_ToTarget ?? "To Target"
      };
    }

    public static Dictionary<string, SwivelMode> GetSwivelModeReverseLookup()
    {
      var map = new Dictionary<string, SwivelMode>(StringComparer.OrdinalIgnoreCase);
      var names = GetSwivelModeNames();

      for (var i = 0; i < names.Length; i++)
      {
        map[names[i]] = (SwivelMode)i;
      }

      return map;
    }

    public static Dictionary<string, MotionState> GetMotionStateReverseLookup()
    {
      var map = new Dictionary<string, MotionState>(StringComparer.OrdinalIgnoreCase);
      var names = GetMotionStateNames();

      for (var i = 0; i < names.Length; i++)
      {
        map[names[i]] = (MotionState)i;
      }

      return map;
    }
  }
}