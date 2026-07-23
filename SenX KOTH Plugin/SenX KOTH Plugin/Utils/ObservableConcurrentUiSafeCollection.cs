using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SenX_KOTH_Plugin.Utils;

/// <summary>
/// Thread-safe, UI-safe observable collection.
/// All mutations are serialised through a channel so UI-bound event handlers
/// are always raised on the original synchronization context.
///
/// Implements <see cref="ICollection{T}"/> so all three major serializers work
/// with zero configuration, converters, or package imports:
///   System.Text.Json  serializes IEnumerable&lt;T&gt;, deserializes via Add(T)
///   Newtonsoft.Json   serializes IEnumerable&lt;T&gt;, deserializes via Add(T)
///   YamlDotNet        serializes IEnumerable&lt;T&gt;, deserializes via Add(T)
/// </summary>
public sealed class ObservableConcurrentUiSafeCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, IDisposable, ICollection<T>, IList
{
    private readonly SynchronizationContext _synchronizationContext;
    private readonly List<T> _items = [];
    private readonly object _itemsLock = new();
    private readonly SenxChannel<ObservableConcurrentUiSafeCollectionOperation<T>> _channel;
    private readonly Task _channelConsumer;
    private readonly CancellationTokenSource _cts = new();
    private bool _isDisposed;
    private int _maxCount = int.MaxValue;
    private const int TrimBuffer = 200;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableConcurrentUiSafeCollection()
    {
        _synchronizationContext = SynchronizationContext.Current!;
        _channel = new();
        _channelConsumer = Task.Run(ChannelConsumerLoop, _cts.Token);
    }

    /// <summary>Seed constructor used by serializers that prefer constructor population.</summary>
    public ObservableConcurrentUiSafeCollection(IEnumerable<T> collection) : this()
    {
        lock (_itemsLock)
        {
            foreach (T item in collection)
                _items.Add(item);
        }
    }

