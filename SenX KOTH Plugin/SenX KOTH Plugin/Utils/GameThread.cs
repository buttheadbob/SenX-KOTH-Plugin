using System;
using System.Threading.Tasks;
using Sandbox;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class GameThread
    {
        internal static void Invoke(Action action, string name = "KoTH")
        {
            if (MySandboxGame.Static == null) return;
            MySandboxGame.Static.Invoke(action, name);
        }

        internal static Task<T> InvokeAsync<T>(Func<T> func, string name = "KoTH")
        {
            if (MySandboxGame.Static == null)
                return Task.FromResult(default(T)!);
            var tcs = new TaskCompletionSource<T>();
            MySandboxGame.Static.Invoke(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            }, name);
            return tcs.Task;
        }
    }
}
