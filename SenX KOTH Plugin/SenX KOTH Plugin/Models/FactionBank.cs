using System;
using System.Collections.Generic;
using SenX_KOTH_Plugin.Utils;
using Torch;

namespace SenX_KOTH_Plugin.Models
{
    internal sealed class FactionBankEntry
    {
        public long FactionId { get; set; }
        public string FactionName { get; set; } = "";
        public string FactionTag { get; set; } = "";
        public int Points { get; set; }
    }

    internal sealed class RaffleTicketEntry
    {
        public long FactionId { get; set; }
        public string FactionName { get; set; } = "";
        public string FactionTag { get; set; } = "";
        public int Count { get; set; }
    }

    internal sealed class RaffleTicketsData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<RaffleTicketEntry> Tickets { get => field; set => SetValue(ref field, value); } = new();
        public DateTime LastDrawDate { get => field; set => SetValue(ref field, value); }
    }
}
