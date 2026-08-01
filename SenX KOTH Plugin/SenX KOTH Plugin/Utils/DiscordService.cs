using NLog;
using System;
using System.Drawing;
using System.Linq;
using SenX_KOTH_Plugin.DiscordAPI;
using Extensions = SenX_KOTH_Plugin.DiscordAPI.Extensions;

namespace SenX_KOTH_Plugin.Utils;

internal static class DiscordService
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => DiscordService");

    public static void SendDiscordWebHook(WebhookEventType eventType, string msg,
        Color? embedColor = null, int alertType = 0, string? zoneName = null)
    {
        var inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null) return;

        var webhooks = inst.Config.Webhooks;
        if (webhooks == null) return;

        string tempTitle = DetermineTitle(msg, inst.Config);

        foreach (var entry in webhooks)
        {
            if (!entry.Enabled || string.IsNullOrEmpty(entry.Url))
                continue;

            if (!entry.AcceptsEvent(eventType))
                continue;

            if (IsZoneScoped(eventType))
            {
                if (!entry.AllZones)
                {
                    if (string.IsNullOrWhiteSpace(entry.ZoneFilter))
                        continue;

                    if (!string.IsNullOrEmpty(zoneName))
                    {
                        var zones = entry.ZoneFilter.Split(',')
                            .Select(z => z.Trim())
                            .Where(z => z.Length > 0)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        if (!zones.Contains(zoneName!))
                            continue;
                    }
                }
            }

            SendToWebhook(entry.Url, msg, tempTitle, embedColor, alertType);
        }
    }

    public static void SendAlertWebHook(string msg)
    {
        SendDiscordWebHook(WebhookEventType.EnterAlert, msg, Color.Orange, 0);
    }

    private static bool IsZoneScoped(WebhookEventType type) => type switch
    {
        WebhookEventType.Capture => true,
        WebhookEventType.Decay => true,
        WebhookEventType.Points => true,
        WebhookEventType.EnterAlert => true,
        _ => false
    };

    private static string DetermineTitle(string msg, SenX_KOTH_PluginConfig config)
    {
        if (msg.Contains("First Place"))          return "FIRST PLACE";
        if (msg.Contains("Second Place"))         return "SECOND PLACE";
        if (msg.Contains("Third Place"))          return "THIRD PLACE";
        if (msg.Contains("The Other People....")) return "The Other People....";
        return config.CustomTitleEnable ? config.CustomTitle : "Hank Says";
    }

    private static async void SendToWebhook(string webhookUrl, string msg, string tempTitle, Color? embedColor, int alertType)
    {
        var inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null) return;

        DiscordWebHook webHook = new DiscordWebHook();
        DiscordMessage message = new DiscordMessage { Username = "KoTH", AvatarUrl = "" };
        webHook.Uri = new Uri(webhookUrl);

        DiscordEmbed embed = new DiscordEmbed()
        {
            Title = tempTitle,
            Timestamp = DateTime.Now,
            Color = embedColor == null ? Extensions.ToHex(Color.Red) : embedColor.ToHex()
        };

        string embedUrl = inst.Config.EmbedImageUrl;
        if (!string.IsNullOrEmpty(embedUrl))
            embed.Thumbnail = new EmbedMedia() { Url = embedUrl };

        if (!string.IsNullOrEmpty(inst.Config.MessagePrefix))
            msg = inst.Config.MessagePrefix + "\n" + msg;

        try
        {
            if (inst.Config.EmbedEnabled)
            {
                embed.Fields.Add(alertType == 0
                    ? new EmbedField { Name = "Man Your Battle Stations!!!", Value = msg }
                    : new EmbedField { Name = "***Rank Update***", Value = msg });

                message.Embeds.Add(embed);
            }
            else
                message.Content = "***" + tempTitle + "*** " + msg;

            await webHook.SendAsync(message);
        }
        catch (Exception e)
        {
            KoTHLog.Error(Log, e, "Discord could be down or there is something wrong with your webhook.");
        }
    }
}