    public ObservableConcurrentUiSafeCollection(int maxCount) : this()
    {
        if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount));
        _maxCount = maxCount;
    }

    public int MaxCount
    {
        get => _maxCount;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _maxCount = value;
            bool trimmed = false;
            lock (_itemsLock)
            {
                if (_items.Count > _maxCount)
                {
                    _items.RemoveRange(0, _items.Count - _maxCount);
                    trimmed = true;
                }
            }
            if (trimmed)
                InvokeCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
    }

    public void Add(T item) => _channel.Writer.TryWrite(new(item, ObservableConcurrentUiSafeCollectionOperationType.Add));

    public void Remove(T item) => _channel.Writer.TryWrite(new(item, ObservableConcurrentUiSafeCollectionOperationType.Remove));

    public void Clear() => _channel.Writer.TryWrite(new ObservableConcurrentUiSafeCollectionOperation<T>(ObservableConcurrentUiSafeCollectionOperationType.Clear));
    
    public T this[int index]
    {
        get
        {
            lock (_itemsLock)
            {
                return _items[index];
            }
        }
    }

    public int FindIndex(Predicate<T> match) { lock (_itemsLock) { return _items.FindIndex(match); } }

    public int Count { get { lock (_itemsLock) { return _items.Count; } } }

    public bool IsReadOnly => false;

    bool ICollection<T>.Remove(T item)
    {
        Remove(item);
        return true; // removal is queued; actual result is async
    }

    public bool Contains(T item) { lock (_itemsLock) { return _items.Contains(item); } }

    public bool Any(Func<T, bool> predicate) { lock (_itemsLock) { return _items.Any(predicate); } }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            _channel.Writer.TryWrite(new(item, ObservableConcurrentUiSafeCollectionOperationType.Add));
    }

    public void RemoveAll(Func<T, bool> predicate)
    {
        _channel.Writer.TryWrite(new(predicate, ObservableConcurrentUiSafeCollectionOperationType.RemoveAll));
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_itemsLock)
        {
            int count = Math.Min(_items.Count, array.Length - arrayIndex);
            for (int i = 0; i < count; i++)
                array[arrayIndex + i] = _items[i];
        }
    }

    /// <summary>Snapshot enumeration â€” safe to call from any thread.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        lock (_itemsLock) { return _items.ToList().GetEnumerator(); }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Non-generic IList / ICollection — required by Avalonia ItemsControl binding

    int IList.Add(object? value)
    {
        Add((T)value!);
        return 0;
    }

    void IList.Remove(object? value) => Remove((T)value!);

    int IList.IndexOf(object? value)
    {
        lock (_itemsLock) { return _items.IndexOf((T)value!); }
    }

    bool IList.Contains(object? value) => value is T t && Contains(t);

    object? IList.this[int index]
    {
        get { lock (_itemsLock) { return _items[index]; } }
        set => throw new NotSupportedException();
    }

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void ICollection.CopyTo(Array array, int index)
    {
        lock (_itemsLock) { ((ICollection)_items.ToArray()).CopyTo(array, index); }
    }

    private void InvokeCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        if (CollectionChanged == null && PropertyChanged == null) return;

        void Raise()
        {
            CollectionChanged?.Invoke(this, args);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        SynchronizationContext? ctx = ObservableConcurrentUiSafeCollectionStatic.SharedContext ?? _synchronizationContext;
        if (ctx == null || ReferenceEquals(ctx, SynchronizationContext.Current))
            Raise();
        else
            ctx.Post(_ => Raise(), null);
    }

    private void TrimExcess()
    {
        if (_maxCount == int.MaxValue) return;

        bool trimmed = false;
        lock (_itemsLock)
        {
            if (_items.Count < _maxCount + TrimBuffer) return;
            _items.RemoveRange(0, _items.Count - _maxCount);
            trimmed = true;
        }
        if (trimmed)
            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    private async Task ChannelConsumerLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var reader = _channel.Reader;

                while (await reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    try
                    {
                        while (reader.TryRead(out ObservableConcurrentUiSafeCollectionOperation<T> op))
                        {
                            switch (op.OperationType)
                            {
                                case ObservableConcurrentUiSafeCollectionOperationType.Add:
                                    {
                                        int idx;
                                        lock (_itemsLock) { _items.Add(op.Item); idx = _items.Count - 1; }
                                        InvokeCollectionChanged(new(NotifyCollectionChangedAction.Add, op.Item, idx));
                                        TrimExcess();
                                    }
                                    break;

                                case ObservableConcurrentUiSafeCollectionOperationType.Remove:
                                    {
                                        int idx;
                                        lock (_itemsLock) { idx = _items.IndexOf(op.Item); }
                                        if (idx >= 0)
                                        {
                                            lock (_itemsLock) { _items.RemoveAt(idx); }
                                            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Remove, op.Item, idx));
                                        }
                                    }
                                    break;

                                case ObservableConcurrentUiSafeCollectionOperationType.Clear:
                                    lock (_itemsLock) { _items.Clear(); }
                                    InvokeCollectionChanged(new(NotifyCollectionChangedAction.Reset));
                                    break;

                                case ObservableConcurrentUiSafeCollectionOperationType.RemoveAll:
                                    {
                                        List<T> removed;
                                        lock (_itemsLock)
                                        {
                                            removed = _items.Where(op.Predicate!).ToList();
                                            foreach (var item in removed)
                                                _items.Remove(item);
                                        }
                                        if (removed.Count > 0)
                                            InvokeCollectionChanged(new(NotifyCollectionChangedAction.Reset));
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await Console.Error.WriteLineAsync($"[ObservableConcurrentUiSafeCollection] Consumer error: {ex}");
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[ObservableConcurrentUiSafeCollection] Consumer error, restarting: {ex}");
                try { await Task.Delay(500, _cts.Token); } catch (OperationCanceledException) { break; }
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _cts.Cancel();
        try { _channelConsumer.Wait(TimeSpan.FromSeconds(5)); } catch { }
        _channelConsumer.Dispose();
        _cts.Dispose();
    }
}

public readonly struct ObservableConcurrentUiSafeCollectionOperation<T>
{
    public T Item { get; }
    public Func<T, bool>? Predicate { get; }
    public ObservableConcurrentUiSafeCollectionOperationType OperationType { get; }

    public ObservableConcurrentUiSafeCollectionOperation(T item, ObservableConcurrentUiSafeCollectionOperationType operationType)
    {
        Item = item;
        Predicate = null;
        OperationType = operationType;
    }

    public ObservableConcurrentUiSafeCollectionOperation(Func<T, bool> predicate, ObservableConcurrentUiSafeCollectionOperationType operationType)
    {
        Item = default!;
        Predicate = predicate;
        OperationType = operationType;
    }

    public ObservableConcurrentUiSafeCollectionOperation(ObservableConcurrentUiSafeCollectionOperationType operationType)
    {
        Item = default!;
        Predicate = null;
        OperationType = operationType;
    }
}

public enum ObservableConcurrentUiSafeCollectionOperationType { Add, Remove, Clear, RemoveAll }