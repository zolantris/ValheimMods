using ValheimVehicles.Storage.Serialization;

namespace ValheimVehicles.ValheimVehicles.Structs;

public record struct VehicleChunkSizeData
{
  public SerializableVector3 position;
  public int chunkSize;
}