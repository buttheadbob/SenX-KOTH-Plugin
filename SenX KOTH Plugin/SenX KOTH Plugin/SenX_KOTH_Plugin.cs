using NLog;
using System;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Nexus;
using SenX_KOTH_Plugin.Services;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin
{
    public sealed class SenX_KOTH_PluginMain : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => Main");

        internal static ObservableConcurrentHashSet<IKothEvent> Events = new();

        private const string DataFolderName = "Senx_Koth";

        private static string DataPath
        {
            get
            {
                var bp = Instance?.StoragePath ?? ".";
                var p = Path.Combine(bp, DataFolderName);
                Directory.CreateDirectory(p);
                return p;
            }
        }

        private SenX_KOTH_PluginControl? _control;
        private QuestManager? _questManager;
        public UserControl GetControl() => _control ??= new SenX_KOTH_PluginControl();

        public SenX_KOTH_PluginConfig? Config { get; private set; }
        public static SenX_KOTH_PluginMain? Instance { get; private set; }
        public static NexusGlobalAPI? NexusGlobalAPI { get; private set; }

        internal static JsonPersistent<SenX_KOTH_PluginConfig>? ConfigPersist { get; private set; }
        internal static JsonPersistent<ScoreFile>? ScorePersist { get; private set; }
        internal static JsonPersistent<EventData>? EventPersist { get; private set; }
        internal static JsonPersistent<BanksData>? BankPersist { get; private set; }
        internal static JsonPersistent<RaffleTicketsData>? RafflePersist { get; private set; }
        internal static JsonPersistent<ZoneListData>? ZonePersist { get; private set; }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            ObservableConcurrentUiSafeCollectionStatic.SetSynchronizationContext(
                System.Threading.SynchronizationContext.Current!);

            SetupConfig();

            ScorePersist = JsonPersistent<ScoreFile>.Load(Path.Combine(DataPath, "ScoreData.json"));
            KoTHLog.Info(Log,"ScoreData loaded — Week entries: " + ScorePersist.Data.WeekScores.Count + ", Month: " + ScorePersist.Data.MonthScores.Count + ", Year: " + ScorePersist.Data.YearScores.Count);

            EventPersist = JsonPersistent<EventData>.Load(Path.Combine(DataPath, "EventData.json"));
            KoTHLog.Info(Log,"EventData loaded — WeekEvents: " + EventPersist.Data.WeekEvents.Count + ", MonthEvents: " + EventPersist.Data.MonthEvents.Count + ", YearEvents: " + EventPersist.Data.YearEvents.Count);
            EventPersist.WatchCollection(EventPersist.Data.WeekEvents);
            EventPersist.WatchCollection(EventPersist.Data.MonthEvents);
            EventPersist.WatchCollection(EventPersist.Data.YearEvents);

            BankPersist = JsonPersistent<BanksData>.Load(Path.Combine(DataPath, "FactionBanks.json"));
            KoTHLog.Info(Log,"FactionBanks loaded — Banks: " + BankPersist.Data.Banks.Count);
            BankPersist.WatchCollection(BankPersist.Data.Banks);

            RafflePersist = JsonPersistent<RaffleTicketsData>.Load(Path.Combine(DataPath, "RaffleTickets.json"));
            KoTHLog.Info(Log,"RaffleTickets loaded — Tickets: " + RafflePersist.Data.Tickets.Count + ", LastDraw: " + RafflePersist.Data.LastDrawDate.ToString("yyyy-MM-dd"));
            RafflePersist.WatchCollection(RafflePersist.Data.Tickets);

            ZonePersist = JsonPersistent<ZoneListData>.Load(Path.Combine(DataPath, "Zones.json"));
            KoTHLog.Info(Log,"Zones loaded — Count: " + ZonePersist.Data.Zones.Count);
            ZonePersist.WatchCollection(ZonePersist.Data.Zones);

            var config = Config;
            if (config == null)
            {
                KoTHLog.Error(Log,"Config failed to load; skipping event initialization.");
                return;
            }

            Events.Add(new ResetEvent(config, EventPersist.Data, BankPersist.Data));
            KoTHLog.Info(Log,"Registered event: ResetEvent");

            Events.Add(new RaffleEvent(config, BankPersist.Data, RafflePersist.Data));
            KoTHLog.Info(Log,"Regi60s check intervalstered event: RaffleEvent — Enabled=" + config.RaffleEnabled);

            Events.Add(new LiveScoreboardEvent(config));
            KoTHLog.Info(Log,"Registered event: LiveScoreboardEvent — DiscordBotEnabled=" + config.DiscordBotEnabled);

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
                    NexusGlobalAPI = new NexusGlobalAPI(OnNexusEnabled);
                    NexusManager.Initialize(this, EventPersist!.Data);
                    Supervisor.Init();
                    _questManager = new QuestManager(Events);
                    _questManager.Init();
                    _ = Discord.DiscordBotService.StartAsync();
                    break;

                case TorchSessionState.Unloading:
                    KoTHLog.Info(Log,"Session Unloading!");
                    _ = Discord.DiscordBotService.StopAsync();
                    _questManager?.Shutdown();
                    _questManager = null;
                    Supervisor.ShutDown();
                    NexusManager.Shutdown();
                    NexusGlobalAPI?.Unload();
                    NexusGlobalAPI = null;
                    break;
            }
        }

        private void OnNexusEnabled()
        {
            KoTHLog.Info(Log,"Nexus 3 API connected. Server ID: " + NexusGlobalAPI?.CurrentServerID);
            if (Config?.NexusEnabled == true)
                NexusManager.Initialize(this, EventPersist!.Data);
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
                Path.Combine(DataPath, "Config.json"));
            Config = ConfigPersist.Data;
        }

        public void SaveConfig() => ConfigPersist?.Save();

        public override void Dispose()
        {
            _ = Discord.DiscordBotService.StopAsync();
            Supervisor.ShutDown();
            if (Config != null)
            {
                Config.ZoneRewards.Dispose();
                Config.WeeklyRankRewards.Dispose();
                Config.MonthlyRankRewards.Dispose();
                Config.YearlyRankRewards.Dispose();
                Config.WeeklyThresholdRewards.Dispose();
                Config.MonthlyThresholdRewards.Dispose();
                Config.YearlyThresholdRewards.Dispose();
                Config.RaffleFirstRewards.Dispose();
                Config.RaffleSecondRewards.Dispose();
                Config.RaffleThirdRewards.Dispose();
            }
            EventPersist?.Data.WeekEvents.Dispose();
            EventPersist?.Data.MonthEvents.Dispose();
            EventPersist?.Data.YearEvents.Dispose();
            ConfigPersist?.Dispose();
            ScorePersist?.Dispose();
            EventPersist?.Dispose();
            BankPersist?.Dispose();
            RafflePersist?.Dispose();
            ZonePersist?.Dispose();
            base.Dispose();
        }
    }
}
