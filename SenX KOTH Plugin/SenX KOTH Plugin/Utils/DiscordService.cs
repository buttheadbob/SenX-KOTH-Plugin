using NLog;
using System;
using System.Drawing;
using SenX_KOTH_Plugin.DiscordAPI;
using Extensions = SenX_KOTH_Plugin.DiscordAPI.Extensions;

namespace SenX_KOTH_Plugin.Utils
{
    public sealed class DiscordService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => DiscordService");        
        /// <summary>
        /// Send messages straight to discord
        /// </summary>
        /// <param name="msg">The message</param>
        /// <param name="EmbedColor">Embed bar color</param>
        /// <param name="AlertType">0 for contest alerts, 1 for rank listing</param>
        public static async void SendDiscordWebHook(string msg, Color? EmbedColor = null, int AlertType = 0)
        { // 0 = Koth under attack,  1 = rank announcement
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
                tempTitle = SenX_KOTH_PluginMain.Instance!.Config!.CustomTitleEnable == false
                    ? "Hank Says"
                    : SenX_KOTH_PluginMain.Instance.Config.CustomTitle;
            }
            if (!SenX_KOTH_PluginMain.Instance!.Config!.WebHookEnabled)
                return;
            
            if (AlertType == 0 && !SenX_KOTH_PluginMain.Instance.Config.Show_AttackMessages)
                return;

            if (string.IsNullOrEmpty(SenX_KOTH_PluginMain.Instance.Config.WebHookUrl))
            {
                Log.Error("discord Webhook is enabled but the Webhook url is empty? That's not really going to accomplishing much.");
                return;
            }

            var WebHook = new DiscordWebHook();
            var message = new DiscordMessage { Username = "KoTH", AvatarUrl = "" };
            WebHook.Uri = new Uri(SenX_KOTH_PluginMain.Instance.Config.WebHookUrl);

            string EmbedURL = SenX_KOTH_PluginMain.Instance.Config.DefaultEmbedPic
                ? "https://i.ibb.co/4W1gG3d/oie-png-1.png"
                : SenX_KOTH_PluginMain.Instance.Config.EmbedPic;

            DiscordEmbed embed = new ()
            {
                Title = tempTitle,
                Timestamp = DateTime.Now,
                Thumbnail = new EmbedMedia() { Url = EmbedURL },
                //Embed Title
                Color = EmbedColor == null ? Extensions.ToHex(Color.Red) : EmbedColor.ToHex()
            };

            if (!string.IsNullOrEmpty(SenX_KOTH_PluginMain.Instance.Config.MessagePrefix))
                msg = $"{SenX_KOTH_PluginMain.Instance.Config.MessagePrefix}\n{msg}";

            try
            {               
                if (SenX_KOTH_PluginMain.Instance.Config.EmbedEnabled)
                {
                    embed.Fields.Add(AlertType == 0
                        ? new EmbedField() {Name = "Man Your Battle Stations!!!", Value = msg}
                        : new EmbedField() {Name = "***Rank Update***", Value = msg});
                    
                    message.Embeds.Add(embed);
                }

                else
                   message.Content = $"***{tempTitle}*** {msg}";

                await WebHook.SendAsync(message);
            }
            catch (Exception e)
            {
                Log.Error(e, "Discord could be down or there is something wrong with your webhook.");
            }
        }
    }
}
