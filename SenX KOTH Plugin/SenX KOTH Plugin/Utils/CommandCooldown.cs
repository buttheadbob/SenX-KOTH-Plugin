using System;
using System.Collections.Generic;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class CommandCooldown
    {
        private static readonly Dictionary<ulong, DateTime> LastUse = new();
        private const int CooldownSeconds = 5;

        public static bool IsOnCooldown(ulong steamId)
        {
            lock (LastUse)
            {
                if (!LastUse.TryGetValue(steamId, out var last)) return false;
                return (DateTime.UtcNow - last).TotalSeconds < CooldownSeconds;
            }
        }

        public static void MarkUsed(ulong steamId)
        {
            lock (LastUse) LastUse[steamId] = DateTime.UtcNow;
        }

        public static bool TryUse(ulong steamId)
        {
            if (IsOnCooldown(steamId)) return false;
            MarkUsed(steamId);
            return true;
        }
    }
}
