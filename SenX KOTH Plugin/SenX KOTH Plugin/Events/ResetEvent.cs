using System;
using System.Collections.Generic;
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
            try { CheckWeeklyReset(); CheckMonthlyReset(); CheckYearlyReset(); }
            catch (Exception ex) { Log.Error(ex, "Error in ResetEvent tick."); }
        }

        private void CheckWeeklyReset()
        {
            if (!_config.WeeklyResetEnabled) return;
            if (DateTime.Now.DayOfWeek != _config.WeeklyResetDay) return;
            if (_config.LastWeeklyReset.Date == DateTime.Today.Date) return;

            var weekList = NexusManager.AccumulatedScores.WeekScores.OrderByDescending(x => x.Value).ToList();
            if (_config.Show_WeeklyResults)
            {
                AnnouncePeriodResults("Weekly", weekList, DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Green);
                RewardService.ExecutePeriodRewards(_config.WeeklyRankRewards.ToList(), _config.WeeklyThresholdRewards.ToList(), weekList);
            }

            _eventData.WeekEvents.Clear();
            NexusManager.AccumulatedScores.WeekScores.Clear();
            NexusManager.BroadcastWipe(WipePeriod.Week);
            _config.LastWeeklyReset = DateTime.Now;
        }

        private void CheckMonthlyReset()
        {
            if (!_config.MonthlyResetEnabled) return;
            var resetDay = Math.Max(1, Math.Min(28, _config.MonthlyResetDay));
            if (DateTime.Now.Day != resetDay) return;
            if (_config.LastMonthlyReset.Date.Month == DateTime.Now.Month && _config.LastMonthlyReset.Date.Year == DateTime.Now.Year) return;

            var monthList = NexusManager.AccumulatedScores.MonthScores.OrderByDescending(x => x.Value).ToList();
            if (_config.Show_MonthlyResults)
            {
                AnnouncePeriodResults("Monthly", monthList, DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Silver);
                RewardService.ExecutePeriodRewards(_config.MonthlyRankRewards.ToList(), _config.MonthlyThresholdRewards.ToList(), monthList);
            }

            _eventData.MonthEvents.Clear();
            NexusManager.AccumulatedScores.MonthScores.Clear();
            NexusManager.BroadcastWipe(WipePeriod.Month);
            _config.LastMonthlyReset = DateTime.Now;
        }

        private void CheckYearlyReset()
        {
            if (!_config.YearlyResetEnabled) return;
            var resetMonth = Math.Max(1, Math.Min(12, _config.YearlyResetMonth));
            var resetDay = Math.Max(1, Math.Min(28, _config.YearlyResetDay));
            if (DateTime.Now.Month != resetMonth || DateTime.Now.Day != resetDay) return;
            if (_config.LastYearlyReset.Date.Year == DateTime.Now.Year) return;

            var yearList = NexusManager.AccumulatedScores.YearScores.OrderByDescending(x => x.Value).ToList();
            if (_config.Show_YearlyResults)
            {
                AnnouncePeriodResults("Yearly", yearList, DrawingColor.Gold, DrawingColor.Silver, DrawingColor.SandyBrown, DrawingColor.Green);
                RewardService.ExecutePeriodRewards(_config.YearlyRankRewards.ToList(), _config.YearlyThresholdRewards.ToList(), yearList);
            }

            _eventData.YearEvents.Clear();
            NexusManager.AccumulatedScores.YearScores.Clear();
            NexusManager.BroadcastWipe(WipePeriod.Year);
            _config.LastYearlyReset = DateTime.Now;
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
