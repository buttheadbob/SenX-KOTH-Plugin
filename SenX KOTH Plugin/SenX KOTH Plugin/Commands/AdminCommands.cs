using System;
using System.Drawing;
using System.Text;
using NLog;
using Torch.Commands.Permissions;
using Torch.Commands;
using VRage.Game.ModAPI;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Commands
{
    [Category("KoTH")]
    public sealed class KothAdminCommands : CommandModule
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => AdminCommands");

        private bool CheckCooldown()
        {
            if (Context.Player == null) return true;
            if (!CommandCooldown.TryUse(Context.Player.SteamUserId))
            {
                Context.Respond("Please wait 5 seconds before using another command.");
                return false;
            }
            return true;
        }

        [Command("ForceTest", "Forces an announcement to test the discord webhook.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ForceWebHookTest()
        {
            if (!CheckCooldown()) return;
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, "First Place: [Vengeful Idiots] with 2565 Points!", Color.Gold, 1);
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, "Second Place: [Space Nuggets] with 1954 Points!", Color.Silver, 1);
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, "Third Place: [Keyboard Warriors] with 584 Points!", Color.SandyBrown, 1);

            var sb = new StringBuilder();
            sb.AppendLine("The Other People....");
            sb.AppendLine("Hamsters of Europa with 486 Points!");
            sb.AppendLine("TRex's with 386 Points!");
            sb.AppendLine("Muppet Empire with 212 Points!");
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, sb.ToString(), Color.Brown, 1);
        }

        [Command("GivePoint", "Gives or removes faction bank points. Usage: !KoTH GivePoint <factionId_or_tag> <value>")]
        [Permission(MyPromoteLevel.Admin)]
        public async void GivePoint(string input, int value)
        {
            if (!CheckCooldown()) return;

            try
            {
                string result = await BankService.AdminAdjustPointsAsync(input, value);
                GameThread.Invoke(() => Context.Respond(result), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "GivePoint command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }
    }
}
