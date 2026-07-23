using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace SenX_KOTH_Plugin.Utils;

public class SenxChannel<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public OrionWriter Writer { get; }
    public OrionReader Reader { get; }
    public int Count => _queue.Count;

    public SenxChannel(bool singleReader = false, bool singleWriter = false)
    {
        Writer = new OrionWriter(this);
        Reader = new OrionReader(this);
    }

    public sealed class OrionWriter
    {
        private readonly SenxChannel<T> _c;
        internal OrionWriter(SenxChannel<T> c) => _c = c;

        public bool TryWrite(T item)
        {
            _c._queue.Enqueue(item);
            try { _c._signal.Release(); }
            catch (SemaphoreFullException) { /* reader has pending signal; item will be drained */ }
            return true;
        }
    }

    public sealed class OrionReader
    {
        private readonly SenxChannel<T> _c;
        internal OrionReader(SenxChannel<T> c) => _c = c;

        public async Task<bool> WaitToReadAsync(CancellationToken token = default)
        {
            await _c._signal.WaitAsync(token);
            return true;
        }

        public bool TryRead([MaybeNullWhen(false)] out T item) => _c._queue.TryDequeue(out item);
    }
}