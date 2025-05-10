using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace ValheimVehicles.Helpers;

public static class StringValidatorExtensions
{
  public static Regex GenerateRegexFromList(List<string> keyNames)
  {
    // Escape special characters in the strings and join them with a pipe (|) for OR condition
    var escapedPrefixes = new List<string>();
    foreach (var prefix in keyNames)
    {
      escapedPrefixes.Add(Regex.Escape(prefix));
    }

    // Create a regex pattern that matches the start of the string (^)
    // It will match any of the provided prefixes at the start of the string
    var pattern = "^(" + string.Join("|", escapedPrefixes) + ")";
    return new Regex(pattern, RegexOptions.Compiled);
  }
}