using UnityEngine;
namespace ValheimVehicles.Interfaces;

public interface IHoverableObj : Hoverable
{
  GameObject gameObject { get; }
}