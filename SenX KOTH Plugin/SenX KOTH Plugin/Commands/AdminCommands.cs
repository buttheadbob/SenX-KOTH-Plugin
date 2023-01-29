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
    }
}
