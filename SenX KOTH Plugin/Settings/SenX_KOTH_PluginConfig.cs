using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Torch;

namespace SenX_KOTH_Plugin
{
    public enum WebhookEventType
    {
        Capture,
        Decay,
        Points,
        EnterAlert,
        Eviction,
        RankResult,
        RaffleWinner
    }

    public enum WipePeriod
    {
        None = -1,
        Week = 0,
        Month = 1,
        Year = 2
    }

    public enum RafflePeriod
    {
        Weekly = 0,
        Monthly = 1
    }

    public enum RewardPeriod
    {
        Weekly = 0,
        Monthly = 1,
        Yearly = 2
    }

    public sealed class LiveCommandReward
    {
        public bool Enabled { get; set; }
        public string CommandText { get; set; } = "";
        public bool PerFactionMember { get; set; }
        public bool OnlyOnlineMembers { get; set; }
    }

    public sealed class ZoneRewardConfig
    {
        public string ZoneName { get; set; } = "";
        public bool TriggerOnEveryCap { get; set; }
        public List<LiveCommandReward> CommandRewards { get; set; } = new();
    }

    public sealed class RankRewardEntry
    {
        public int Rank { get; set; } = 1;
        public List<CommandRewardEntry> Commands { get; set; } = new();

        [JsonIgnore]
        public string DisplayText => $"Rank {Rank} ({Commands.Count} commands)";
    }

    public sealed class ThresholdRewardEntry
    {
        public int MinPoints { get; set; }
        public List<CommandRewardEntry> Commands { get; set; } = new();

        [JsonIgnore]
        public string DisplayText => $"Min {MinPoints} pts ({Commands.Count} cmds)";
    }

    public sealed class CommandRewardEntry
    {
        public string CommandText { get; set; } = "";
        public bool PerFactionMember { get; set; }
        public bool OnlyOnlineMembers { get; set; }
    }

    public sealed class WebhookEntry : ViewModel
    {
        [JsonIgnore]
        public bool IsExpanded { get; set; }

        public bool Enabled { get; set => SetValue(ref field, value); } = true;
        public string Url { get; set => SetValue(ref field, value); } = "";
        public bool CaptureEvents { get; set => SetValue(ref field, value); } = true;
        public bool DecayEvents { get; set => SetValue(ref field, value); } = true;
        public bool PointsEvents { get; set => SetValue(ref field, value); } = true;
        public bool EnterAlerts { get; set => SetValue(ref field, value); } = true;
        public bool EvictionEvents { get; set => SetValue(ref field, value); } = true;
        public bool RankResults { get; set => SetValue(ref field, value); } = true;
        public bool RaffleWinners { get; set => SetValue(ref field, value); } = true;
        public bool AllZones { get; set => SetValue(ref field, value); } = true;
        public string ZoneFilter { get; set => SetValue(ref field, value); } = "";

        public bool AcceptsEvent(WebhookEventType type) => type switch
        {
            WebhookEventType.Capture => CaptureEvents,
            WebhookEventType.Decay => DecayEvents,
            WebhookEventType.Points => PointsEvents,
            WebhookEventType.EnterAlert => EnterAlerts,
            WebhookEventType.Eviction => EvictionEvents,
            WebhookEventType.RankResult => RankResults,
            WebhookEventType.RaffleWinner => RaffleWinners,
            _ => false
        };
    }

    public sealed class SenX_KOTH_PluginConfig : ViewModel
    {
        public string MessagePrefix { get; set => SetValue(ref field, value); } = "\u27DC \u27DC \u27DC";
        public string Color { get; set => SetValue(ref field, value); } = "";
        public bool EmbedEnabled { get; set => SetValue(ref field, value); }
        public string EmbedTitle { get; set => SetValue(ref field, value); } = "Notice";
        public string EmbedImageUrl { get; set => SetValue(ref field, value); } = "";

        public DateTime LastWeeklyReset { get; set => SetValue(ref field, value); } = DateTime.MinValue;
        public DateTime LastMonthlyReset { get; set => SetValue(ref field, value); } = DateTime.MinValue;
        public DateTime LastYearlyReset { get; set => SetValue(ref field, value); } = DateTime.MinValue;

        public int LastWeeklyProcessWeek { get; set => SetValue(ref field, value); }
        public int LastWeeklyProcessYear { get; set => SetValue(ref field, value); }
        public int LastMonthlyProcessMonth { get; set => SetValue(ref field, value); }
        public int LastMonthlyProcessYear { get; set => SetValue(ref field, value); }
        public int LastYearlyProcessYear { get; set => SetValue(ref field, value); }

        public bool Show_WeeklyResults { get; set => SetValue(ref field, value); } = true;
        public bool Show_MonthlyResults { get; set => SetValue(ref field, value); } = true;
        public bool Show_YearlyResults { get; set => SetValue(ref field, value); } = true;

        public string CustomMessage { get; set => SetValue(ref field, value); } = "";
        public bool CustomMessageEnable { get; set => SetValue(ref field, value); }
        public string CustomTitle { get; set => SetValue(ref field, value); } = "";
        public bool CustomTitleEnable { get; set => SetValue(ref field, value); }

        public bool WeeklyRewardsEnabled { get; set => SetValue(ref field, value); } = true;
        public bool MonthlyRewardsEnabled { get; set => SetValue(ref field, value); } = true;
        public bool YearlyRewardsEnabled { get; set => SetValue(ref field, value); } = true;

        public string SharedDataPath { get; set => SetValue(ref field, value); } = "";

        public bool DebugLoggingEnabled { get; set => SetValue(ref field, value); }

        public string EnterAlert_MessageTemplate { get; set => SetValue(ref field, value); }
            = "{player} from [{factionTag}] entered {zoneName}";

        public ObservableCollection<WebhookEntry> Webhooks
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<ZoneRewardConfig> ZoneRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<RankRewardEntry> WeeklyRankRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<RankRewardEntry> MonthlyRankRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<RankRewardEntry> YearlyRankRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<ThresholdRewardEntry> WeeklyThresholdRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<ThresholdRewardEntry> MonthlyThresholdRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<ThresholdRewardEntry> YearlyThresholdRewards
            { get; set => SetValue(ref field, value); } = [];

        public bool RaffleEnabled { get; set => SetValue(ref field, value); }
        public RafflePeriod RafflePeriod
        {
            get;
            set
            {
                SetValue(ref field, value);
                OnPropertyChanged(nameof(IsRaffleWeekly));
                OnPropertyChanged(nameof(IsRaffleMonthly));
            }
        } = RafflePeriod.Weekly;
        public DayOfWeek RaffleDayOfWeek { get; set => SetValue(ref field, value); } = DayOfWeek.Saturday;
        public int RaffleDayOfMonth { get; set => SetValue(ref field, value); } = 1;

        public bool IsRaffleWeekly => RafflePeriod == RafflePeriod.Weekly;
        public bool IsRaffleMonthly => RafflePeriod == RafflePeriod.Monthly;
        public int RaffleHour { get; set => SetValue(ref field, value); } = 20;
        public int RaffleMinute { get; set => SetValue(ref field, value); }
        public int TicketCost { get; set => SetValue(ref field, value); } = 10;

        public ObservableCollection<CommandRewardEntry> RaffleFirstRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<CommandRewardEntry> RaffleSecondRewards
            { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<CommandRewardEntry> RaffleThirdRewards
            { get; set => SetValue(ref field, value); } = [];
    }
}
