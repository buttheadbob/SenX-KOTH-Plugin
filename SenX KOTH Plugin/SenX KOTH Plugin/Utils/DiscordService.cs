using NLog;
using System;
using System.Drawing;
using SenX_KOTH_Plugin.DiscordAPI;
using Extensions = SenX_KOTH_Plugin.DiscordAPI.Extensions;

namespace SenX_KOTH_Plugin.Utils;

internal static class DiscordService
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => DiscordService");

    public static void SendDiscordWebHook(string msg, Color? embedColor = null, int alertType = 0)
    {
        var inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null || !inst.Config.WebHookEnabled) return;
        if (string.IsNullOrEmpty(inst.Config.WebHookUrl)) return;

        string tempTitle = DetermineTitle(msg, inst.Config);
        SendToWebhook(inst.Config.WebHookUrl, msg, tempTitle, embedColor, alertType);
    }

    public static void SendAlertWebHook(string msg)
    {
        var inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null || !inst.Config.WebHookEnabled) return;
        if (string.IsNullOrEmpty(inst.Config.WebHookUrl)) return;

        SendToWebhook(inst.Config.WebHookUrl, msg, "KoTH Zone Entry", Color.Orange, 0);
    }

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
