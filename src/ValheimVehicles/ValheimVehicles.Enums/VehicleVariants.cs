using Newtonsoft.Json;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
namespace ValheimVehicles.Enums;

[JsonConverter(typeof(SafeVehicleVariantConverter))]
public enum VehicleVariant
{
  Water,
  Sub,
  Land,
  All,
  Air
}