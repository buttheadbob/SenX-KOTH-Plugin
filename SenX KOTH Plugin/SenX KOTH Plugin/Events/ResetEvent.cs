using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Timers;
using NLog;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Nexus;
using SenX_KOTH_Plugin.Utils;
using DrawingColor = System.Drawing.Color;

namespace SenX_KOTH_Plugin.Events
{
    internal sealed class ResetEvent : IKothEvent
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => ResetEvent");

        private readonly SenX_KOTH_PluginConfig _config;
        private readonly EventData _eventData;
        private readonly BanksData _bankData;
        private Timer? _timer;

        public string Name => "ResetEvent";
        public bool ShouldRun => true;
        public bool IsRunning { get; private set; }

        public ResetEvent(SenX_KOTH_PluginConfig config, EventData eventData, BanksData bankData)
        {
            _config = config;
            _eventData = eventData;
            _bankData = bankData;
        }

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
            try { ProcessWeekly(); ProcessMonthly(); ProcessYearly(); }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Error in ResetEvent tick."); }
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

        private void ProcessWeekly()
        {
            var now = DateTime.Now;
            if (!ShouldProcessWeekly(now)) return;

            if (_config.WeeklyRewardsEnabled)
            {
                var weekList = NexusManager.AccumulatedScores.WeekScores
                    .OrderByDescending(x => x.Value)
                    .Select(x => new KeyValuePair<string, int>(x.Key, (int)x.Value))
                    .ToList();

                if (_config.Show_WeeklyResults)
                {
                    AnnouncePeriodResults("Weekly", weekList,
                        DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Green);
                    RewardService.ExecutePeriodRewards(_config.WeeklyRankRewards.ToList(),
                        _config.WeeklyThresholdRewards.ToList(), weekList);
                }
            }

            MergePoints(_eventData.WeekEvents, _eventData.MonthEvents);
            MergeScores(NexusManager.AccumulatedScores.WeekScores, NexusManager.AccumulatedScores.MonthScores);

            _eventData.WeekEvents.Clear();
            NexusManager.AccumulatedScores.WeekScores.Clear();
            NexusManager.BroadcastWipe(WipePeriod.Week);

            _config.LastWeeklyProcessWeek = GetIsoWeek(now);
            _config.LastWeeklyProcessYear = now.Year;
            ForceSave();
        }

        private void ProcessMonthly()
        {
            var now = DateTime.Now;
            if (!ShouldProcessMonthly(now)) return;

            if (_config.MonthlyRewardsEnabled)
            {
                var monthList = NexusManager.AccumulatedScores.MonthScores
                    .OrderByDescending(x => x.Value)
                    .Select(x => new KeyValuePair<string, int>(x.Key, (int)x.Value))
                    .ToList();

                if (_config.Show_MonthlyResults)
                {
                    AnnouncePeriodResults("Monthly", monthList,
                        DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Silver);
                    RewardService.ExecutePeriodRewards(_config.MonthlyRankRewards.ToList(),
                        _config.MonthlyThresholdRewards.ToList(), monthList);
                }
            }

            MergePoints(_eventData.MonthEvents, _eventData.YearEvents);
            MergeScores(NexusManager.AccumulatedScores.MonthScores, NexusManager.AccumulatedScores.YearScores);

            _eventData.MonthEvents.Clear();
            NexusManager.AccumulatedScores.MonthScores.Clear();
            NexusManager.BroadcastWipe(WipePeriod.Month);

            _config.LastMonthlyProcessMonth = now.Month;
            _config.LastMonthlyProcessYear = now.Year;
            ForceSave();
        }

        private void ProcessYearly()
        {
            var now = DateTime.Now;
            if (!ShouldProcessYearly(now)) return;

            if (_config.YearlyRewardsEnabled)
            {
                var yearList = NexusManager.AccumulatedScores.YearScores
                    .OrderByDescending(x => x.Value)
                    .Select(x => new KeyValuePair<string, int>(x.Key, (int)x.Value))
                    .ToList();

                if (_config.Show_YearlyResults)
                {
                    AnnouncePeriodResults("Yearly", yearList,
                        DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Green);
                    RewardService.ExecutePeriodRewards(_config.YearlyRankRewards.ToList(),
                        _config.YearlyThresholdRewards.ToList(), yearList);
                }
            }

            _eventData.YearEvents.Clear();
            NexusManager.AccumulatedScores.YearScores.Clear();
            NexusManager.BroadcastWipe(WipePeriod.Year);

            _config.LastYearlyProcessYear = now.Year;
            ForceSave();
        }

        private static void MergePoints(ObservableConcurrentUiSafeCollection<PointEarned> from, ObservableConcurrentUiSafeCollection<PointEarned> to)
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

        private void ForceSave()
        {
            SenX_KOTH_PluginMain.EventPersist?.Save();
            SenX_KOTH_PluginMain.ScorePersist?.Save();
            SenX_KOTH_PluginMain.ConfigPersist?.Save();
        }

        private static void AnnouncePeriodResults(string periodName, List<KeyValuePair<string, int>> sortedScores,
            DrawingColor firstColor, DrawingColor secondColor, DrawingColor thirdColor, DrawingColor restColor)
        {
            var results = new StringBuilder();
            var sentRest = false;
            for (int i = 0; i < sortedScores.Count; i++)
            {
                switch (i)
                {
                    case 0: results.AppendLine("First Place"); results.AppendLine(sortedScores[i].ToString()); DiscordService.SendDiscordWebHook(results.ToString(), firstColor, 1); results.Clear(); break;
                    case 1: results.AppendLine("Second Place"); results.AppendLine(sortedScores[i].ToString()); DiscordService.SendDiscordWebHook(results.ToString(), secondColor, 1); results.Clear(); break;
                    case 2: results.AppendLine("Third Place"); results.AppendLine(sortedScores[i].ToString()); DiscordService.SendDiscordWebHook(results.ToString(), thirdColor, 1); results.Clear(); break;
                    default: if (!sentRest) { results.AppendLine("The Other People...."); sentRest = true; } results.AppendLine(sortedScores[i].ToString()); break;
                }
            }
            if (sortedScores.Count > 3 && results.Length > 0) DiscordService.SendDiscordWebHook(results.ToString(), restColor, 1);
        }
    }
}
