#region

using System;

#endregion

namespace ValheimVehicles.SharedScripts.UI.Observables
{
  [Serializable]
  public class ObservableField<T>
  {
    private T _value;

    public ObservableField(T initial)
    {
      _value = initial;
    }

    public T Value
    {
      get => _value;
      set
      {
        if (!Equals(_value, value))
        {
          _value = value;
          OnValueChanged?.Invoke(_value);
        }
      }
    }

    public event Action<T> OnValueChanged;

    public void ForceNotify()
    {
      OnValueChanged?.Invoke(_value);
    }
  }
}