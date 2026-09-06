using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace SenX_KOTH_Plugin.Utils;

internal static class SharedFile
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => SharedFile");
    private static readonly SemaphoreSlim WriteGate = new(1, 1);
    private const int MaxRetries = 30;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    public static async Task<(bool ok, T? data)> ReadAsync<T>(string path, int maxRetries = MaxRetries)
    {
        var fs = await AcquireAsync(path, maxRetries).ConfigureAwait(false);
        if (fs == null)
            return (false, default);

        try
        {
            if (fs.Length == 0)
                return (true, default);
            return (true, await ReadJsonAsync<T>(fs).ConfigureAwait(false));
        }
        finally
        {
            fs.Dispose();
        }
    }

    public static async Task<(bool ok, T? result)> ReadModifyWriteAsync<T>(string path, Func<T?, T> mutate, int maxRetries = MaxRetries)
    {
        // Fail-fast callers don't queue behind an in-process write.
        if (maxRetries <= 0)
        {
            if (!await WriteGate.WaitAsync(0).ConfigureAwait(false))
                return (false, default);
        }
        else
        {
            await WriteGate.WaitAsync().ConfigureAwait(false);
        }

        try
        {
            var fs = await AcquireAsync(path, maxRetries).ConfigureAwait(false);
            if (fs == null)
                return (false, default);

            try
            {
                T? current = fs.Length > 0 ? await ReadJsonAsync<T>(fs).ConfigureAwait(false) : default;
                var result = mutate(current);

                var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                await WriteTextAsync(tmp, JsonConvert.SerializeObject(result, Formatting.Indented)).ConfigureAwait(false);

                fs.Dispose();
                fs = null;

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

                return (true, result);
            }
            finally
            {
                fs?.Dispose();
            }
        }
        catch (Exception ex)
        {
            KoTHLog.Error(Log, ex, "Failed to write {0}.", path);
            return (false, default);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    private static async Task<FileStream?> AcquireAsync(string path, int maxRetries)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex)
            {
                if (attempt >= maxRetries)
                {
                    KoTHLog.Error(Log, ex, "Failed to acquire lock on {0} after {1} attempts.", path, attempt + 1);
                    return null;
                }
                if (attempt == 0)
                    KoTHLog.Warn(Log, "File " + path + " is locked by another instance; waiting...");
                await Task.Delay(RetryDelay).ConfigureAwait(false);
            }
        }
    }

    private static async Task<T?> ReadJsonAsync<T>(FileStream fs)
    {
        fs.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(fs, Encoding.UTF8, true, 4096, leaveOpen: true);
        return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync().ConfigureAwait(false));
    }

    private static async Task WriteTextAsync(string path, string text)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(fs, new UTF8Encoding(false));
        await writer.WriteAsync(text).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }
}
