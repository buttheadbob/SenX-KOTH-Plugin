using System.Collections.Generic;
using SenX_KOTH_Plugin.Utils;
using Torch;

namespace SenX_KOTH_Plugin.Models
{
    internal sealed class EventData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<PointEarned> WeekEvents { get => field; set => SetValue(ref field, value); } = new();
        public ObservableConcurrentUiSafeCollection<PointEarned> MonthEvents { get => field; set => SetValue(ref field, value); } = new();
        public ObservableConcurrentUiSafeCollection<PointEarned> YearEvents { get => field; set => SetValue(ref field, value); } = new();
    }

    internal sealed class BanksData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<FactionBankEntry> Banks { get => field; set => SetValue(ref field, value); } = new();
    }

    internal sealed class ZoneListData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<KothZone> Zones { get => field; set => SetValue(ref field, value); } = new();
    }
}
