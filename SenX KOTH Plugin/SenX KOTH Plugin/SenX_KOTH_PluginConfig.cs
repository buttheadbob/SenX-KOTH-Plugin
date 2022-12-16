using System;
using System.Collections.Generic;
using Torch;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin
{
    public class SenX_KOTH_PluginConfig : ViewModel
    {
        private bool _WebHookEnabled = false;
        public bool WebHookEnabled { get => _WebHookEnabled; set => SetValue(ref _WebHookEnabled, value); }

        private string _WebHookUrl = "https://discord.com/api/webhooks/1051944599519776778/fqQIThVIISBVbQuXQ9Qm8Gm8uX79-WqVaXXUMfpgZjPL9EFuRx9pMh6Mt8R0yWXX-hZG";
        public string WebHookUrl { get => _WebHookUrl; set => SetValue(ref _WebHookUrl, value); }

        private string _MessagePrefix = "➜ ➜ ➜";
        public string MessagePrefix { get => _MessagePrefix; set => SetValue(ref _MessagePrefix, value);}

        private string _Color = "";
        public string Color { get => _Color; set=> SetValue(ref _Color, value); }

        private bool _EmbedEnabled  = false;
        public bool EmbedEnabled { get => _EmbedEnabled; set=> SetValue(ref _EmbedEnabled, value);}

        private List<ScoreData> _YearlyScoreRecord = new List<ScoreData>();
        public List<ScoreData> YearlyScoreRecord { get => _YearlyScoreRecord; set => SetValue(ref _YearlyScoreRecord, value); }

        private List<ScoreData> _MonthScoreRecord = new List<ScoreData>();
        public List<ScoreData> MonthScoreRecord { get => _MonthScoreRecord; set => SetValue(ref _MonthScoreRecord, value); }

        private List<ScoreData> _WeekScoreData = new List<ScoreData>();
        public List<ScoreData> WeekScoreData { get => _WeekScoreData; set => SetValue(ref _WeekScoreData, value); }

    }
}
