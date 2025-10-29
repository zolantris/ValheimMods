using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Zolantris.Shared
{
  public interface IParticleSystemCollisionForwarder
  {
    void OnParticleCollisionEvent(GameObject other, [CanBeNull] List<ParticleCollisionEvent> collisionEvent);
  }

  [RequireComponent(typeof(ParticleSystem))]
  public class ParticleSystem_CollisionForwarder : MonoBehaviour
  {
    private ParticleSystem _ps;
    public IParticleSystemCollisionForwarder _forwarder;
    private readonly List<ParticleCollisionEvent> _events = new();
    public bool ShouldCollectEvents = false;

    private void Awake()
    {
      _ps = GetComponent<ParticleSystem>();
      _forwarder = GetComponentInParent<IParticleSystemCollisionForwarder>();
    }

    public void SetForwarder(IParticleSystemCollisionForwarder forwarder)
    {
      _forwarder = forwarder;
    }

    private void OnParticleCollision(GameObject other)
    {
      if (ShouldCollectEvents)
      {
        ParticlePhysicsExtensions.GetCollisionEvents(_ps, other, _events);
      }

      var events = ShouldCollectEvents ? _events : null;

      _forwarder?.OnParticleCollisionEvent(other, events);
      LoggerProvider.LogDebug($"[ParticleSystem_CollisionForwarder] hit {other.name}  events={events}");
    }
  }
}