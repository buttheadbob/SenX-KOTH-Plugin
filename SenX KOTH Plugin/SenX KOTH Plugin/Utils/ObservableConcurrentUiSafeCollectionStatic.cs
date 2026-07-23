using System.Threading;

namespace SenX_KOTH_Plugin.Utils;

public static class ObservableConcurrentUiSafeCollectionStatic
{
    private static SynchronizationContext? _sharedContext;

    public static void SetSynchronizationContext(SynchronizationContext context) =>
        _sharedContext = context;

    public static SynchronizationContext? SharedContext => _sharedContext;
}