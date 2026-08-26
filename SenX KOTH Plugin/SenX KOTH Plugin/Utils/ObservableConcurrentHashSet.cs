using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SenX_KOTH_Plugin.Utils;

/// <summary>Thread-safe, observable hash set. Enumeration returns a snapshot.</summary>
public class ObservableConcurrentHashSet<T> : INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyCollection<T> where T : notnull
{
    private readonly HashSet<T> _items;
    private readonly object _lock = new();

    public ObservableConcurrentHashSet() => _items = [];

    public ObservableConcurrentHashSet(IEqualityComparer<T>? comparer) => _items = new HashSet<T>(comparer);

    public int Count { get { lock (_lock) return _items.Count; } }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Add(T item)
    {
        bool added;
        lock (_lock) added = _items.Add(item);

        if (added)
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        return added;
    }

    public bool Remove(T item)
    {
        bool removed;
        lock (_lock) removed = _items.Remove(item);

        if (removed)
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
        }

        return removed;
    }

    public void Clear()
    {
        bool hadItems;
        lock (_lock)
        {
            hadItems = _items.Count > 0;
            if (hadItems) _items.Clear();
        }

        if (hadItems)
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }
    }

    public bool Contains(T item) { lock (_lock) return _items.Contains(item); }

    public List<T> ToList() { lock (_lock) return [.. _items]; }

    public IEnumerator<T> GetEnumerator()
    {
        List<T> snapshot;
        lock (_lock) snapshot = [.. _items];
        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, object? item = null)
    {
        CollectionChanged?.Invoke(this, action switch
        {
            NotifyCollectionChangedAction.Add => new NotifyCollectionChangedEventArgs(action, item),
            NotifyCollectionChangedAction.Remove => new NotifyCollectionChangedEventArgs(action, item),
            NotifyCollectionChangedAction.Reset => new NotifyCollectionChangedEventArgs(action),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
