using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace SenX_KOTH_Plugin.Utils;

internal sealed class JsonPersistent<T> : IDisposable where T : class, INotifyPropertyChanged, new()
{
    private readonly object _saveLock = new();
    private Timer? _saveTimer;
    private int _pendingSave;

    private string Path { get; }
    public T Data { get; }

    public JsonPersistent(string path, T data)
    {
        Path = path;
        Data = data;
        Data.PropertyChanged += OnPropertyChanged;
    }

    public void WatchCollection(INotifyCollectionChanged collection)
    {
        collection.CollectionChanged += (_, _) => OnPropertyChanged(null, null!);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _saveTimer ??= new Timer(_ => FlushSave());
        _pendingSave = 1;
        _saveTimer.Change(1000, -1);
    }

    public void Save()
    {
        _saveTimer?.Change(-1, -1);
        _pendingSave = 1;
        FlushSave();
    }

    private void FlushSave()
    {
        if (Interlocked.Exchange(ref _pendingSave, 0) == 0) return;
        lock (_saveLock)
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Path, JsonConvert.SerializeObject(Data, Formatting.Indented));
        }
    }

    public static JsonPersistent<T> Load(string path)
    {
        T data;
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<T>(json) ?? new T();
        }
        else
        {
            data = new T();
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        return new JsonPersistent<T>(path, data);
    }

    public void Dispose()
    {
        Data.PropertyChanged -= OnPropertyChanged;
        _saveTimer?.Dispose();
        lock (_saveLock)
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Path, JsonConvert.SerializeObject(Data, Formatting.Indented));
        }
    }
}