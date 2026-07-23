using System;
using System.Collections.Generic;
using Torch;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin
{
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
        public int PointsForPrizeThreshold { get; set; }
        public bool TriggerOnEveryCap { get; set; }
        public List<LiveCommandReward> CommandRewards { get; set; } = new();
    }

    public sealed class RankRewardEntry
    {
        public int Rank { get; set; } = 1;
        public List<CommandRewardEntry> Commands { get; set; } = new();
    }

    public sealed class ThresholdRewardEntry
    {
        public int MinPoints { get; set; }
        public List<CommandRewardEntry> Commands { get; set; } = new();
    }

    public sealed class CommandRewardEntry
    {
        public string CommandText { get; set; } = "";
        public bool PerFactionMember { get; set; }
        public bool OnlyOnlineMembers { get; set; }
    }

    public sealed class SenX_KOTH_PluginConfig : ViewModel
    {
        public bool WebHookEnabled { get => field; set => SetValue(ref field, value); }
        public string WebHookUrl { get => field; set => SetValue(ref field, value); } = "";
        public string MessagePrefix { get => field; set => SetValue(ref field, value); } = "\u27DC \u27DC \u27DC";
        public string Color { get => field; set => SetValue(ref field, value); } = "";
        public bool EmbedEnabled { get => field; set => SetValue(ref field, value); }
        public string EmbedTitle { get => field; set => SetValue(ref field, value); } = "Notice";
        public string EmbedImageUrl { get => field; set => SetValue(ref field, value); } = "";

        public DateTime LastWeeklyReset { get => field; set => SetValue(ref field, value); } = DateTime.MinValue;
        public DateTime LastMonthlyReset { get => field; set => SetValue(ref field, value); } = DateTime.MinValue;
        public DateTime LastYearlyReset { get => field; set => SetValue(ref field, value); } = DateTime.MinValue;

        public bool Show_AttackMessages { get => field; set => SetValue(ref field, value); } = true;
        public bool Show_WeeklyResults { get => field; set => SetValue(ref field, value); } = true;
        public bool Show_MonthlyResults { get => field; set => SetValue(ref field, value); } = true;
        public bool Show_YearlyResults { get => field; set => SetValue(ref field, value); } = true;

        public string CustomMessage { get => field; set => SetValue(ref field, value); } = "";
        public bool CustomMessageEnable { get => field; set => SetValue(ref field, value); }
        public string CustomTitle { get => field; set => SetValue(ref field, value); } = "";
        public bool CustomTitleEnable { get => field; set => SetValue(ref field, value); }

        public DayOfWeek WeeklyResetDay { get => field; set => SetValue(ref field, value); } = DayOfWeek.Monday;
        public bool WeeklyResetEnabled { get => field; set => SetValue(ref field, value); } = true;
        public int MonthlyResetDay { get => field; set => SetValue(ref field, value); } = 1;
        public bool MonthlyResetEnabled { get => field; set => SetValue(ref field, value); } = true;
        public int YearlyResetMonth { get => field; set => SetValue(ref field, value); } = 1;
        public int YearlyResetDay { get => field; set => SetValue(ref field, value); } = 1;
        public bool YearlyResetEnabled { get => field; set => SetValue(ref field, value); } = true;

        public bool NexusEnabled { get => field; set => SetValue(ref field, value); }

        public bool DiscordBotEnabled { get => field; set => SetValue(ref field, value); }
        public string DiscordBotToken { get => field; set => SetValue(ref field, value); } = "";
        public ulong RewardChannelId { get => field; set => SetValue(ref field, value); }
        public ulong LiveScoreboardChannelId { get => field; set => SetValue(ref field, value); }
        public bool SelfManagedChannelsEnabled { get => field; set => SetValue(ref field, value); }
        public ulong KoTHCategoryId { get => field; set => SetValue(ref field, value); }
        public ulong DiscordGuildId { get => field; set => SetValue(ref field, value); }
        public ulong LiveScoreboardMessageId { get => field; set => SetValue(ref field, value); }

        public string EnterAlert_MessageTemplate { get => field; set => SetValue(ref field, value); }
            = "{player} from [{factionTag}] entered {zoneName}";

        public ObservableConcurrentUiSafeCollection<ZoneRewardConfig> ZoneRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<RankRewardEntry> WeeklyRankRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<RankRewardEntry> MonthlyRankRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<RankRewardEntry> YearlyRankRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<ThresholdRewardEntry> WeeklyThresholdRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<ThresholdRewardEntry> MonthlyThresholdRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<ThresholdRewardEntry> YearlyThresholdRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public bool RaffleEnabled { get => field; set => SetValue(ref field, value); }
        public RafflePeriod RafflePeriod { get => field; set => SetValue(ref field, value); } = RafflePeriod.Weekly;
        public DayOfWeek RaffleDayOfWeek { get => field; set => SetValue(ref field, value); } = DayOfWeek.Saturday;
        public int RaffleDayOfMonth { get => field; set => SetValue(ref field, value); } = 1;
        public int RaffleHour { get => field; set => SetValue(ref field, value); } = 20;
        public int RaffleMinute { get => field; set => SetValue(ref field, value); }
        public int TicketCost { get => field; set => SetValue(ref field, value); } = 10;

        public ObservableConcurrentUiSafeCollection<CommandRewardEntry> RaffleFirstRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<CommandRewardEntry> RaffleSecondRewards
            { get => field; set => SetValue(ref field, value); } = new();

        public ObservableConcurrentUiSafeCollection<CommandRewardEntry> RaffleThirdRewards
            { get => field; set => SetValue(ref field, value); } = new();
    }
}
