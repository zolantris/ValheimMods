using System.IO;
using System.Linq;
namespace ModSync.Utils;

public class PathUtils
{
  public static string SafeCombine(params string[] parts)
  {
    if (parts == null || parts.Length == 0)
      return string.Empty;

    var last = parts.LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? "";

    // windows can have both escape hatch slash or / for json parse.
    var endsWithAnySeparator = last.EndsWith("/") || last.EndsWith("\\");

    var sanitizedStrings = parts
      .Where(s => !string.IsNullOrEmpty(s))
      .Select((s, i) =>
        i == 0 ? s.TrimEnd('/', '\\') : s.Trim('/', '\\')
      ).ToArray();

    var output = Path.Combine(sanitizedStrings);

    if (endsWithAnySeparator)
    {
      output += Path.DirectorySeparatorChar;
    }

    return output;
  }
}