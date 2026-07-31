using System;
using System.Collections.Generic;
using NLog;
using VRage.Game.ModAPI;
using VRageMath;

namespace SenX_KOTH_Plugin.Utils
{
    internal sealed class EnterAlertService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => EnterAlertService");

        private readonly Dictionary<ulong, Dictionary<string, DateTime>> _playerZoneAlerts
            = new Dictionary<ulong, Dictionary<string, DateTime>>();

        public bool ShouldAlert(IMyPlayer player, string zoneName)
        {
            SenX_KOTH_PluginConfig? config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null)
                return false;

            if (!config.WebHookEnabled && !config.NexusSendDiscord)
                return false;

            ulong steamId = player.SteamUserId;
            if (steamId == 0)
                return false;

            DateTime now = DateTime.UtcNow;
            int minMinutes = 5;

            lock (_playerZoneAlerts)
            {
                if (!_playerZoneAlerts.ContainsKey(steamId))
                    _playerZoneAlerts[steamId] = new Dictionary<string, DateTime>();

                if (_playerZoneAlerts[steamId].ContainsKey(zoneName))
                {
                    double minutesSince = (now - _playerZoneAlerts[steamId][zoneName]).TotalMinutes;
                    if (minutesSince < minMinutes)
                        return false;
                }

                _playerZoneAlerts[steamId][zoneName] = now;
                return true;
            }
        }

        public string BuildMessage(IMyPlayer player, IMyFaction? faction, string zoneName, Vector3D position, float radius)
        {
            SenX_KOTH_PluginConfig? config = SenX_KOTH_PluginMain.Instance?.Config;
            string template = config?.EnterAlert_MessageTemplate ?? "{player} from [{factionTag}] entered {zoneName}";

            string message = template;
            message = message.Replace("{player}", player.DisplayName);
            message = message.Replace("{steamId}", player.SteamUserId.ToString());
            message = message.Replace("{factionTag}", faction?.Tag ?? "None");
            message = message.Replace("{factionName}", faction?.Name ?? "None");
            message = message.Replace("{zoneName}", zoneName);
            message = message.Replace("{x}", position.X.ToString("F0"));
            message = message.Replace("{y}", position.Y.ToString("F0"));
            message = message.Replace("{z}", position.Z.ToString("F0"));
            message = message.Replace("{radius}", radius.ToString("F0"));
            message = message.Replace("{time}", DateTime.UtcNow.ToString("HH:mm:ss UTC"));

            return message;
        }
    }
}
