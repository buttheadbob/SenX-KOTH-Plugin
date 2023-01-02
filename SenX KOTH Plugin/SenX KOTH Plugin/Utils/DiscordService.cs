using NLog;
using System;
using System.Drawing;
using SenX_KOTH_Plugin.DiscordAPI;

namespace SenX_KOTH_Plugin.Utils
{
    public class DiscordService
    {
        public static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => DiscordService");        
        /// <summary>
        /// Send messages straight to discord
        /// </summary>
        /// <param name="msg">The message</param>
        /// <param name="EmbedColor">Embed bar color</param>
        /// <param name="AlertType">0 for contest alerts, 1 for rank listing</param>
        public static async void SendDiscordWebHook(string msg, Color? EmbedColor = null, int AlertType = 0)
        { // 0 = Koth under attack,  1 = rank announcement
            string tempTitle = "";
            if (SenX_KOTH_PluginMain.Instance.Config.CustomTitleEnable == false)
            {
                tempTitle = "Hank Says";
            }
            else
            {
                tempTitle = SenX_KOTH_PluginMain.Instance.Config.CustomTitle;
            }

            if (!SenX_KOTH_PluginMain.Instance.Config.WebHookEnabled)
                return;
            
            if (AlertType == 0 && !SenX_KOTH_PluginMain.Instance.Config.Show_AttackMessages)
                return;

            if (string.IsNullOrEmpty(SenX_KOTH_PluginMain.Instance.Config.WebHookUrl))
            {
                Log.Error("discord Webhook is enabled but the Webhook url is empty? That's not really going to accomplishing much.");
                return;
            }

            DiscordWebhook Webhook = new DiscordWebhook();
            DiscordMessage message = new DiscordMessage() { Username = "KoTH", AvatarUrl = "" };
            Webhook.Uri = new Uri(SenX_KOTH_PluginMain.Instance.Config.WebHookUrl);
            DiscordEmbed embed = new DiscordEmbed()
            {
                Title = tempTitle,
                Timestamp = DateTime.Now,
                Thumbnail = new EmbedMedia() { Url = "https://flxt.tmsimg.com/assets/p1976161_e_v8_ab.jpg" }
            };

            //Embed Title
            if (EmbedColor == null)
            { embed.Color = Color.Red; }
            else
            { embed.Color = EmbedColor; }

            if (!string.IsNullOrEmpty(SenX_KOTH_PluginMain.Instance.Config.MessegePrefix))
            {
                msg = $"{SenX_KOTH_PluginMain.Instance.Config.MessegePrefix} {msg}";
            }

            try
            {               
                if (SenX_KOTH_PluginMain.Instance.Config.EmbedEnabled)
                {
                    if (AlertType == 0)
                        embed.Fields.Add(new EmbedField() { Name = "Man Your Battle Stations!!!", Value = msg });
                    else
                        embed.Fields.Add(new EmbedField() { Name = "Rank Update", Value = msg });
                    message.Embeds.Add(embed);                   
                }

                else
                {
                   message.Content = $"***{tempTitle}*** {msg}";
                }

                await Webhook.SendAsync(message);
            }
            catch (Exception e)
            {
                Log.Error(e, "Discord could be down or there is something wrong with your webhook.");
            }
        }
    }
}
