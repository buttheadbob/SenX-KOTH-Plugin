using System;
using Torch;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin
{
    public class SenX_KOTH_PluginConfig : ViewModel
    {
        private bool _WebHookEnabled = false;
        public bool WebHookEnabled { get => _WebHookEnabled; set => SetValue(ref _WebHookEnabled, value); }

        private string _WebHookUrl = "";
        public string WebHookUrl { get => _WebHookUrl; set => SetValue(ref _WebHookUrl, value); }

        private string _MessegePrefix = "➜ ➜ ➜";
        public string MessegePrefix { get => _MessegePrefix; set => SetValue(ref _MessegePrefix, value);}

        private string _Color = "";
        public string Color { get => _Color; set=> SetValue(ref _Color, value); }

        private bool _EmbedEnabled = false;
        public bool EmbedEnabled { get => _EmbedEnabled; set=> SetValue(ref _EmbedEnabled, value);}

        private string _EmbedTitle = "Notice";
        public string EmbedTitle { get => _EmbedTitle; set => SetValue(ref _EmbedTitle, value); }

        private bool _DefaultEmbedPic = false;
        public bool DefaultEmbedPic { get => _DefaultEmbedPic; set => SetValue(ref _DefaultEmbedPic, value); }

        private string _EmbedPic = "";
        public string EmbedPic { get => _EmbedPic; set => SetValue(ref _EmbedPic, value); }

        private DateTime _LastWeeklyReset = DateTime.MinValue;
        public DateTime LastWeeklyReset { get => _LastWeeklyReset; set => SetValue(ref _LastWeeklyReset, value); }

        private DateTime _LastMonthlyReset = DateTime.MinValue;
        public DateTime LastMonthlyReset { get => _LastMonthlyReset; set => SetValue(ref _LastMonthlyReset, value); }

        private bool _Show_AttackMessages = true;
        public bool Show_AttackMessages { get => _Show_AttackMessages; set => SetValue(ref _Show_AttackMessages, value); }

        private bool _Show_WeeklyResults = true;
        public bool Show_WeeklyResults { get => _Show_WeeklyResults; set => SetValue(ref _Show_WeeklyResults, value); }

        private bool _Show_MonthlyResults = true;
        public bool Show_MonthlyResults { get => _Show_MonthlyResults; set => SetValue(ref _Show_MonthlyResults, value); }

        private bool _Show_YearlyResults = true;
        public bool Show_YearlyResults { get => _Show_YearlyResults; set => SetValue(ref _Show_YearlyResults, value); }

        private string _CustomMessege = "";
        public string CustomMessege { get => _CustomMessege; set=> SetValue(ref _CustomMessege, value); }

        private bool _CustomMessegeEnable = false;
        public bool CustomMessegeEnable { get => _CustomMessegeEnable; set => SetValue(ref _CustomMessegeEnable, value); }

        private string _CustomTitle = "";
        public string CustomTitle { get => _CustomTitle; set => SetValue(ref _CustomTitle, value); }

        private bool _CustomTitleEnable = false;  
        public bool CustomTitleEnable { get => _CustomTitleEnable; set => SetValue(ref _CustomTitleEnable, value); }

        private DayOfReset _ResetDay = DayOfReset.Monday;
        public DayOfReset ResetDay { get => _ResetDay; set => SetValue(ref _ResetDay, value);}
    }
}
