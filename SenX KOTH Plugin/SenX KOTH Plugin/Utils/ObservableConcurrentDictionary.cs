using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SenX_KOTH_Plugin.Utils;

public class ObservableConcurrentDictionary<TKey, TValue> : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _dict = new();

    public IReadOnlyDictionary<TKey, TValue> Items => _dict;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly SynchronizationContext? _context = SynchronizationContext.Current;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_context != null)
            _context.Post(_ => PropertyChanged?.Invoke(this, new (propertyName)), null);
        else
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_context != null)
            _context.Post(_ => CollectionChanged?.Invoke(this, e), null);
        else
            CollectionChanged?.Invoke(this, e);
    }

    public bool TryAdd(TKey key, TValue value)
    {
        if (!_dict.TryAdd(key, value)) return false;

        OnCollectionChanged(new (NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        OnPropertyChanged(nameof(Count));
        return true;
    }

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        bool wasAdded = false;
        TValue? capturedOldValue = default;
        bool hadOldValue = false;

        TValue newValue = _dict.AddOrUpdate(key, k =>
            {
                wasAdded = true;
                return addValueFactory(k);
            },
            (k, oldVal) =>
            {
                capturedOldValue = oldVal;
                hadOldValue = true;
                return updateValueFactory(k, oldVal);
            });

        if (wasAdded)
        {
            OnCollectionChanged(new (NotifyCollectionChangedAction.Add, new List<KeyValuePair<TKey, TValue>> { new (key, newValue) }));
            OnPropertyChanged(nameof(Count));
        }
        else if (hadOldValue)
        {
            OnCollectionChanged(new (NotifyCollectionChangedAction.Replace, new List<KeyValuePair<TKey, TValue>> { new(key, newValue) }, new List<KeyValuePair<TKey, TValue>> { new (key, capturedOldValue!) }));
        }

        return newValue;
    }

    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        bool wasAdded = false;
        TValue? capturedOldValue = default;
        bool hadOldValue = false;

        TValue newValue = _dict.AddOrUpdate(key, k =>
            {
                wasAdded = true;
                return addValue;
            },
            (k, oldVal) =>
            {
                capturedOldValue = oldVal;
                hadOldValue = true;
                return updateValueFactory(k, oldVal);
            });

        if (wasAdded)
        {
            OnCollectionChanged(new(NotifyCollectionChangedAction.Add, new List<KeyValuePair<TKey, TValue>> { new (key, newValue) }));
            OnPropertyChanged(nameof(Count));
        }
        else if (hadOldValue)
        {
            OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, new List<KeyValuePair<TKey, TValue>> { new (key, newValue) }, new List<KeyValuePair<TKey, TValue>> { new (key, capturedOldValue!) }));
        }

        return newValue;
    }

    public bool TryRemove(TKey key, out TValue? value)
    {
        if (!_dict.TryRemove(key, out value)) return false;

        if (value != null)
            OnCollectionChanged(new(NotifyCollectionChangedAction.Remove, value));

        OnPropertyChanged(nameof(Count));
        return true;
    }
    
    public bool TryGetValue(TKey key, out TValue? value) => _dict.TryGetValue(key, out value);

    public void Clear()
    {
        _dict.Clear();
        OnCollectionChanged(new (NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(nameof(Count));
    }

    public bool TryUpdate(TKey key, TValue newValue)
    {
        if (!_dict.TryGetValue(key, out TValue? oldValue) || !_dict.TryUpdate(key, newValue, oldValue)) return false;
        OnCollectionChanged(new (NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, newValue), new KeyValuePair<TKey, TValue>(key, oldValue)));
        return true;
    }

    public TValue this[TKey key]
    {
        get => _dict[key];
        set
        {
            _dict[key] = value;
            OnCollectionChanged(new (NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value)));
        }
    }

    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    public int Count => _dict.Count;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}