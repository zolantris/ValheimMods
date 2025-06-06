// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
namespace ValheimVehicles.SharedScripts.Modules
{
  // Helper for projection (Select)
  internal class ProjectionList<TIn, TOut> : IList<TOut>
  {
    private readonly FastQuery<TIn> _parent;
    private readonly Func<TIn, TOut> _selector;
    private List<TIn> _filtered;

    public ProjectionList(FastQuery<TIn> parent, Func<TIn, TOut> selector)
    {
      _parent = parent;
      _selector = selector;
      _filtered = _parent.ToList();
    }

    public int Count => _filtered.Count;
    public bool IsReadOnly => true;
    public TOut this[int index] { get => _selector(_filtered[index]); set => throw new NotSupportedException(); }

    public IEnumerator<TOut> GetEnumerator()
    {
      for (var i = 0; i < _filtered.Count; i++)
        yield return _selector(_filtered[i]);
    }

    // Rest: explicit interface implementations (not shown for brevity)
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    public void Add(TOut item)
    {
      throw new NotSupportedException();
    }
    public void Clear()
    {
      throw new NotSupportedException();
    }
    public bool Contains(TOut item)
    {
      throw new NotSupportedException();
    }
    public void CopyTo(TOut[] array, int arrayIndex)
    {
      throw new NotSupportedException();
    }
    public bool Remove(TOut item)
    {
      throw new NotSupportedException();
    }
    public int IndexOf(TOut item)
    {
      throw new NotSupportedException();
    }
    public void Insert(int index, TOut item)
    {
      throw new NotSupportedException();
    }
    public void RemoveAt(int index)
    {
      throw new NotSupportedException();
    }
  }

  public class FastQuery<T>
  {
    private readonly IList<T> _source;
    private readonly List<Func<T, bool>> _filters = new();
    private Func<T, object> _selector = null;
    private Comparison<T> _sorter = null;

    public FastQuery(IList<T> source)
    {
      _source = source;
    }

    public FastQuery<T> Where(Func<T, bool> predicate)
    {
      _filters.Add(predicate);
      return this;
    }

    public FastQuery<T> OrderBy(Comparison<T> comparer)
    {
      _sorter = comparer;
      return this;
    }

    public FastQuery<TResult> Select<TResult>(Func<T, TResult> selector)
    {
      var parent = this;
      return new FastQuery<TResult>(new ProjectionList<T, TResult>(parent, selector));
    }

    public List<T> ToList()
    {
      var list = new List<T>(_source.Count);
      for (var i = 0; i < _source.Count; i++)
      {
        var item = _source[i];
        var accept = true;
        foreach (var filter in _filters)
          if (!filter(item))
          {
            accept = false;
            break;
          }
        if (accept)
          list.Add(item);
      }

      if (_sorter != null)
        list.Sort(_sorter);

      return list;
    }

    // Optional: ToArray, FirstOrDefault, etc.
  }
}