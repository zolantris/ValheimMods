// Add under namespace ModSync.Config (same file)

using System.Collections.Generic;
namespace ModSync.Config;

// Top-level JSON patch target
public class JsonSyncTarget
{
  /// <summary>Absolute or relative file path to JSON to mutate.</summary>
  public string inputFilePath;

  /// <summary>
  /// Key-value pairs to set in the JSON.
  /// Keys support simple dot-paths like "a.b.c" (top-level key recommended).
  /// Values can interpolate variables like ${Version}.
  /// </summary>
  public Dictionary<string, string> setFields;
}