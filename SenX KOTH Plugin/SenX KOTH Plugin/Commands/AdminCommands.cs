using System.Drawing;
using System.Text;
using System.Threading;
using Torch.Commands.Permissions;
using Torch.Commands;
using VRage.Game.ModAPI;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Commands
{
    [Category("KoTH")]
    public sealed class KothAdminCommands : CommandModule
    {
        [Command("ForceUpdate", "Forces the score to update off schedule.")]
        [Permission(MyPromoteLevel.Admin)]
        public void AnnounceCurrentWeek()
        {
            ResetScores.ProcessScoresAndReset();
        }
        
        [Command("ForceTest", "Forces the score to update off schedule.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ForceWebHookTest()
        {
            DiscordService.SendDiscordWebHook("First Place Vengeful Idiots with 2565 Points!", Color.Gold, 1);
            Thread.Sleep(5000);
            DiscordService.SendDiscordWebHook("Second Place Space Nuggets with 1954 Points!", Color.Silver, 1);
            Thread.Sleep(5000);
            DiscordService.SendDiscordWebHook("Third Place Legionly Legions with 584 Points!", Color.SandyBrown, 1);
            Thread.Sleep(5000);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The Other People....");
            sb.AppendLine("Hamsters of Europa with 486 Points!");
            sb.AppendLine("TRex's with 386 Points!");
            sb.AppendLine("Muppet Empire with 212 Points!");
            DiscordService.SendDiscordWebHook(sb.ToString(), Color.Brown, 1);
        }
    }
}
