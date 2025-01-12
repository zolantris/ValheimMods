using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TriggerButtonEditorWindow : EditorWindow
{
    public const string MethodKeyword = "Trigger";
    private GameObject selectedObject;

    private void OnGUI()
    {
        GUILayout.Label(
            "Add any component and trigger methods related to that component");
        // Display a field to select the GameObject with the components
        selectedObject = (GameObject)EditorGUILayout.ObjectField(
            "Select GameObject", selectedObject, typeof(GameObject), true);

        // If a GameObject is selected, try to get all components
        if (selectedObject != null)
        {
            // Get all components attached to the selected GameObject (top-level components)
            var components = selectedObject.GetComponents<Component>();


            // Iterate through each component and find methods that start with "Trigger"
            foreach (var component in components)
            {
                var methods = component.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance);
                if (methods.Length > 0)
                    GUILayout.Label($"Component: {component.GetType().Name}\n");

                foreach (var method in methods)
                    // If the method name starts with "Trigger"
                    if (method.Name.StartsWith(MethodKeyword))
                        // Create a button for each matching method
                        if (GUILayout.Button($"Run: {method.Name}"))
                            // Invoke the method when the button is clicked
                            method.Invoke(component, null);
            }
        }
        else
        {
            GUILayout.Label("Please select a GameObject.");
        }
    }

    [MenuItem("Tools/Trigger Method Buttons")]
    public static void ShowWindow()
    {
        // Open the window
        GetWindow<TriggerButtonEditorWindow>("Trigger Methods");
    }
}