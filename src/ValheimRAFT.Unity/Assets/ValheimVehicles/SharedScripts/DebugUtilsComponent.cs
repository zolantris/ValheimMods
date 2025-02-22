#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class DebugUtilsComponent : MonoBehaviour
  {
    private const string CubeTextName = "CubeText";
    public bool autoUpdateColliders;
    public Material cubeMaterial;

    public Transform DebugUtilParent;
    private List<DebugItem> _instances = new();
    private bool isSetup;

    private void Start()
    {
      if (!cubeMaterial || !DebugUtilParent)
      {
        isSetup = false;
        return;
      }

      isSetup = true;
    }

    /// <summary>
    /// To be called during a FixedUpdate to update cube positions etc.
    /// </summary>
    private void FixedUpdate()
    {
      if (!isSetup) return;
      if (!autoUpdateColliders)
      {
      }
    }

    /// <summary>
    /// This renders all debug cubes. Also will destroy them if they are dereferencing objects.
    /// </summary>
    public void RenderAllDebugCubes()
    {
      _instances.ToList().ForEach(x =>
      {
        if (x.cube == null)
        {
          _instances.Remove(x);
          return;
        }
        RenderDebugCube(x);
      });
    }

    public static string SplitCamelCase(string input)
    {
      // Regex to find uppercase letters that are preceded by lowercase letters
      const string pattern = @"([a-z])([A-Z])";

      // Replace with the first letter, followed by a space, then the second letter
      var result = Regex.Replace(input, pattern, "$1 $2");

      // Trim to remove any leading or trailing spaces
      return result.Trim();
    }

    public void RenderDebugCube(DebugItem instance)
    {
      if (!autoUpdateColliders)
      {
        if (instance.cube) Destroy(instance.cube);
        _instances.Remove(instance);
        return;
      }

      if (instance.cube == null)
      {
        // Create the cube
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"debug_cube_{instance.name}";
        cube.transform.localScale = Vector3.one * 0.3f;
        var cubeCollider = cube.GetComponent<BoxCollider>();
        if (cubeCollider) Destroy(cubeCollider);

        // sets the cube to the instance.gameobject
        instance.cube = cube;

        var meshRenderer = cube.GetComponent<MeshRenderer>();
        meshRenderer.material =
          new Material(cubeMaterial)
          {
            color = instance.color
          };
        cube.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Add the text element as a child
        var textObj = new GameObject(CubeTextName);
        textObj.transform.SetParent(cube.transform);
        textObj.transform.localPosition =
          new Vector3(0, 1.2f, 0) + instance.textOffset; // Adjust height as needed
        var textMesh = textObj.AddComponent<TextMesh>();
        var text = SplitCamelCase(instance.name.Replace("_", " "));
        textMesh.text = text; // Set desired text
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.1f; // Adjust size as needed
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.yellow;
      }

      // Update the cube's position and set its parent
      instance.cube.transform.position = instance.position;
      instance.cube.transform.SetParent(
        DebugUtilParent,
        true);

      // Ensure the text always faces the camera
      var textTransform = instance.cube.transform.Find(CubeTextName);
      if (textTransform != null && Camera.main != null)
      {
        textTransform.LookAt(Camera.main.transform);
        textTransform.rotation =
          Quaternion.LookRotation(textTransform.forward *
                                  -1); // Flip to face correctly
      }
    }

    public struct DebugItem : IEquatable<DebugItem>
    {
      public GameObject cube;
      public Vector3 position;
      public string name;
      public Color color;
      public Vector3 textOffset;
      public bool Equals(DebugItem other)
      {
        return Equals(cube, other.cube);
      }
      public override bool Equals(object obj)
      {
        return obj is DebugItem other && Equals(other);
      }
      public override int GetHashCode()
      {
        return cube != null ? cube.GetHashCode() : 0;
      }
    }
  }
}