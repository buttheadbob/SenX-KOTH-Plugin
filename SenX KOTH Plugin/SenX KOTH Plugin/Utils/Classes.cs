using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Sandbox.Game.World;

namespace SenX_KOTH_Plugin.Utils
{
    [Serializable]
    public class SerializableKeyValuePair<TKey, TValue>
    {
        public SerializableKeyValuePair()
        {
        }

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; set; }
        public TValue Value { get; set; }

    }
    public enum DayOfReset
    {
        Sunday = 0,
        Monday = 1
    }

    public sealed class LiveAgent
    {
        // If today is reset day, used to check for reset time and initiate the reset process.
        private System.Timers.Timer ResetChecker = new System.Timers.Timer();
        private SenX_KOTH_PluginConfig Config => SenX_KOTH_PluginMain.Instance.Config;

        public void Run()
        {
            ResetChecker.Interval = 300000; // run once every five mins.
            ResetChecker.Elapsed += Reset;
            ResetChecker.Start();

            // Start the game instance with most up to date information
            ResetScores.ProcessScoresAndReset();

            // Need to reset weekly and monthly scores when due..
            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday && Config.LastWeeklyReset.Date != DateTime.Today.Date && Config.ResetDay == DayOfReset.Sunday)
            {
                // Announce weekly score if enabled.
                StringBuilder WeekResults = new StringBuilder();

                // Create a formatted ranking list
                var weeklist = SenX_KOTH_PluginMain.MasterScore.WeekScores.ToList();

                // Sort the list.
                weeklist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                // Push list to weekresults
                foreach (var Result in weeklist)
                {
                    WeekResults.AppendLine(Result.ToString());
                }

                DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Gold, 1);

                SenX_KOTH_PluginMain.MasterScore.WeekScores.Clear();
                Config.LastWeeklyReset = DateTime.Now;
                SenX_KOTH_PluginMain.Save_MasterData(SenX_KOTH_PluginMain.MasterScore);
            }

            if (DateTime.Now.DayOfWeek == DayOfWeek.Monday && Config.LastWeeklyReset.Date != DateTime.Today.Date && Config.ResetDay == DayOfReset.Monday)
            {
                // Announce weekly score if enabled.
                StringBuilder WeekResults = new StringBuilder();

                // Create a formatted ranking list
                var weeklist = SenX_KOTH_PluginMain.MasterScore.WeekScores.ToList();

                // Sort the list.
                weeklist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                // Push list to weekresults
                foreach (var Result in weeklist)
                {
                    WeekResults.AppendLine(Result.ToString());
                }

                DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Gold, 1);

                SenX_KOTH_PluginMain.MasterScore.WeekScores.Clear();
                Config.LastWeeklyReset = DateTime.Now;
                SenX_KOTH_PluginMain.Save_MasterData(SenX_KOTH_PluginMain.MasterScore);
            }

            if (DateTime.Now.Day == 1 && Config.LastMonthlyReset.Date.Month != DateTime.Now.Month)
            {
                // Announce monthly score if enabled.
                StringBuilder MonthResults = new StringBuilder();

                // Create a formatted ranking list
                var monthlist = SenX_KOTH_PluginMain.MasterScore.MonthScores.ToList();

                // Sort the list.
                monthlist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                // Push list to weekresults
                foreach (var Result in monthlist)
                {
                    MonthResults.AppendLine(Result.ToString());
                }

                DiscordService.SendDiscordWebHook(MonthResults.ToString(), Color.Silver, 1);

                SenX_KOTH_PluginMain.MasterScore.MonthScores.Clear();
                Config.LastMonthlyReset = DateTime.Now;
                SenX_KOTH_PluginMain.Save_MasterData(SenX_KOTH_PluginMain.MasterScore);
            }
        }

        public void Reset(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!MySession.Static.IsSaveInProgress)
            ResetScores.ProcessScoresAndReset();
        }

        public void Dispose()
        {
            ResetChecker.Stop();
            ResetChecker.Dispose();
        }
    }

    
}
