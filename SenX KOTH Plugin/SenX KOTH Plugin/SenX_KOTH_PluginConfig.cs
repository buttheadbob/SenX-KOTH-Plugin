using System;
using System.Collections.Generic;
using Torch;
using SenX_KOTH_Plugin.Utils;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SenX_KOTH_Plugin
{
    public class SenX_KOTH_PluginConfig : ViewModel
    {
        private bool _WebHookEnabled = false;
        public bool WebHookEnabled { get => _WebHookEnabled; set => SetValue(ref _WebHookEnabled, value); }

        private string _WebHookUrl = "https://discord.com/api/webhooks/1051944599519776778/fqQIThVIISBVbQuXQ9Qm8Gm8uX79-WqVaXXUMfpgZjPL9EFuRx9pMh6Mt8R0yWXX-hZG";
        public string WebHookUrl { get => _WebHookUrl; set => SetValue(ref _WebHookUrl, value); }

        private string _MessegePrefix = "➜ ➜ ➜";
        public string MessegePrefix { get => _MessegePrefix; set => SetValue(ref _MessegePrefix, value);}

        private string _Color = "";
        public string Color { get => _Color; set=> SetValue(ref _Color, value); }

        private bool _EmbedEnabled = false;
        public bool EmbedEnabled { get => _EmbedEnabled; set=> SetValue(ref _EmbedEnabled, value);}

        private string _EmbedTitle = "Notice";
        public string EmbedTitle { get => _EmbedTitle; set => SetValue(ref _EmbedTitle, value); }

        private string _EmbedPic = "";
        public string EmbedPic { get => _EmbedPic; set => SetValue(ref _EmbedPic, value); }

        private DateTime _LastWeeklyReset = DateTime.MinValue;
        public DateTime LastWeeklyReset { get => _LastWeeklyReset; set => SetValue(ref _LastWeeklyReset, value); }

        private DateTime _LastMonthlyReset = DateTime.MinValue;
        public DateTime LastMonthlyReset { get => _LastMonthlyReset; set => SetValue(ref _LastMonthlyReset, value); }

        private List<ScoreData> _YearlyScoreRecord = new List<ScoreData>();
        public List<ScoreData> YearlyScoreRecord { get => _YearlyScoreRecord; set => SetValue(ref _YearlyScoreRecord, value); }

        private List<ScoreData> _MonthScoreRecord = new List<ScoreData>();
        public List<ScoreData> MonthScoreRecord { get => _MonthScoreRecord; set => SetValue(ref _MonthScoreRecord, value); }

        private List<ScoreData> _WeekScoreData = new List<ScoreData>();
        public List<ScoreData> WeekScoreData { get => _WeekScoreData; set => SetValue(ref _WeekScoreData, value); }

        private bool _Show_AttackMessages = true;
        public bool Show_AttackMessages { get => _Show_AttackMessages; set => SetValue(ref _Show_AttackMessages, value); }

        private bool _Show_WeeklyResults = true;
        public bool Show_WeeklyResults { get => _Show_WeeklyResults; set => SetValue(ref _Show_WeeklyResults, value); }

        private bool _Show_MonthlyResults = true;
        public bool Show_MonthlyResults { get => _Show_MonthlyResults; set => SetValue(ref _Show_MonthlyResults, value); }

        private bool _Show_YearlyResults = true;
        public bool Show_YearlyResults { get => _Show_YearlyResults; set => SetValue(ref _Show_YearlyResults, value); }

        private string _CustomMessege = "";
        public string CustomMessege { get => _CustomMessege; set=> SetValue(ref _CustomMessege, value);}

        private DayOfReset _ResetDay = 0;
        public DayOfReset ResetDay { get => _ResetDay; set => SetValue(ref _ResetDay, value);}
    }
}
