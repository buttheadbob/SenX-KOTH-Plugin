using NLog;
using System;
using System.Drawing;
using SenX_KOTH_Plugin.DiscordAPI;
using Extensions = SenX_KOTH_Plugin.DiscordAPI.Extensions;

namespace SenX_KOTH_Plugin.Utils;

    internal sealed class DiscordService
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => DiscordService");

    public static void SendDiscordWebHook(string msg, Color? embedColor = null, int alertType = 0)
    {
        string tempTitle = "";

        if (msg.Contains("First Place"))
        {
            tempTitle = "FIRST PLACE";
            msg = msg.Replace("First Place", "");
        }
        else if (msg.Contains("Second Place"))
        {
            tempTitle = "SECOND PLACE";
            msg = msg.Replace("Second Place", "");
        }
        else if (msg.Contains("Third Place"))
        {
            tempTitle = "THIRD PLACE";
            msg = msg.Replace("Third Place", "");
        }
        else if (msg.Contains("The Other People...."))
        {
            tempTitle = "The Other People....";
            msg = msg.Replace("The Other People....", "");
        }
        else
        {
            SenX_KOTH_PluginMain? instance = SenX_KOTH_PluginMain.Instance;
            if (instance?.Config != null)
            {
                tempTitle = instance.Config.CustomTitleEnable == false
                    ? "Hank Says"
                    : instance.Config.CustomTitle;
            }
        }

        SenX_KOTH_PluginMain? inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null)
            return;

        if (!inst.Config.WebHookEnabled)
            return;

        if (alertType == 0 && !inst.Config.Show_AttackMessages)
            return;

        if (string.IsNullOrEmpty(inst.Config.WebHookUrl))
        {
            KoTHLog.Error(Log,"discord Webhook is enabled but the Webhook url is empty.");
            return;
        }

        SendToWebhook(inst.Config.WebHookUrl, msg, tempTitle, embedColor, alertType);
    }

    public static void SendAlertWebHook(string msg)
    {
        SenX_KOTH_PluginMain? inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null)
            return;

        if (!inst.Config.WebHookEnabled)
            return;

        if (string.IsNullOrEmpty(inst.Config.WebHookUrl))
            return;

        SendToWebhook(inst.Config.WebHookUrl, msg, "KoTH Zone Entry", Color.Orange, 0);
    }

    private static async void SendToWebhook(string webhookUrl, string msg, string tempTitle, Color? embedColor, int alertType)
    {
        SenX_KOTH_PluginMain? inst = SenX_KOTH_PluginMain.Instance;
        if (inst?.Config == null)
            return;

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
            KoTHLog.Error(Log,e, "Discord could be down or there is something wrong with your webhook.");
        }
    }
}
