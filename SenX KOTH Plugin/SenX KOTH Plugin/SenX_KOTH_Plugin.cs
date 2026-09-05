using NLog;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Services;
using SenX_KOTH_Plugin.Utils;
// ReSharper disable InconsistentNaming

namespace SenX_KOTH_Plugin
{
    public sealed class SenX_KOTH_PluginMain : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => Main");

        internal static ObservableConcurrentHashSet<IKothEvent> Events = new();

        private const string DataFolderName = "Senx_Koth";

        internal static string LocalDataPath
        {
            get
            {
                var bp = Instance?.StoragePath ?? ".";
                var p = Path.Combine(bp, DataFolderName);
                Directory.CreateDirectory(p);
                return p;
            }
        }

        internal static string DataPath
        {
            get
            {
                var shared = Instance?.Config?.SharedDataPath;
                if (!string.IsNullOrEmpty(shared)) return shared!;
                return LocalDataPath;
            }
        }

        private SenX_KOTH_PluginControl? _control;
        private QuestManager? _questManager;
        public UserControl GetControl() => _control ??= new ();

        public SenX_KOTH_PluginConfig? Config { get; private set; }
        public static SenX_KOTH_PluginMain? Instance { get; private set; }

        internal static Dispatcher? UiDispatcher;

        internal static void RunOnUiThread(Action action)
        {
            var dispatcher = UiDispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.Invoke(action);
            else
                action();
        }

        internal static JsonPersistent<SenX_KOTH_PluginConfig>? ConfigPersist { get; private set; }
        internal static JsonPersistent<ZoneListData>? ZonePersist { get; private set; }

        public override void Init(ITorchBase torch)
    {
        base.Init(torch);
        Instance = this;

        SetupConfig();

var config = Config;
if (config == null)
{
    KoTHLog.Error(Log,"Config failed to load; skipping event initialization.");
    return;
}

ZonePersist = JsonPersistent<ZoneListData>.Load(Path.Combine(LocalDataPath, "Zones.json"));
    KoTHLog.Info(Log,"Zones loaded — Count: " + ZonePersist.Data.Zones.Count);
    ZonePersist.WatchCollection(ZonePersist.Data.Zones);

    Directory.CreateDirectory(DataPath);

    Events.Add(new ResetEvent(config));
    KoTHLog.Info(Log,"Registered event: ResetEvent");

    Events.Add(new RaffleEvent(config));
    KoTHLog.Info(Log,"Registered event: RaffleEvent — Enabled=" + config.RaffleEnabled);

        TorchSessionManager? sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
        if (sessionManager != null)
            sessionManager.SessionStateChanged += SessionChanged;
        else
            KoTHLog.Warn(Log,"No session manager loaded!");
    }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    KoTHLog.Info(Log,"Session Loaded! Registered events: " + Events.Count);
                    Supervisor.Init();
                    PointBuffer.Start();
                    _questManager = new QuestManager(Events);
                    _questManager.Init();
                    break;

                case TorchSessionState.Unloading:
                    KoTHLog.Info(Log,"Session Unloading!");
                    _questManager?.Shutdown();
                    _questManager = null;
                    Supervisor.ShutDown();
                    PointBuffer.Stop();
                    break;
            }
        }

        public override void Update()
        {
            foreach (IKothEvent e in Events)
            {
                if (e.IsRunning)
                    e.Update();

                e.IntegrityCheck();
            }
        }

        private void SetupConfig()
        {
            ConfigPersist = JsonPersistent<SenX_KOTH_PluginConfig>.Load(
                Path.Combine(LocalDataPath, "Config.json"));
            Config = ConfigPersist.Data;

            var p = ConfigPersist;
            var c = Config;

            p.WatchCollection(c.Webhooks);
            p.WatchCollection(c.ZoneRewards);
            p.WatchCollection(c.WeeklyRankRewards);
            p.WatchCollection(c.MonthlyRankRewards);
            p.WatchCollection(c.YearlyRankRewards);
            p.WatchCollection(c.WeeklyThresholdRewards);
            p.WatchCollection(c.MonthlyThresholdRewards);
            p.WatchCollection(c.YearlyThresholdRewards);
            p.WatchCollection(c.RaffleFirstRewards);
            p.WatchCollection(c.RaffleSecondRewards);
            p.WatchCollection(c.RaffleThirdRewards);

            void OnWebhookPropertyChanged(object? s, PropertyChangedEventArgs e)
                => p.NotifyChanged();

            foreach (var wh in c.Webhooks)
                wh.PropertyChanged += OnWebhookPropertyChanged;

            c.Webhooks.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                    foreach (WebhookEntry wh in e.NewItems)
                        wh.PropertyChanged += OnWebhookPropertyChanged;
                if (e.OldItems != null)
                    foreach (WebhookEntry wh in e.OldItems)
                        wh.PropertyChanged -= OnWebhookPropertyChanged;
            };
        }

        public void SaveConfig() => ConfigPersist?.Save();

        internal static EventData? LoadEventData()
        {
            return SharedFile.Read<EventData>(Path.Combine(LocalDataPath, "EventData.json"));
        }

        internal static ScoreFile? LoadScoreFile()
        {
            return SharedFile.Read<ScoreFile>(Path.Combine(DataPath, "ScoreData.json"));
        }

        public override void Dispose()
        {
            Supervisor.ShutDown();
            PointBuffer.Stop();
            ConfigPersist?.Dispose();
            ZonePersist?.Dispose();
            base.Dispose();
        }
    }
}
