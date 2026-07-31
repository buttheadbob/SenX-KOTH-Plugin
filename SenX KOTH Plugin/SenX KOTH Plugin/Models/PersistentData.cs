using System.Collections.Generic;
using SenX_KOTH_Plugin.Utils;
using Torch;

namespace SenX_KOTH_Plugin.Models
{
    internal sealed class EventData
    {
        public List<PointEarned> WeekEvents { get; set; } = new();
        public List<PointEarned> MonthEvents { get; set; } = new();
        public List<PointEarned> YearEvents { get; set; } = new();
    }

    internal sealed class BanksData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<FactionBankEntry> Banks { get; set => SetValue(ref field, value); } = new();
    }

    internal sealed class ZoneListData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<KothZone> Zones { get; set => SetValue(ref field, value); } = new();
    }
}
