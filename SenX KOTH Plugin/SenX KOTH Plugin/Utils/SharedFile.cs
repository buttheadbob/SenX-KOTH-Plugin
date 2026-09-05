using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace SenX_KOTH_Plugin.Utils;

internal static class SharedFile
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => SharedFile");
    private static readonly object InProcessLock = new();
    private const int MaxRetries = 30;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    public static T? Read<T>(string path)
    {
        lock (InProcessLock)
        {
            if (!File.Exists(path)) return default;
            using var fs = Acquire(path);
            if (fs == null || fs.Length == 0) return default;
            return ReadJson<T>(fs);
        }
    }

    public static bool ReadModifyWrite<T>(string path, Func<T?, T> mutate, out T? result)
    {
        lock (InProcessLock)
        {
            var fs = Acquire(path);
            if (fs == null)
            {
                result = default;
                return false;
            }

            try
            {
                T? current = fs.Length > 0 ? ReadJson<T>(fs) : default;
                result = mutate(current);

                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(result, Formatting.Indented));

                fs.Dispose();
                if (File.Exists(path))
                {
                    try { File.Replace(tmp, path, null); }
                    catch (Exception)
                    {
                        File.Delete(path);
                        File.Move(tmp, path);
                    }
                }
                else
                {
                    File.Move(tmp, path);
                }

                return true;
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Failed to write {0}.", path);
                result = default;
                return false;
            }
            finally
            {
                fs?.Dispose();
            }
        }
    }

    private static FileStream? Acquire(string path)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex)
            {
                if (attempt >= MaxRetries)
                {
                    KoTHLog.Error(Log, ex, "Failed to acquire lock on {0} after {1} attempts.", path, MaxRetries + 1);
                    return null;
                }
                if (attempt == 0)
                    KoTHLog.Warn(Log, "File " + path + " is locked by another instance; waiting...");
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static T? ReadJson<T>(FileStream fs)
    {
        fs.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(fs, Encoding.UTF8, true, 4096, leaveOpen: true);
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }
}
