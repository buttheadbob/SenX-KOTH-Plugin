using System;
using System.Drawing;
using SenX_KOTH_Plugin.Models;

namespace SenX_KOTH_Plugin.Utils;

internal static class AnnouncementService
{
    public static void ZoneCapture(KothZone zone, string factionTag, string factionName, int pointsEarned)
    {
        if (!zone.DiscordAnnounceCapture) return;

        DiscordService.SendDiscordWebHook(WebhookEventType.Capture,
            "[" + factionTag + "] " + factionName + " captured " + zone.Name + "!",
            Color.Gold, 1, zone.Name);
    }

    public static void ZoneDecay(KothZone zone)
    {
        if (!zone.DiscordAnnounceDecay) return;

        DiscordService.SendDiscordWebHook(WebhookEventType.Decay,
            zone.Name + " capture lost - returning to Neutral",
            Color.DarkRed, 1, zone.Name);
    }

    public static void ZonePointsEarned(KothZone zone, string factionTag, string factionName, int points)
    {
        if (!zone.DiscordAnnouncePoints) return;

        DiscordService.SendDiscordWebHook(WebhookEventType.Points,
            "[" + factionTag + "] " + factionName + " earned " + points + "pts in " + zone.Name + "!",
            Color.Orange, 0, zone.Name);
    }

    public static void ZoneEnterAlert(KothZone zone, string message)
    {
        if (!zone.DiscordAnnounceEnter) return;

        DiscordService.SendDiscordWebHook(WebhookEventType.EnterAlert, message,
            Color.Orange, 0, zone.Name);
    }

    public static void ZoneEviction(KothZone zone, bool manualEvict, int evictionDurationSeconds)
    {
        if (!zone.DiscordAnnounceEviction) return;

        var durSecs = manualEvict ? 5 : evictionDurationSeconds;
        var endAt = DateTimeOffset.UtcNow.AddSeconds(durSecs).ToUnixTimeSeconds();

        DiscordService.SendDiscordWebHook(WebhookEventType.Eviction,
            zone.Name + " \u2014 Eviction Active, ends <t:" + endAt + ":R>",
            Color.Red, 0, zone.Name);
    }

    public static void RankResult(string message, Color color)
    {
        DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, message, color, 1);
    }

    public static void PeriodResults(string periodName, string resultsText)
    {
        DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, periodName + " Results:\n" + resultsText, Color.Gold, 1);
    }

    public static void RaffleWinner(string message, Color color)
    {
        DiscordService.SendDiscordWebHook(WebhookEventType.RaffleWinner, message, color, 1);
    }
}
