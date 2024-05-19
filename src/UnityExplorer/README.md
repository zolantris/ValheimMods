# UnityExplorer 

🔍 An in-game UI for exploring, debugging and modifying Unity games.
✔️ Supports most Unity versions from 5.2 to 2021+ (IL2CPP and Mono).

[GitHub](https://github.com/sinai-dev/UnityExplorer)

# Manual install

BepInEx:

1. Take the `UnityExplorer.BIE.[version].dll` file and the `UniverseLib.[version].dll` file and put them in `BepInEx\plugins\`.

MelonLoader:

1. Take the `UnityExplorer.ML.[version].dll` file and put it in the `Mods\` folder created by MelonLoader.
2. Take the `UniverseLib.[version].dll` file can put it in the `UserLibs/` folder created by MelonLoader.

# Features

### Object Explorer

* Use the <b>Scene Explorer</b> tab to traverse the active scenes, as well as the DontDestroyOnLoad and HideAndDontSave objects.
  * The "HideAndDontSave" scene contains objects with that flag, as well as Assets and Resources which are not in any scene but behave the same way.
  * You can use the Scene Loader to easily load any of the scenes in the build (may not work for Unity 5.X games)
* Use the <b>Object Search</b> tab to search for Unity objects (including GameObjects, Components, etc), C# Singletons or Static Classes.
  * Use the UnityObject search to look for any objects which derive from `UnityEngine.Object`, with optional filters
  * The singleton search will look for any classes with a typical "Instance" field, and check it for a current value. This may cause unexpected behaviour in some IL2CPP games as we cannot distinguish between true properties and field-properties, so some property accessors will be invoked.

### Inspector

The inspector is used to see detailed information on objects of any type and manipulate their values, as well as to inspect C# Classes with static reflection.

* The <b>GameObject Inspector</b> (tab prefix `[G]`) is used to inspect a `GameObject`, and to see and manipulate its Transform and Components.
  * You can edit any of the input fields in the inspector (excluding readonly fields) and press <b>Enter</b> to apply your changes. You can also do this to the GameObject path as a way to change the GameObject's parent. Press the <b>Escape</b> key to cancel your edits.
  * <i>note: When inspecting a GameObject with a Canvas, the transform controls may be overridden by the RectTransform anchors.</i>
* The <b>Reflection Inspectors</b> (tab prefix `[R]` and `[S]`) are used for everything else
  * Automatic updating is not enabled by default, and you must press Apply for any changes you make to take effect.
  * Press the `▼` button to expand certain values such as strings, enums, lists, dictionaries, some structs, etc
  * Use the filters at the top to quickly find the members you are looking for
  * For `Texture2D`, `Image, `Sprite` and `Material` objects, there is a `View Texture` button at the top of the inspector which lets you view the Texture(s) and save them as a PNG file. 
  * For `AudioClip` objects there is a `Show Player` button which opens an audio player widget. For clips which are loaded as `DecompressOnLoad`, there is also a button to save them to a `.wav` file. 

### C# Console

* The C# Console uses the `Mono.CSharp.Evaluator` to define temporary classes or run immediate REPL code.
* You can execute a script automatically on startup by naming it `startup.cs` and placing it in the `sinai-dev-UnityExplorer\Scripts\` folder (this folder will be created where you placed the DLL file).
* See the "Help" dropdown in the C# console menu for more detailed information.

### Hook Manager

* The Hooks panel allows you to hook methods at the click of a button for debugging purposes.
  * Simply enter any class and hook the methods you want from the menu. 
  * You can edit the source code of the generated hook with the "Edit Hook Source" button. Accepted method names are `Prefix` (which can return `bool` or `void`), `Postfix`, `Finalizer` (which can return `Exception` or `void`), and `Transpiler` (which must return `IEnumerable<HarmonyLib.CodeInstruction>`). You can define multiple patches if you wish.

### Mouse-Inspect

* The "Mouse Inspect" dropdown in the "Inspector" panel allows you to inspect objects under the mouse.
  * <b>World</b>: uses Physics.Raycast to look for Colliders
  * <b>UI</b>: uses GraphicRaycasters to find UI objects

### Freecam

* UnityExplorer provides a basic Free Camera which you can control with your keyboard and mouse.
* Unlike all other features of UnityExplorer, you can still use Freecam while UnityExplorer's menu is hidden.
* Supports using the game's main Camera or a separate custom Camera.
* See the Freecam panel for further instructions and details.

### Clipboard

* The "Clipboard" panel allows you to see your current paste value, or clear it (resets it to `null`)
  * Can copy the value from any member in a Reflection Inspector, Enumerable or Dictionary, and from the target of any Inspector tab
  * Can paste values onto any member in a Reflection Inspector
  * Non-parsable arguments in Method/Property Evaluators allow pasting values
  * The C# Console has helper methods `Copy(obj)` and `Paste()` for accessing the Clipboard

### Settings

* You can change the settings via the "Options" tab of the menu, or directly from the config file.
  * BepInEx: `BepInEx\config\com.sinai.unityexplorer.cfg`
  * MelonLoader: `UserData\MelonPreferences.cfg`
  * Standalone `{DLL_location}\sinai-dev-UnityExplorer\config.cfg`

### Settings

* You can change the settings via the "Options" tab of the menu, or directly from the config file.

# Acknowledgments

* [ManlyMarco](https://github.com/ManlyMarco) for [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor) \[[license](THIRDPARTY_LICENSES.md#runtimeunityeditor-license)\], the ScriptEvaluator from RUE's REPL console was used as the base for UnityExplorer's C# console.
* [denikson](https://github.com/denikson) (aka Horse) for [mcs-unity](https://github.com/sinai-dev/mcs-unity) \[no license\], used as the `Mono.CSharp` reference for the C# Console.