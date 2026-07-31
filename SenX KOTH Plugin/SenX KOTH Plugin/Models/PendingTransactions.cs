using SenX_KOTH_Plugin.Utils;
using Torch;

namespace SenX_KOTH_Plugin.Models
{
    internal sealed class PendingTransactionsData : ViewModel
    {
        public ObservableConcurrentUiSafeCollection<PointCreditEntry> Credits { get; set => SetValue(ref field, value); } = new();
    }
}
