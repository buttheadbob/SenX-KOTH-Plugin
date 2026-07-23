using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SenX_KOTH_Plugin.Utils;

/// <summary>
/// Represents a thread-safe, observable hash set that supports notifying listeners of dynamic changes
/// such as when items are added, removed, or the collection is cleared. This class ensures
/// thread-safe operations and allows concurrent access for enumeration.
/// </summary>
/// <typeparam name="T">
/// The type of elements in the set. Must be non-nullable.
/// </typeparam>
public class ObservableConcurrentHashSet<T> : INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyCollection<T> where T : notnull
{
    private readonly HashSet<T> _items;
    private readonly object _lock = new();
    
    public ObservableConcurrentHashSet() => _items = [];
    
    public ObservableConcurrentHashSet(IEqualityComparer<T>? comparer) => _items = new HashSet<T>(comparer);

    /// <summary>
    /// Gets the number of elements contained in the observable concurrent hash set.
    /// This property reflects the current count of items in the set and notifies
    /// listeners when the count changes due to additions or removals.
    /// </summary>
    public int Count
    {
        get { lock (_lock) return _items.Count; }
    }

    /// <summary>
    /// Occurs when the collection changes, such as when items are added, removed, or the entire collection is reset.
    /// This event is part of the <see cref="INotifyCollectionChanged"/> interface implementation
    /// and provides a mechanism to notify listeners of dynamic changes to the collection.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value in the object changes.
    /// This event is part of the <see cref="INotifyPropertyChanged"/> interface implementation
    /// and provides a mechanism to notify subscribers of updates to property values.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Attempts to add the specified item to the observable concurrent hash set.
    /// If the item is successfully added, notifications for collection and property changes
    /// are triggered.
    /// </summary>
    /// <param name="item">The item to add to the set. Must not be null.</param>
    /// <returns>
    /// Returns <c>true</c> if the item was successfully added to the set;
    /// otherwise, <c>false</c> if the item already exists in the set.
    /// </returns>
    public bool Add(T item)
    {
        bool added;
        lock (_lock)
        {
            added = _items.Add(item);
        }

        if (added)
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        return added;
    }

    /// <summary>
    /// Attempts to remove the specified item from the observable concurrent hash set.
    /// If the item is successfully removed, notifications for collection and property changes
    /// are triggered.
    /// </summary>
    /// <param name="item">The item to remove from the set. Must not be null.</param>
    /// <returns>
    /// Returns <c>true</c> if the item was successfully removed from the set;
    /// otherwise, <c>false</c> if the item does not exist in the set.
    /// </returns>
    public bool Remove(T item)
    {
        bool removed;
        lock (_lock)
        {
            removed = _items.Remove(item);
        }

        if (removed)
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
        }

        return removed;
    }

    /// <summary>
    /// Removes all items from the observable concurrent hash set.
    /// Notifications for collection and property changes are triggered
    /// if the set is not already empty.
    /// </summary>
    public void Clear()
    {
        bool hadItems;
        lock (_lock)
        {
            hadItems = _items.Count > 0;
            if (hadItems)
                _items.Clear();
        }

        if (hadItems)
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }
    }

    /// <summary>
    /// Determines whether the observable concurrent hash set contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate in the set. Must not be null.</param>
    /// <returns>
    /// Returns <c>true</c> if the item is found in the set; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(T item)
    {
        lock (_lock) return _items.Contains(item);
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> containing all the elements in the observable concurrent hash set.
    /// The operation is thread-safe, ensuring a consistent snapshot of the set at the time of invocation.
    /// </summary>
    /// <returns>
    /// A <see cref="List{T}"/> containing all elements in the set at the point in time the snapshot was taken.
    /// </returns>
    public List<T> ToList()
    {
        lock (_lock) return [.. _items];
    }

    /// <summary>
    /// Returns an enumerator that iterates through the observable concurrent hash set.
    /// The enumeration represents a snapshot of the set's state at the time it is requested,
    /// ensuring thread-safe access without locking the collection during iteration.
    /// </summary>
    /// <returns>
    /// An enumerator for enumerating the elements in the set.
    /// </returns>
    public IEnumerator<T> GetEnumerator()
    {
        // Snapshot enumeration — safe, no lock retention
        List<T> snapshot;
        lock (_lock) snapshot = [.. _items];
        return snapshot.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the observable concurrent hash set.
    /// The enumeration represents a snapshot of the set's state at the time it is requested,
    /// ensuring thread-safe access without locking the collection during iteration.
    /// </summary>
    /// <returns>
    /// An enumerator for iterating through the elements in the set.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, object? item = null)
    {
        CollectionChanged?.Invoke(this, action switch
        {
            NotifyCollectionChangedAction.Add => new (action, item),
            NotifyCollectionChangedAction.Remove => new (action, item),
            NotifyCollectionChangedAction.Reset => new (action),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new (propertyName));
    }
}