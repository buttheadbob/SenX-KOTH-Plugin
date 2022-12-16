using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Utils.SteamWorkshopTools;
using SenX_KOTH_Plugin.DiscordAPI;

namespace SenX_KOTH_Plugin.Utils
{    
    public class DiscordService
    {
        public static readonly Logger Log = LogManager.GetLogger("KoTH => DiscordService");


        public static async void SendDiscordWebHook(string msg)
        {
            if (!SenX_KOTH_PluginMain.Instance.Config.WebHookEnabled)
                return;
            
            if (msg.Contains("under attack"))
                return;

            if (string.IsNullOrEmpty(SenX_KOTH_PluginMain.Instance.Config.WebHookUrl))
            {
                Log.Error("discord Webhook is enabled but the Webhook url is empty? you should fix that!");
                return;
            }

            DiscordWebhook Webhook = new DiscordWebhook();
            DiscordMessage message = new DiscordMessage() { Username = "KoTH", AvatarUrl = "https://flxt.tmsimg.com/assets/p1976161_e_v8_ab.jpg" };
            Webhook.Uri = new Uri(SenX_KOTH_PluginMain.Instance.Config.WebHookUrl);
            DiscordEmbed embed = new DiscordEmbed()
            {
                Title = "Hank Says",
                Timestamp = DateTime.Now,
                Color = Color.DarkGray,
                Thumbnail = new EmbedMedia() { Url = "https://flxt.tmsimg.com/assets/p1976161_e_v8_ab.jpg" }
            };

            if (!string.IsNullOrEmpty(SenX_KOTH_PluginMain.Instance.Config.MessegePrefix))
            {
                msg = $"{SenX_KOTH_PluginMain.Instance.Config.MessegePrefix} {msg}";
            }

            try
            {
               
                if (SenX_KOTH_PluginMain.Instance.Config.EmbedEnabled)
                {
                    embed.Fields.Add(new EmbedField() { Name = "Man Your BattleStations!!!", Value= msg });
                    message.Embeds.Add(embed);                   
                }

                else
                {
                   message.Content= msg;
                }

                await Webhook.SendAsync(message);
            }
            catch (Exception e)
            {
                Log.Error(e, "discord Webhook is most likely bad or discord is down");
            }
        }
    }
}
