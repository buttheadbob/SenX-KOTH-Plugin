using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SenX_KOTH_Plugin.Bot;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Discord
{
    internal static class DiscordBotService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => DiscordBot");

        private static DiscordBotClient? _client;
        private static ulong _rewardChannelId;
        private static ulong _liveChannelId;
        private static ulong _lastLiveMessageId;

        public static bool IsEnabled { get; private set; }

        public static async Task StartAsync()
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null || !config.DiscordBotEnabled || string.IsNullOrEmpty(config.DiscordBotToken))
                return;

            _rewardChannelId = config.RewardChannelId;
            _liveChannelId = config.LiveScoreboardChannelId;
            _lastLiveMessageId = config.LiveScoreboardMessageId;

            _client = SenxDiscordBot.CreateBuilder()
                .WithToken(config.DiscordBotToken)
                .Build();

            await _client.StartAsync();
            IsEnabled = true;
            Log.Info("DiscordBotService started.");

            if (config.SelfManagedChannelsEnabled && config.KoTHCategoryId != 0)
                await EnsureSelfManagedChannelsAsync(config);
        }

        public static Task StopAsync()
        {
            if (_client == null) return Task.CompletedTask;
            _client.Dispose();
            _client = null;
            IsEnabled = false;
            Log.Info("DiscordBotService stopped.");
            return Task.CompletedTask;
        }

        public static async Task SendRewardAnnouncementAsync(string title, string description)
        {
            if (_client == null || _rewardChannelId == 0) return;
            await _client.SendEmbedAsync(_rewardChannelId, title, description, 0xF1C40F);
        }

        public static async Task UpdateLiveScoreboardAsync()
        {
            if (_client == null || _liveChannelId == 0) return;

            var scoreData = SenX_KOTH_PluginMain.EventPersist?.Data;
            if (scoreData == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("=== KOTH Live ===");

            foreach (var zone in ZoneManager.ZoneCache)
            {
                var zoneEvents = scoreData.WeekEvents
                    .Where(e => string.Equals(e.ZoneName, zone.ZoneName, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(e => e.FactionName)
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Points));

                if (zoneEvents.Count > 0)
                {
                    var parts = string.Join(" ", zoneEvents.OrderByDescending(x => x.Value)
                        .Select(kvp => "[" + kvp.Key + "] (" + kvp.Value + "p)"));
                    sb.AppendLine(zone.ZoneName + ": " + parts);
                }
                else
                    sb.AppendLine(zone.ZoneName + ": No activity");
            }

            sb.AppendLine("Updated: " + DateTime.UtcNow.ToString("HH:mm:ss") + " UTC");
            sb.AppendLine("```");

            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null) return;

            try
            {
                if (_lastLiveMessageId != 0)
                {
                    var editMsg = new DiscordMessage
                    {
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "KOTH Live",
                                Description = sb.ToString(),
                                Color = 0x3498DB,
                                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            }
                        }
                    };
                    await _client.EditMessageAsync(_liveChannelId, _lastLiveMessageId, editMsg);
                    config.LiveScoreboardMessageId = _lastLiveMessageId;
                    SenX_KOTH_PluginMain.ConfigPersist?.Save();
                    return;
                }

                _lastLiveMessageId = await _client.SendEmbedAsync(_liveChannelId, "KOTH Live", sb.ToString(), 0x3498DB);
                config.LiveScoreboardMessageId = _lastLiveMessageId;
                SenX_KOTH_PluginMain.ConfigPersist?.Save();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Live scoreboard update failed");
            }
        }

        private static async Task EnsureSelfManagedChannelsAsync(SenX_KOTH_PluginConfig config)
        {
            try
            {
                if (_client == null || config.KoTHCategoryId == 0 || config.DiscordGuildId == 0) return;

                if (_rewardChannelId == 0 || !await _client.ChannelExistsAsync(_rewardChannelId))
                {
                    _rewardChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, "rewards", config.KoTHCategoryId);
                    config.RewardChannelId = _rewardChannelId;
                }

                if (_liveChannelId == 0 || !await _client.ChannelExistsAsync(_liveChannelId))
                {
                    _liveChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, "live-scoreboard", config.KoTHCategoryId);
                    config.LiveScoreboardChannelId = _liveChannelId;
                }

                var zonePersist = SenX_KOTH_PluginMain.ZonePersist;
                if (zonePersist == null) return;

                foreach (var zone in zonePersist.Data.Zones)
                {
                    if (zone.DiscordChannelId == 0 || !await _client.ChannelExistsAsync(zone.DiscordChannelId))
                    {
                        zone.DiscordChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, "zone-" + zone.Name.ToLower(), config.KoTHCategoryId);
                    }
                }

                SenX_KOTH_PluginMain.ZonePersist?.Save();
                SenX_KOTH_PluginMain.ConfigPersist?.Save();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to set up self-managed channels.");
            }
        }
    }
}
