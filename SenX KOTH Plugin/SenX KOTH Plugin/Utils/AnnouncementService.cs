using System;
using System.Drawing;
using SenX_KOTH_Plugin.Discord;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Nexus;

namespace SenX_KOTH_Plugin.Utils;

internal static class AnnouncementService
{
    public static void ZoneCapture(KothZone zone, string factionTag, string factionName, int pointsEarned)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null || !zone.DiscordAnnounceCapture) return;

        if (config.WebHookEnabled)
            DiscordService.SendDiscordWebHook(
                "[" + factionTag + "] " + factionName + " captured " + zone.Name + "!",
                Color.Gold, 1);

        if (config.NexusSendDiscord && !ShouldSkipRelay())
            NexusManager.BroadcastDiscordZoneRelay(zone.Name, new ProtoEmbed
            {
                Title = zone.Name + " \u2014 Held by " + factionTag,
                Description = "`" + factionTag + " " + factionName + "`" +
                              " \u2014 Points Earned: `" + pointsEarned + "`",
                Color = 0xFFD700u
            }, zone.DiscordChannelId);
    }

    public static void ZoneDecay(KothZone zone)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null || !zone.DiscordAnnounceDecay) return;

        if (config.WebHookEnabled)
            DiscordService.SendDiscordWebHook(
                zone.Name + " capture lost - returning to Neutral",
                Color.DarkRed, 1);

        if (config.NexusSendDiscord && !ShouldSkipRelay())
            NexusManager.BroadcastDiscordZoneRelay(zone.Name, new ProtoEmbed
            {
                Title = zone.Name + " \u2014 Returned to Neutral",
                Description = "Capture lost",
                Color = 0x8B0000u
            }, zone.DiscordChannelId);
    }

    public static void ZonePointsEarned(KothZone zone, string factionTag, string factionName, int points)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null || !zone.DiscordAnnouncePoints) return;

        if (config.WebHookEnabled)
            DiscordService.SendDiscordWebHook(
                "[" + factionTag + "] " + factionName + " earned " + points + "pts in " + zone.Name + "!",
                Color.Orange, 0);

        if (config.NexusSendDiscord && !ShouldSkipRelay())
            NexusManager.BroadcastDiscordZoneRelay(zone.Name, new ProtoEmbed
            {
                Title = zone.Name + " \u2014 Points Earned",
                Description = "`[" + factionTag + "] " + factionName + "` earned " + points + " pts",
                Color = 0xFFA500u
            }, zone.DiscordChannelId);
    }

    public static void ZoneEnterAlert(KothZone zone, string message)
    {
        if (!zone.DiscordAnnounceEnter) return;

        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null) return;

        if (config.WebHookEnabled)
            DiscordService.SendAlertWebHook(message);

        if (config.NexusSendDiscord && !ShouldSkipRelay())
            NexusManager.BroadcastDiscordZoneRelay(zone.Name, new ProtoEmbed
            {
                Title = zone.Name + " \u2014 Player Entered",
                Description = message,
                Color = 0xFFA500u
            }, zone.DiscordChannelId);
    }

    public static void ZoneEviction(KothZone zone, bool manualEvict, int evictionDurationSeconds)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config?.NexusSendDiscord != true) return;

        var durSecs = manualEvict ? 5 : evictionDurationSeconds;
        var endAt = DateTimeOffset.UtcNow.AddSeconds(durSecs).ToUnixTimeSeconds();

        NexusManager.BroadcastDiscordZoneRelay(zone.Name, new ProtoEmbed
        {
            Title = zone.Name + " \u2014 Eviction Active",
            Description = "Ends <t:" + endAt + ":R>",
            Color = 0xFF0000u
        }, zone.DiscordChannelId);
    }

    public static void RankResult(string message, Color color)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null) return;

        if (config.WebHookEnabled)
            DiscordService.SendDiscordWebHook(message, color, 1);
    }

    public static void PeriodResults(string periodName, string resultsText)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null) return;

        if (config.NexusSendDiscord && !DiscordBotService.IsEnabled)
            NexusManager.BroadcastDiscordRewardRelay(periodName + " Results", "```\n" + resultsText + "```", 0xF1C40Fu);
        else
            _ = DiscordBotService.SendRewardAnnouncementAsync(periodName + " Results", "```\n" + resultsText + "```");
    }

    public static void RaffleWinner(string message, Color color)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
        if (config == null) return;

        if (config.WebHookEnabled)
            DiscordService.SendDiscordWebHook(message, color, 1);
    }

    private static bool ShouldSkipRelay()
    {
        return NexusManager.IsAuthorityLocal() && DiscordBotService.IsEnabled;
    }
}
