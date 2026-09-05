using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using NLog;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;
using DrawingColor = System.Drawing.Color;

namespace SenX_KOTH_Plugin.Events
{
    internal sealed class ResetEvent : IKothEvent
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => ResetEvent");

        private readonly SenX_KOTH_PluginConfig _config;
        private Timer? _timer;

        public string Name => "ResetEvent";
        public bool ShouldRun => true;
        public bool IsRunning { get; private set; }

        public ResetEvent(SenX_KOTH_PluginConfig config) => _config = config;

        private string EventPath => Path.Combine(SenX_KOTH_PluginMain.LocalDataPath, "EventData.json");
        private string ScorePath => Path.Combine(SenX_KOTH_PluginMain.DataPath, "ScoreData.json");

        public void Start()
        {
            _timer = new Timer(60000);
            _timer.Elapsed += Tick;
            _timer.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            IsRunning = false;
        }

        public void Update() { }
        public void IntegrityCheck() { }
        public void Save() { }

        private void Tick(object? sender, ElapsedEventArgs e)
        {
            try
            {
                PointBuffer.Flush();
                ProcessWeekly();
                ProcessMonthly();
                ProcessYearly();
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Error in ResetEvent tick."); }
        }

        private bool ShouldProcessWeekly(DateTime now)
        {
            return _config.LastWeeklyProcessYear == 0
                || _config.LastWeeklyProcessYear != now.Year
                || GetIsoWeek(now) != _config.LastWeeklyProcessWeek;
        }

        private bool ShouldProcessMonthly(DateTime now)
        {
            return _config.LastMonthlyProcessYear == 0
                || _config.LastMonthlyProcessYear != now.Year
                || now.Month != _config.LastMonthlyProcessMonth;
        }

        private bool ShouldProcessYearly(DateTime now)
        {
            return _config.LastYearlyProcessYear == 0
                || now.Year != _config.LastYearlyProcessYear;
        }

        // --- Period processing ---

        private void ProcessWeekly()
        {
            var now = DateTime.Now;
            if (!ShouldProcessWeekly(now)) return;

            List<KeyValuePair<string, int>>? weekList = null;

            SharedFile.ReadModifyWrite<ScoreFile>(ScorePath, data =>
            {
                var scores = data ?? new ScoreFile();
                if (_config.WeeklyRewardsEnabled)
                {
                    weekList = scores.WeekScores
                        .OrderByDescending(x => x.Value)
                        .Select(x => new KeyValuePair<string, int>(x.Key, (int)x.Value))
                        .ToList();
                }
                MergeScores(scores.WeekScores, scores.MonthScores);
                scores.WeekScores.Clear();
                return scores;
            }, out _);

            if (_config.WeeklyRewardsEnabled && _config.Show_WeeklyResults && weekList != null)
            {
                AnnouncePeriodResults("Weekly", weekList,
                    DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Green);
                RewardService.ExecutePeriodRewards(_config.WeeklyRankRewards.ToList(),
                    _config.WeeklyThresholdRewards.ToList(), weekList);
            }

            SharedFile.ReadModifyWrite<EventData>(EventPath, data =>
            {
                var events = data ?? new EventData();
                MergePoints(events.WeekEvents, events.MonthEvents);
                events.WeekEvents.Clear();
                return events;
            }, out _);

            _config.LastWeeklyProcessWeek = GetIsoWeek(now);
            _config.LastWeeklyProcessYear = now.Year;
            SenX_KOTH_PluginMain.ConfigPersist?.Save();
        }

        private void ProcessMonthly()
        {
            var now = DateTime.Now;
            if (!ShouldProcessMonthly(now)) return;

            List<KeyValuePair<string, int>>? monthList = null;

            SharedFile.ReadModifyWrite<ScoreFile>(ScorePath, data =>
            {
                var scores = data ?? new ScoreFile();
                if (_config.MonthlyRewardsEnabled)
                {
                    monthList = scores.MonthScores
                        .OrderByDescending(x => x.Value)
                        .Select(x => new KeyValuePair<string, int>(x.Key, (int)x.Value))
                        .ToList();
                }
                MergeScores(scores.MonthScores, scores.YearScores);
                scores.MonthScores.Clear();
                return scores;
            }, out _);

            if (_config.MonthlyRewardsEnabled && _config.Show_MonthlyResults && monthList != null)
            {
                AnnouncePeriodResults("Monthly", monthList,
                    DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Silver);
                RewardService.ExecutePeriodRewards(_config.MonthlyRankRewards.ToList(),
                    _config.MonthlyThresholdRewards.ToList(), monthList);
            }

            SharedFile.ReadModifyWrite<EventData>(EventPath, data =>
            {
                var events = data ?? new EventData();
                MergePoints(events.MonthEvents, events.YearEvents);
                events.MonthEvents.Clear();
                return events;
            }, out _);

            _config.LastMonthlyProcessMonth = now.Month;
            _config.LastMonthlyProcessYear = now.Year;
            SenX_KOTH_PluginMain.ConfigPersist?.Save();
        }

        private void ProcessYearly()
        {
            var now = DateTime.Now;
            if (!ShouldProcessYearly(now)) return;

            List<KeyValuePair<string, int>>? yearList = null;

            SharedFile.ReadModifyWrite<ScoreFile>(ScorePath, data =>
            {
                var scores = data ?? new ScoreFile();
                if (_config.YearlyRewardsEnabled)
                {
                    yearList = scores.YearScores
                        .OrderByDescending(x => x.Value)
                        .Select(x => new KeyValuePair<string, int>(x.Key, (int)x.Value))
                        .ToList();
                }
                scores.YearScores.Clear();
                return scores;
            }, out _);

            if (_config.YearlyRewardsEnabled && _config.Show_YearlyResults && yearList != null)
            {
                AnnouncePeriodResults("Yearly", yearList,
                    DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Green);
                RewardService.ExecutePeriodRewards(_config.YearlyRankRewards.ToList(),
                    _config.YearlyThresholdRewards.ToList(), yearList);
            }

            SharedFile.ReadModifyWrite<EventData>(EventPath, data =>
            {
                var events = data ?? new EventData();
                events.YearEvents.Clear();
                return events;
            }, out _);

            _config.LastYearlyProcessYear = now.Year;
            SenX_KOTH_PluginMain.ConfigPersist?.Save();
        }

        private static void MergePoints(List<PointEarned> from, List<PointEarned> to)
        {
            foreach (var pt in from)
            {
                if (!to.Any(e => e.FromServerID == pt.FromServerID && e.EventId == pt.EventId))
                    to.Add(pt);
            }
        }

        internal static void MergeScores(
            List<KeyValuePair<string, ulong>> from,
            List<KeyValuePair<string, ulong>> to)
        {
            foreach (var kv in from)
            {
                int idx = to.FindIndex(x => x.Key == kv.Key);
                if (idx >= 0)
                    to[idx] = new KeyValuePair<string, ulong>(kv.Key, to[idx].Value + kv.Value);
                else
                    to.Add(kv);
            }
        }

        private static int GetIsoWeek(DateTime d)
        {
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private static void AnnouncePeriodResults(string periodName, List<KeyValuePair<string, int>> sortedScores,
            DrawingColor firstColor, DrawingColor secondColor, DrawingColor thirdColor, DrawingColor restColor)
        {
            var results = new StringBuilder();
            var periodResults = new StringBuilder();
            var sentRest = false;
            for (int i = 0; i < sortedScores.Count; i++)
            {
                string medal = i == 0 ? "\U0001f947 " : i == 1 ? "\U0001f948 " : i == 2 ? "\U0001f949 " : "    ";
                periodResults.AppendLine(medal + sortedScores[i].Key + " \u2014 " + sortedScores[i].Value + " pts");

                switch (i)
                {
                    case 0: results.AppendLine("First Place"); results.AppendLine(sortedScores[i].ToString()); AnnouncementService.RankResult(results.ToString(), firstColor); results.Clear(); break;
                    case 1: results.AppendLine("Second Place"); results.AppendLine(sortedScores[i].ToString()); AnnouncementService.RankResult(results.ToString(), secondColor); results.Clear(); break;
                    case 2: results.AppendLine("Third Place"); results.AppendLine(sortedScores[i].ToString()); AnnouncementService.RankResult(results.ToString(), thirdColor); results.Clear(); break;
                    default: if (!sentRest) { results.AppendLine("The Other People...."); sentRest = true; } results.AppendLine(sortedScores[i].ToString()); break;
                }
            }
            if (sortedScores.Count > 3 && results.Length > 0) AnnouncementService.RankResult(results.ToString(), restColor);

            AnnouncementService.PeriodResults(periodName, periodResults.ToString());
        }
    }
}
