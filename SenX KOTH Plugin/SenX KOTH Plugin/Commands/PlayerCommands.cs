using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRageMath;

namespace SenX_KOTH_Plugin.Commands
{

    [Category("KoTH")]
    public sealed class KothPlayerCommands : CommandModule
    {
        SenX_KOTH_PluginConfig Config => SenX_KOTH_PluginMain.Instance.Config;

        [Command("Week", "Shows the current KoTH ranking and points for the week.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowWeek()
        {
            StringBuilder WeekResults = new StringBuilder();
            string Prefix = "Standing Weekly Results";

            List<KeyValuePair<string, int>> weeklist = new List<KeyValuePair<string, int>>();
            
            // Create a formatted ranking list
            foreach(var Result in Config.WeekScoreData)
            {
                weeklist.Add(new KeyValuePair<string, int>(Result.FactionName, Result.Points));
            }

            // Sort the list.
            weeklist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekresults
            foreach(var Result in weeklist)
            {
                WeekResults.AppendLine(Result.ToString());
            }

            if (Context.Player != null)
            {
                ModCommunication.SendMessageTo(new DialogMessage("KoTH", Prefix, WeekResults.ToString()), Context.Player.SteamUserId);
            } else
            {
                Context.Respond(WeekResults.ToString(), Color.Gold, "KoTH");
            }
        }

        [Command("Month", "Shows the current KoTH ranking and points for the month.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowMonth()
        {
            StringBuilder MonthResults = new StringBuilder();
            string Prefix = "Standing Month Results";

            List<KeyValuePair<string, int>> Monthlist = new List<KeyValuePair<string, int>>();

            // Create a formatted ranking list
            foreach (var Result in Config.MonthScoreRecord)
            {
                Monthlist.Add(new KeyValuePair<string, int>(Result.FactionName, Result.Points));
            }

            // Sort the list.
            Monthlist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekresults
            foreach (var Result in Monthlist)
            {
                MonthResults.AppendLine(Result.ToString());
            }

            if (Context.Player != null)
            {
                ModCommunication.SendMessageTo(new DialogMessage("KoTH", Prefix, MonthResults.ToString()), Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond(MonthResults.ToString(), Color.Gold, "KoTH");
            }
        }

        [Command("Year", "Shows the current KoTH ranking and points for the year.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowYear()
        {
            StringBuilder YearResults = new StringBuilder();
            string Prefix = "Standing Month Results";

            List<KeyValuePair<string, int>> Yearlist = new List<KeyValuePair<string, int>>();

            // Create a formatted ranking list
            foreach (var Result in Config.MonthScoreRecord)
            {
                Yearlist.Add(new KeyValuePair<string, int>(Result.FactionName, Result.Points));
            }

            // Sort the list.
            Yearlist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekresults
            foreach (var Result in Yearlist)
            {
                YearResults.AppendLine(Result.ToString());
            }

            if (Context.Player != null)
            {
                ModCommunication.SendMessageTo(new DialogMessage("KoTH", Prefix, YearResults.ToString()), Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond(YearResults.ToString(), Color.Gold, "KoTH");
            }
        }
    }
}
