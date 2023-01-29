using System;
using System.Globalization;
using System.Linq;
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
        [Command("Week", "Shows the current KoTH ranking and points for the week.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowWeek()
        {
            var WeekResults = new StringBuilder();
            WeekResults.AppendLine();
            const string Prefix = "Standing Weekly Results";


            // Create a formatted ranking list

            var weekList = SenX_KOTH_PluginMain.MasterScore.WeekScores.ToList();

            // Sort the list.
            weekList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekResults
            foreach(var Result in weekList)
            {
                WeekResults.AppendLine($"{Result.Key} => {Result.Value}");
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
            var MonthResults = new StringBuilder();
            MonthResults.AppendLine();
            var Prefix = "Standing Results for " + DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture); ;

            // Create a formatted ranking list

            var monthList = SenX_KOTH_PluginMain.MasterScore.MonthScores.ToList();

            // Sort the list.
            monthList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekResults
            foreach (var Result in monthList)
            {
                MonthResults.AppendLine($"{Result.Key} => {Result.Value}");
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
            var YearResults = new StringBuilder();
            YearResults.AppendLine();
            var Prefix = "Standing Results for " + DateTime.Now.Year;
                        
            // Create a formatted ranking list
            var yearList = SenX_KOTH_PluginMain.MasterScore.YearScores.ToList();

            // Sort the list.
            yearList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekResults
            foreach (var Result in yearList)
            {
                YearResults.AppendLine($"{Result.Key} => {Result.Value}");
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

        [Command("About", "Shows the current KoTH ranking and points for the week/month/year with a few added options.  For any feature request, contact SentorX#0001 on Discord.")]
        [Permission(MyPromoteLevel.None)]
        public void About()
        {
            var YearResults = new StringBuilder();
            YearResults.AppendLine();
            var Prefix = "Standing Results for " + DateTime.Now.Year;

            // Create a formatted ranking list
            var yearList = SenX_KOTH_PluginMain.MasterScore.YearScores.ToList();

            // Sort the list.
            yearList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekResults
            foreach (var Result in yearList)
            {
                YearResults.AppendLine($"{Result.Key} => {Result.Value}");
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
