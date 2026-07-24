using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SenX_KOTH_Plugin.Bot;
using SenX_KOTH_Plugin.Events;
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
        private static readonly Dictionary<ulong, DateTime> _lastZonePost = new();

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
            KoTHLog.Info(Log,"DiscordBotService started.");

            if (config.SelfManagedChannelsEnabled && config.KoTHCategoryId != 0)
                await EnsureSelfManagedChannelsAsync(config);
        }

        public static Task StopAsync()
        {
            if (_client == null) return Task.CompletedTask;
            _client.Dispose();
            _client = null;
            IsEnabled = false;
            KoTHLog.Info(Log,"DiscordBotService stopped.");
            return Task.CompletedTask;
        }

        public static async Task SendRewardAnnouncementAsync(string title, string description)
        {
            if (_client == null) return;

            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null) return;

            if (_rewardChannelId == 0 || !await _client.ChannelExistsAsync(_rewardChannelId))
            {
                _rewardChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, "rewards", config.KoTHCategoryId);
                config.RewardChannelId = _rewardChannelId;
                SenX_KOTH_PluginMain.ConfigPersist?.Save();
            }

            await _client.SendEmbedAsync(_rewardChannelId, title, description, 0xF1C40Fu);
        }

        public static async Task UpdateLiveScoreboardAsync()
        {
            if (_client == null) return;

            var scoreData = SenX_KOTH_PluginMain.EventPersist?.Data;
            if (scoreData == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("=== Points So Far ===");

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
                                Description = sb.ToString(),
                                Color = 0x3498DBu
                            }
                        }
                    };
                    await _client.EditMessageAsync(_liveChannelId, _lastLiveMessageId, editMsg);
                    return;
                }
            }
            catch
            {
                if (_liveChannelId != 0)
                {
                    try { await _client.DeleteChannelAsync(_liveChannelId); } catch { }
                    _liveChannelId = 0;
                }
                _lastLiveMessageId = 0;
            }

            if (_liveChannelId == 0 || !await _client.ChannelExistsAsync(_liveChannelId))
            {
                _liveChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, "points-this-week", config.KoTHCategoryId);
                config.LiveScoreboardChannelId = _liveChannelId;
            }

            _lastLiveMessageId = await _client.SendEmbedAsync(_liveChannelId, "", sb.ToString(), 0x3498DBu);
            config.LiveScoreboardMessageId = _lastLiveMessageId;
            SenX_KOTH_PluginMain.ConfigPersist?.Save();
        }

        public static async Task UpdateZoneChannelsAsync()
        {
            if (_client == null) return;

            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null) return;

            var zoneEvents = SenX_KOTH_PluginMain.Events
                .OfType<ZonePointEvent>()
                .Where(e => e.IsRunning)
                .ToList();

            foreach (var evt in zoneEvents)
            {
                var zone = evt.Zone;
                if (zone.DiscordChannelId == 0) continue;

                if (_lastZonePost.TryGetValue(zone.DiscordChannelId, out var lastPost))
                {
                    if ((DateTime.UtcNow - lastPost).TotalSeconds < 10)
                        continue;
                }

                var (title, description, color) = BuildZoneEmbed(evt);
                if (title == null) continue;

                if (!await _client.ChannelExistsAsync(zone.DiscordChannelId))
                {
                    var prefix = string.IsNullOrEmpty(config.DiscordChannelPrefix) ? "zone-" : config.DiscordChannelPrefix;
                    zone.DiscordChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, prefix + zone.Name.ToLower(), config.KoTHCategoryId);
                    SenX_KOTH_PluginMain.ZonePersist?.Save();
                }

                await _client.SendEmbedAsync(zone.DiscordChannelId, title, description, color);
                _lastZonePost[zone.DiscordChannelId] = DateTime.UtcNow;
            }
        }

        private static (string? title, string description, uint color) BuildZoneEmbed(ZonePointEvent evt)
        {
            var zoneName = evt.Zone.Name;
            var tag = evt.CaptureFactionName;
            var pct = evt.Zone.CapturePointsNeeded > 0
                ? evt.CaptureProgress * 100 / evt.Zone.CapturePointsNeeded : 0;

            switch (evt.EvictionState)
            {
                case EvictionPhase.Warning30:
                {
                    var evictAt = DateTimeOffset.UtcNow.AddSeconds(evt.EvictionTimeRemaining).ToUnixTimeSeconds();
                    return (zoneName + " \u2014 Eviction Warning",
                        "Evacuate! Eviction starts <t:" + evictAt + ":R>",
                        0xFFA500u);
                }
                case EvictionPhase.Active:
                {
                    var endAt = DateTimeOffset.UtcNow.AddSeconds(evt.EvictionTimeRemaining).ToUnixTimeSeconds();
                    return (zoneName + " \u2014 Eviction Active",
                        "Ends <t:" + endAt + ":R>",
                        0xFF0000u);
                }
            }

            switch (evt.State)
            {
                case CaptureState.Capturing:
                {
                    var desc = "`" + tag + "`: " + pct + "%";
                    var timeRem = ComputeDiscordTimeRemaining(evt, true);
                    if (timeRem != null) desc += " \u2014 Time Remaining: `" + timeRem + "`";
                    return (zoneName + " \u2014 Capturing", desc, 0x00FF00u);
                }

                case CaptureState.Contested:
                {
                    var desc = "`" + tag + "`: " + pct + "%";
                    desc += " \u2014 Enemies Inside: `" + evt.TotalEnemiesInside + "`";
                    return (zoneName + " \u2014 Contested", desc, 0xFF0000u);
                }

                case CaptureState.Decaying:
                {
                    var desc = "`" + tag + "`: " + pct + "%";
                    var timeRem = ComputeDiscordTimeRemaining(evt, false);
                    if (timeRem != null) desc += " \u2014 Time Remaining: `" + timeRem + "`";
                    return (zoneName + " \u2014 Decaying", desc, 0xFFA500u);
                }

                case CaptureState.Captured:
                {
                    var desc = "`" + tag + " " + evt.CaptureFactionName + "`";
                    desc += " \u2014 Points Earned: `" + evt.CapturePointsEarned + "`";
                    return (zoneName + " \u2014 Held by " + tag, desc, 0xFFD700u);
                }
            }

            return (null, "", 0);
        }

        private static string? ComputeDiscordTimeRemaining(ZonePointEvent evt, bool capturing)
        {
            if (evt.AutoDecayActive)
            {
                int remaining = evt.AutoDecayTimeRemaining;
                if (remaining <= 0) return null;
                return remaining < 60 ? remaining + "s" : (remaining / 60) + "m";
            }

            if (capturing)
            {
                int gainPerTick = ((int)evt.SuitCount * evt.Zone.PointsPerSuit)
                                + ((int)evt.GridCount * evt.Zone.PointsPerGrid);
                if (gainPerTick <= 0) return null;
                int remaining = evt.Zone.CapturePointsNeeded - evt.CaptureProgress;
                if (remaining <= 0) return "0s";
                int ticksNeeded = remaining / gainPerTick + (remaining % gainPerTick > 0 ? 1 : 0);
                int seconds = ticksNeeded * evt.Zone.CapturePointIntervalSeconds;
                return seconds < 60 ? seconds + "s" : (seconds / 60) + "m";
            }
            else
            {
                int lossPerTick = ((int)evt.EnemySuitCount * evt.Zone.PointsPerSuit)
                                + ((int)evt.EnemyGridCount * evt.Zone.PointsPerGrid);
                if (lossPerTick <= 0) return null;
                int remaining = evt.CaptureProgress;
                if (remaining <= 0) return "0s";
                int ticksNeeded = remaining / lossPerTick + (remaining % lossPerTick > 0 ? 1 : 0);
                int seconds = ticksNeeded * evt.Zone.CapturePointIntervalSeconds;
                return seconds < 60 ? seconds + "s" : (seconds / 60) + "m";
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
                else if (_rewardChannelId != 0)
                {
                    await _client.ModifyChannelPermissionsAsync(_rewardChannelId, config.DiscordGuildId);
                }

                if (_liveChannelId == 0 || !await _client.ChannelExistsAsync(_liveChannelId))
                {
                    _liveChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, "points-this-week", config.KoTHCategoryId);
                    config.LiveScoreboardChannelId = _liveChannelId;
                }
                else if (_liveChannelId != 0)
                {
                    var ch = await _client.GetChannelAsync(_liveChannelId);
                    if (ch != null && !string.Equals(ch.Name, "points-this-week", StringComparison.OrdinalIgnoreCase))
                        await _client.ModifyChannelAsync(_liveChannelId, "points-this-week");
                    await _client.ModifyChannelPermissionsAsync(_liveChannelId, config.DiscordGuildId);
                }

                var zonePersist = SenX_KOTH_PluginMain.ZonePersist;
                if (zonePersist == null) return;

                var prefix = string.IsNullOrEmpty(config.DiscordChannelPrefix) ? "zone-" : config.DiscordChannelPrefix;

                foreach (var zone in zonePersist.Data.Zones)
                {
                    var expectedName = prefix + zone.Name.ToLower();
                    if (zone.DiscordChannelId == 0 || !await _client.ChannelExistsAsync(zone.DiscordChannelId))
                    {
                        zone.DiscordChannelId = await _client.CreateTextChannelAsync(config.DiscordGuildId, expectedName, config.KoTHCategoryId);
                    }
                    else
                    {
                        var ch = await _client.GetChannelAsync(zone.DiscordChannelId);
                        if (ch != null && !string.Equals(ch.Name, expectedName, StringComparison.OrdinalIgnoreCase))
                            await _client.ModifyChannelAsync(zone.DiscordChannelId, expectedName);
                        await _client.ModifyChannelPermissionsAsync(zone.DiscordChannelId, config.DiscordGuildId);
                    }
                }

                SenX_KOTH_PluginMain.ZonePersist?.Save();
                SenX_KOTH_PluginMain.ConfigPersist?.Save();
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log,ex, "Failed to set up self-managed channels.");
            }
        }
    }
}
