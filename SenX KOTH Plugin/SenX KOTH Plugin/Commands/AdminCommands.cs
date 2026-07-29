using System;
using System.Linq;
using System.Drawing;
using System.Text;
using Torch.Commands.Permissions;
using Torch.Commands;
using VRage.Game.ModAPI;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Nexus;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Commands
{
    [Category("KoTH")]
    public sealed class KothAdminCommands : CommandModule
    {
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

        [Command("ForceUpdate", "Forces the score to update and broadcast to Nexus if enabled.")]
        [Permission(MyPromoteLevel.Admin)]
        public void AnnounceCurrentWeek()
        {
            if (!CheckCooldown()) return;
            NexusManager.BroadcastVerification();
            Context.Respond("Score update triggered and broadcast to Nexus (if enabled).");
        }

        [Command("ForceTest", "Forces an announcement to test the discord webhook.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ForceWebHookTest()
        {
            if (!CheckCooldown()) return;
            DiscordService.SendDiscordWebHook("First Place: [Vengeful Idiots] with 2565 Points!", Color.Gold, 1);
            DiscordService.SendDiscordWebHook("Second Place: [Space Nuggets] with 1954 Points!", Color.Silver, 1);
            DiscordService.SendDiscordWebHook("Third Place: [Keyboard Warriors] with 584 Points!", Color.SandyBrown, 1);

            var sb = new StringBuilder();
            sb.AppendLine("The Other People....");
            sb.AppendLine("Hamsters of Europa with 486 Points!");
            sb.AppendLine("TRex's with 386 Points!");
            sb.AppendLine("Muppet Empire with 212 Points!");
            DiscordService.SendDiscordWebHook(sb.ToString(), Color.Brown, 1);
        }

        [Command("ForceNexusSync", "Forces Nexus verification broadcast immediately.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ForceNexusSync()
        {
            if (!CheckCooldown()) return;
            NexusManager.BroadcastVerification();
            Context.Respond("Nexus verification broadcast sent.");
        }

        [Command("ForceRewardSync", "Forces reward config sync to all Nexus servers.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ForceRewardSync()
        {
            if (!CheckCooldown()) return;
            NexusManager.BroadcastRewardConfig();
            Context.Respond("Reward config sync broadcast sent.");
        }

        [Command("CreateZone", "Creates a KoTH zone at your current position.")]
        [Permission(MyPromoteLevel.Admin)]
        public void CreateZone(string name, float radius = 50f)
        {
            if (!CheckCooldown()) return;
            if (string.IsNullOrWhiteSpace(name))
            {
                Context.Respond("Usage: !KoTH CreateZone <name> [radius]");
                return;
            }

            var player = Context.Player;
            if (player?.Character == null)
            {
                Context.Respond("You must be in-game with a character to create a zone.");
                return;
            }

            var persist = SenX_KOTH_PluginMain.ZonePersist;
            if (persist == null) return;

            var pos = player.Character.WorldMatrix.Translation;
            var zone = ZoneManager.CreateZone(persist.Data, name, radius, pos, player.DisplayName ?? "Admin");

            if (zone != null)
            {
                persist.Save();
                Context.Respond("Zone '" + name + "' created at your position with radius " + zone.Radius + "m.");
            }
            else
            {
                Context.Respond("Failed to create zone '" + name + "'. It may already exist.");
            }
        }

        [Command("DeleteZone", "Deletes a KoTH zone by name.")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteZone(string name)
        {
            if (!CheckCooldown()) return;
            if (string.IsNullOrWhiteSpace(name))
            {
                Context.Respond("Usage: !KoTH DeleteZone <name>");
                return;
            }

            var persist = SenX_KOTH_PluginMain.ZonePersist;
            if (persist == null) return;

            if (ZoneManager.DeleteZone(persist.Data, name))
            {
                persist.Save();
                Context.Respond("Zone '" + name + "' deleted.");
            }
            else
                Context.Respond("Zone '" + name + "' not found.");
        }

        [Command("GivePoint", "Gives or removes faction bank points. Usage: !KoTH GivePoint <factionId_or_tag> <value>")]
        [Permission(MyPromoteLevel.Admin)]
        public void GivePoint(string input, int value)
        {
            if (!CheckCooldown()) return;
            var persist = SenX_KOTH_PluginMain.BankPersist;
            if (persist == null) return;

            if (BankService.AdminAdjustPoints(persist.Data, input, value, out string result))
            {
                persist.Save();
                Context.Respond(result);
            }
            else
                Context.Respond(result);
        }
    }
}
