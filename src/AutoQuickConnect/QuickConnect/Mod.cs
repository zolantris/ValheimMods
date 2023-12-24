using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace QuickConnect;

[BepInPlugin("net.bdew.valheim.QuickConnect", "QuickConnect", "1.6.0")]
internal class Mod : BaseUnityPlugin
{
	public static ManualLogSource Log;

	public static ConfigEntry<float> windowPosX;

	public static ConfigEntry<float> windowPosY;

	public static ConfigEntry<int> buttonFontSize;

	public static ConfigEntry<int> labelFontSize;

	public static ConfigEntry<int> windowWidth;

	public static ConfigEntry<int> windowHeight;

	private void Awake()
	{
		Log = BepInEx.Logging.Logger.CreateLogSource("QuickConnect");
		windowPosX = base.Config.Bind("UI", "WindowPosX", 20f);
		windowPosY = base.Config.Bind("UI", "WindowPosY", 20f);
		buttonFontSize = base.Config.Bind("UI", "ButtonFontSize", 0);
		labelFontSize = base.Config.Bind("UI", "LabelFontSize", 0);
		windowWidth = base.Config.Bind("UI", "WindowWidth", 250);
		windowHeight = base.Config.Bind("UI", "WindowHeight", 50);
		Harmony harmony = new Harmony("net.bdew.valheim.QuickConnect");
		harmony.PatchAll();
	}
}
