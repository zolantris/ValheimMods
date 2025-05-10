using System;
using Newtonsoft.Json;
using ValheimVehicles.Enums;
namespace ValheimVehicles.Helpers;

public class SafeVehicleVariantConverter : JsonConverter<VehicleVariant>
{
  public override void WriteJson(JsonWriter writer, VehicleVariant value, JsonSerializer serializer)
  {
    writer.WriteValue(value.ToString());
  }

  public override VehicleVariant ReadJson(JsonReader reader, Type objectType, VehicleVariant existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.String)
    {
      var enumText = reader.Value?.ToString();
      if (!string.IsNullOrEmpty(enumText))
      {
        // Manual mapping without TryParse to avoid recursion
        foreach (var name in Enum.GetNames(typeof(VehicleVariant)))
        {
          if (string.Equals(name, enumText, StringComparison.OrdinalIgnoreCase))
          {
            return (VehicleVariant)Enum.Parse(typeof(VehicleVariant), name);
          }
        }
      }
    }

    // fallback to All
    return VehicleVariant.All;
  }
}