using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;
using Sandbox.Game.World;

namespace SenX_KOTH_Plugin.Utils
{
    public enum DayOfReset
    {
        Sunday = 0,
        Monday = 1
    }

    public sealed class LiveAgent
    {
        // If today is reset day, used to check for reset time and initiate the reset process.
        private readonly Timer ResetChecker = new ();
        private SenX_KOTH_PluginConfig Config => SenX_KOTH_PluginMain.Instance.Config;

        public void Run()
        {
            ResetChecker.Interval = 300000; // run once every five minutes.
            ResetChecker.Elapsed += Reset;
            ResetChecker.Start();

            // Start the game instance with most up to date information
            ResetScores.ProcessScoresAndReset();

            // Need to reset weekly and monthly scores when due..
            if (Config.ResetDay == DayOfReset.Monday && DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                ResetAndAnnounce();
            if (Config.ResetDay == DayOfReset.Sunday && DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                ResetAndAnnounce();
        }

        private void ResetAndAnnounce()
        {
            if (Config.LastWeeklyReset.Date == DateTime.Today.Date) return;

            // Announce weekly score if enabled.
            if (Config.Show_WeeklyResults)
            {
                StringBuilder WeekResults = new ();

                // Create a formatted ranking list
                List<KeyValuePair<string, int>> weekList = SenX_KOTH_PluginMain.MasterScore.WeekScores.ToList();

                // Sort the list.
                weekList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                // Push list to WeekResults
                for (int index = 0; index < weekList.Count; index++)
                {
                    switch (index)
                    {
                        case 0:
                            WeekResults.AppendLine("Weekly Rank First Place");
                            WeekResults.AppendLine(weekList[0].ToString());
                            DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Gold, 1);
                            Delay(1);
                            WeekResults.Clear();
                            
                            continue;

                        case 1:
                            WeekResults.AppendLine("Weekly Rank Second Place");
                            WeekResults.AppendLine(weekList[1].ToString());
                            DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Silver, 1);
                            WeekResults.Clear();
                            Delay(1);
                            continue;

                        case 2:
                            WeekResults.Clear();
                            WeekResults.AppendLine("Weekly Rank Third Place");
                            WeekResults.AppendLine(weekList[2].ToString());
                            DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.SandyBrown, 1);
                            Delay(1);
                            WeekResults.Clear();
                            continue;

                        default:
                            if (index >= 3)
                                WeekResults.AppendLine("Weekly Rank The Other People....");
                            
                            KeyValuePair<string, int> Result = weekList[index];
                            WeekResults.AppendLine(Result.ToString());
                            continue;
                    }
                }

                DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Green, 1);

                SenX_KOTH_PluginMain.MasterScore.WeekScores.Clear();
                Config.LastWeeklyReset = DateTime.Now;
                SenX_KOTH_PluginMain.Save_MasterData(SenX_KOTH_PluginMain.MasterScore);
            }

            if (!Config.Show_MonthlyResults || DateTime.Now.Day != 1 || Config.LastMonthlyReset.Date.Month == DateTime.Now.Month)
            {
                // Do nothing...     
            } 
            else
            {
                // Announce monthly score if enabled.
                StringBuilder monthResults = new ();

                // Create a formatted ranking list
                List<KeyValuePair<string, int>> monthList = SenX_KOTH_PluginMain.MasterScore.MonthScores;

                // Sort the list.
                monthList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                // Push list to weekResults 
                foreach (KeyValuePair<string, int> Result in monthList)
                {
                    monthResults.AppendLine(Result.ToString());
                }

                DiscordService.SendDiscordWebHook(monthResults.ToString(), Color.Silver, 1);

                SenX_KOTH_PluginMain.MasterScore.MonthScores.Clear();
                Config.LastMonthlyReset = DateTime.Now;
                SenX_KOTH_PluginMain.Save_MasterData(SenX_KOTH_PluginMain.MasterScore);
            }
        }

        private static void Reset(object sender, ElapsedEventArgs e)
        {
            if (!MySession.Static.IsSaveInProgress)
                ResetScores.ProcessScoresAndReset();
        }

        public void Dispose()
        {
            ResetChecker.Stop();
            ResetChecker.Dispose();
        }

        private static void Delay(double seconds)
        {
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }
    }
}