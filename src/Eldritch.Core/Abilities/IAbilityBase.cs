namespace Eldritch.Core.Abilities
{
  public interface IAbilityBase
  {
    public void OnActivate(); // public
    public void OnDeactivate(); // public
    public void Activate(); // method clal from other apis
    public void Deactivate(); // method call from other apis
  }
}