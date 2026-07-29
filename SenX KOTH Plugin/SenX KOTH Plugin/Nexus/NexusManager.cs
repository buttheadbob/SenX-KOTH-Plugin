using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Timers;
using NLog;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;
using VRage.Game.ModAPI;

namespace SenX_KOTH_Plugin.Nexus
{
    internal static class NexusManager
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => NexusManager");

        public static readonly ScoreFile AccumulatedScores = new();
        private static EventData? _eventData;
        private static Timer? _verificationTimer;

        private static long _nextEventId;
        public const ushort NexusChannelId = 50672;

        public static void Initialize(SenX_KOTH_PluginMain plugin, EventData eventData)
        {
            _eventData = eventData;
            RebuildAccumulatedScores();

            var config = plugin.Config;
            if (config?.NexusEnabled == true && SenX_KOTH_PluginMain.NexusGlobalAPI is { Enabled: true })
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NexusChannelId, HandleNexusMessage);
                KoTHLog.Info(Log,"Nexus message handler registered on channel " + NexusChannelId);
            }

            _verificationTimer = new Timer(900000);
            _verificationTimer.Elapsed += (_, _) => BroadcastVerification();
            _verificationTimer.Start();
        }

        public static void Shutdown()
        {
            _verificationTimer?.Dispose();
            _verificationTimer = null;

            if (SenX_KOTH_PluginMain.NexusGlobalAPI is { Enabled: true })
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NexusChannelId, HandleNexusMessage);
        }

        public static long GenerateEventId()
        {
            byte serverId = SenX_KOTH_PluginMain.NexusGlobalAPI is { Enabled: true } api
                ? api.CurrentServerID : (byte)0;
            return ((long)serverId << 56) | (Interlocked.Increment(ref _nextEventId) & 0x00FFFFFFFFFFFFFF);
        }

        public static void AddPointEvent(EventData data, PointEarned point)
        {
            if (!data.WeekEvents.Any(e => e.FromServerID == point.FromServerID && e.EventId == point.EventId))
                data.WeekEvents.Add(point);

            UpdateAccumulatedScores(point);
            RewardService.CheckLiveRewards(point);
        }

        private static void UpdateAccumulatedScores(PointEarned point)
        {
            if (point.LastWipe.HasValue) return;
            AddToScoreList(AccumulatedScores.WeekScores, point.FactionName, (ulong)point.Points);
        }

        private static void AddToScoreList(List<KeyValuePair<string, ulong>> list, string factionName, ulong points)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == factionName)
                {
                    list[i] = new KeyValuePair<string, ulong>(factionName, list[i].Value + points);
                    return;
                }
            }
            list.Add(new KeyValuePair<string, ulong>(factionName, points));
        }

        public static void BroadcastPointDelta(PointEarned point)
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;

            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;

            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(point);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to broadcast point delta."); }
        }

        public static void BroadcastWipe(WipePeriod period)
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;

            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;

            try
            {
                var wipe = new PointEarned
                {
                    FactionName = "", FactionTag = "",
                    LastWipe = DateTime.UtcNow,
                    FromServerID = api.CurrentServerID,
                    EarnedAt = DateTime.UtcNow,
                    EventId = GenerateEventId(),
                    WipePeriod = period
                };

                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(wipe);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to broadcast wipe."); }
        }

        public static void BroadcastVerification()
        {
            if (_eventData == null) return;
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;

            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;

            try
            {
                var ver = new PointVerification
                {
                    FromServerID = api.CurrentServerID,
                    WeekEvents = _eventData.WeekEvents.ToList()
                };

                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(ver);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to broadcast verification."); }
        }

        public static void BroadcastRewardConfig()
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;

            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;

            try
            {
                var configJson = Newtonsoft.Json.JsonConvert.SerializeObject(config,
                    new Newtonsoft.Json.JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None });

                var sync = new RewardConfigSync
                {
                    FromServerID = api.CurrentServerID,
                    ConfigData = System.Text.Encoding.UTF8.GetBytes(configJson)
                };

                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(sync);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to broadcast reward config."); }
        }

        private static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamId, bool fromServer)
        {
            if (_eventData == null) return;
            try
            {
                var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
                if (api is not { Enabled: true }) return;

                var incomingMsg = MyAPIGateway.Utilities.SerializeFromBinary<NexusGlobalAPI.ModAPIMsg>(data);
                if (incomingMsg?.msgData == null) return;
                if (incomingMsg.fromServerID == api.CurrentServerID) return;

                TryHandlePointEarned(incomingMsg);
                TryHandlePointVerification(incomingMsg);
                TryHandleRewardConfigSync(incomingMsg);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Error handling Nexus message."); }
        }

        private static void TryHandlePointEarned(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var point = MyAPIGateway.Utilities.SerializeFromBinary<PointEarned>(msg.msgData);
                if (point == null) return;

                if (point.WipePeriod != WipePeriod.None)
                {
                    ApplyWipe(point);
                }
                else if (_eventData != null)
                {
                    AddNexusPoint(_eventData, point);
                }
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to deserialize point earned from Nexus message."); }
        }

        private static void AddNexusPoint(EventData data, PointEarned point)
        {
            bool exists =
                data.WeekEvents.Any(e => e.FromServerID == point.FromServerID && e.EventId == point.EventId) ||
                data.MonthEvents.Any(e => e.FromServerID == point.FromServerID && e.EventId == point.EventId) ||
                data.YearEvents.Any(e => e.FromServerID == point.FromServerID && e.EventId == point.EventId);

            if (exists) return;

            if (IsCurrentWeek(point.EarnedAt))
                data.WeekEvents.Add(point);
            else if (IsCurrentMonth(point.EarnedAt))
                data.MonthEvents.Add(point);
            else if (IsCurrentYear(point.EarnedAt))
                data.YearEvents.Add(point);

            UpdateAccumulatedScores(point);
        }

        private static bool IsCurrentWeek(DateTime date)
        {
            var now = DateTime.Now;
            return GetIsoWeek(date) == GetIsoWeek(now) && date.Year == now.Year;
        }

        private static bool IsCurrentMonth(DateTime date)
        {
            var now = DateTime.Now;
            return date.Month == now.Month && date.Year == now.Year;
        }

        private static bool IsCurrentYear(DateTime date)
        {
            return date.Year == DateTime.Now.Year;
        }

        internal static int GetIsoWeek(DateTime d)
        {
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private static void TryHandlePointVerification(NexusGlobalAPI.ModAPIMsg msg)
        {
            if (_eventData == null) return;
            try
            {
                var ver = MyAPIGateway.Utilities.SerializeFromBinary<PointVerification>(msg.msgData);
                if (ver == null) return;
                ApplyVerification(ver);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to deserialize point verification from Nexus message."); }
        }

        private static void TryHandleRewardConfigSync(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var sync = MyAPIGateway.Utilities.SerializeFromBinary<RewardConfigSync>(msg.msgData);
                if (sync?.ConfigData == null) return;

                var config = SenX_KOTH_PluginMain.Instance?.Config;
                if (config == null) return;

                var configJson = System.Text.Encoding.UTF8.GetString(sync.ConfigData);
                var syncedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<SenX_KOTH_PluginConfig>(configJson);
                if (syncedConfig == null) return;

                config.ZoneRewards.Clear();
                foreach (var z in syncedConfig.ZoneRewards) config.ZoneRewards.Add(z);

                config.WeeklyRankRewards.Clear();
                foreach (var r in syncedConfig.WeeklyRankRewards) config.WeeklyRankRewards.Add(r);

                config.MonthlyRankRewards.Clear();
                foreach (var r in syncedConfig.MonthlyRankRewards) config.MonthlyRankRewards.Add(r);

                config.YearlyRankRewards.Clear();
                foreach (var r in syncedConfig.YearlyRankRewards) config.YearlyRankRewards.Add(r);

                config.WeeklyThresholdRewards.Clear();
                foreach (var t in syncedConfig.WeeklyThresholdRewards) config.WeeklyThresholdRewards.Add(t);

                config.MonthlyThresholdRewards.Clear();
                foreach (var t in syncedConfig.MonthlyThresholdRewards) config.MonthlyThresholdRewards.Add(t);

                config.YearlyThresholdRewards.Clear();
                foreach (var t in syncedConfig.YearlyThresholdRewards) config.YearlyThresholdRewards.Add(t);

                SenX_KOTH_PluginMain.ConfigPersist?.Save();
                KoTHLog.Info(Log,"Reward config synced from server " + sync.FromServerID);
            }
            catch (Exception ex) { KoTHLog.Error(Log,ex, "Failed to sync reward config from Nexus message."); }
        }

        private static void ApplyWipe(PointEarned wipe)
        {
            if (_eventData == null || !wipe.LastWipe.HasValue) return;

            switch (wipe.WipePeriod)
            {
                case WipePeriod.Week:
                    _eventData.WeekEvents.RemoveAll(e => e.FromServerID == wipe.FromServerID && e.EarnedAt < wipe.LastWipe!.Value);
                    _eventData.WeekEvents.Add(wipe);
                    break;
                case WipePeriod.Month:
                    _eventData.MonthEvents.RemoveAll(e => e.FromServerID == wipe.FromServerID && e.EarnedAt < wipe.LastWipe!.Value);
                    _eventData.MonthEvents.Add(wipe);
                    break;
                case WipePeriod.Year:
                    _eventData.YearEvents.RemoveAll(e => e.FromServerID == wipe.FromServerID && e.EarnedAt < wipe.LastWipe!.Value);
                    _eventData.YearEvents.Add(wipe);
                    break;
            }

            RebuildAccumulatedScores();
        }

        private static void ApplyVerification(PointVerification ver)
        {
            if (_eventData == null) return;

            _eventData.WeekEvents.RemoveAll(e => e.FromServerID == ver.FromServerID);
            _eventData.WeekEvents.AddRange(ver.WeekEvents);

            RebuildAccumulatedScores();
        }

        private static void RebuildAccumulatedScores()
        {
            if (_eventData == null) return;

            AccumulatedScores.WeekScores.Clear();
            AccumulatedScores.MonthScores.Clear();
            AccumulatedScores.YearScores.Clear();

            foreach (var p in _eventData.WeekEvents.ToList())
                if (!p.LastWipe.HasValue)
                    AddToScoreList(AccumulatedScores.WeekScores, p.FactionName, (ulong)p.Points);

            foreach (var p in _eventData.MonthEvents.ToList())
                if (!p.LastWipe.HasValue)
                    AddToScoreList(AccumulatedScores.MonthScores, p.FactionName, (ulong)p.Points);

            foreach (var p in _eventData.YearEvents.ToList())
                if (!p.LastWipe.HasValue)
                    AddToScoreList(AccumulatedScores.YearScores, p.FactionName, (ulong)p.Points);
        }
    }
}
