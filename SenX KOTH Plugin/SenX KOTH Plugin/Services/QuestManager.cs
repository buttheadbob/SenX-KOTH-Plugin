using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Messages;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace SenX_KOTH_Plugin.Services
{
    internal sealed class QuestManager
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => QuestManager");

        private const ushort RICH_HUD_CHANNEL = 43521;

        private readonly ObservableConcurrentHashSet<IKothEvent> _events;
        private readonly Dictionary<ZonePointEvent, HashSet<ulong>> _richHudTracked = new();
        private Timer? _timer;

        public QuestManager(ObservableConcurrentHashSet<IKothEvent> events)
        {
            _events = events;
        }

        public void Init()
        {
            _timer = new Timer(_ => Tick(), null, 1000, 1000);
        }

        public void Shutdown()
        {
            _timer?.Dispose();
            _timer = null;

            var clear = new QuestUpdateMessage { Clear = true };
            foreach (var (evt, steamIds) in _richHudTracked)
                foreach (var sid in steamIds)
                    SendRichHudMessage(clear, sid);

            _richHudTracked.Clear();
        }

        private void Tick()
        {
            if (SenX_KOTH_PluginMain.Instance == null) return;

            var zoneEvents = _events.OfType<ZonePointEvent>().ToList();

            var aliveEvents = new HashSet<ZonePointEvent>(zoneEvents);
            foreach (var evt in _richHudTracked.Keys.ToList())
            {
                if (!aliveEvents.Contains(evt))
                {
                    var clear = new QuestUpdateMessage { ZoneName = evt.Zone.Name, Clear = true };
                    foreach (var sid in _richHudTracked[evt])
                        SendRichHudMessage(clear, sid);
                    _richHudTracked.Remove(evt);
                }
            }

            var runningEvents = zoneEvents
                .Where(e => e.IsRunning && e.Zone.DisplayMode != QuestDisplayMode.None)
                .ToList();

            if (runningEvents.Count == 0) return;

            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var evt in runningEvents)
            {
                var cacheEntry = ZoneManager.ZoneCache.FirstOrDefault(z =>
                    string.Equals(z.ZoneName, evt.Zone.Name, StringComparison.OrdinalIgnoreCase));
                if (cacheEntry == null) continue;

                var questDistanceSq = (double)evt.Zone.QuestDistance * evt.Zone.QuestDistance;
                var captureRadiusSq = (double)(cacheEntry.Radius * 0.8f);
                captureRadiusSq *= captureRadiusSq;

                var insideFactionCounts = new Dictionary<long, int>();
                var outsideFactionCounts = new Dictionary<long, int>();

                if (evt.Zone.ShowEnemiesOutside)
                {
                    try
                    {
                        var sphere = new BoundingSphereD(cacheEntry.Position, evt.Zone.QuestDistance);
                        var entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                        foreach (var ent in entities)
                        {
                            if (ent.Closed || ent.MarkedForClose) continue;
                            if (ent is MySafeZone || ent is MyPlanet || ent is MyVoxelBase) continue;

                            var distSq = Vector3D.DistanceSquared(ent.PositionComp.GetPosition(), cacheEntry.Position);
                            bool inside = distSq <= captureRadiusSq;

                            long factionId = 0;
                            if (ent is MyCharacter character)
                            {
                                var identityId = character.GetPlayerIdentityId();
                                if (identityId == 0) continue;
                                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(identityId);
                                if (faction == null) continue;
                                factionId = faction.FactionId;
                            }
                            else if (ent is MyCubeGrid grid)
                            {
                                if (grid.Physics == null) continue;

                                var distributor = grid.GridSystems.ResourceDistributor;
                                if (distributor == null
                                    || distributor.ResourceStateByType(MyResourceDistributorComponent.ElectricityId) == MyResourceStateEnum.NoPower)
                                    continue;

                                if (grid.BigOwners == null || grid.BigOwners.Count == 0) continue;
                                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
                                if (faction == null) continue;
                                factionId = faction.FactionId;
                            }
                            else continue;

                            var dict = inside ? insideFactionCounts : outsideFactionCounts;
                            if (!dict.ContainsKey(factionId))
                                dict[factionId] = 0;
                            dict[factionId]++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "QuestManager: outside entity query failed for zone " + evt.Zone.Name);
                    }
                }

                var factionPlayers = new Dictionary<long, List<IMyPlayer>>();
                foreach (var p in players)
                {
                    if (Vector3D.DistanceSquared(p.GetPosition(), cacheEntry.Position) > questDistanceSq)
                        continue;

                    var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(p.IdentityId);
                    long factionId = faction?.FactionId ?? 0;

                    if (!factionPlayers.ContainsKey(factionId))
                        factionPlayers[factionId] = new List<IMyPlayer>();
                    factionPlayers[factionId].Add(p);
                }

                foreach (var (factionId, factionMembers) in factionPlayers)
                {
                    long enemiesInside = 0;
                    long enemiesOutside = 0;

                    foreach (var (fid, count) in insideFactionCounts)
                        if (fid != factionId) enemiesInside += count;

                    foreach (var (fid, count) in outsideFactionCounts)
                        if (fid != factionId) enemiesOutside += count;

                    var lines = BuildQuestLines(evt, enemiesInside, enemiesOutside);

                    if (evt.Zone.DisplayMode == QuestDisplayMode.Notifications)
                    {
                        foreach (var p in factionMembers)
                            foreach (var line in lines)
                                MyVisualScriptLogicProvider.ShowNotification(line, 1000, "White", p.IdentityId);
                    }
                    else if (evt.Zone.DisplayMode == QuestDisplayMode.RichHUD)
                    {
                        if (!_richHudTracked.TryGetValue(evt, out var tracked))
                        {
                            tracked = new HashSet<ulong>();
                            _richHudTracked[evt] = tracked;
                        }

                        var steamIds = factionMembers.Select(p => p.SteamUserId).ToHashSet();

                        var msg = new QuestUpdateMessage
                        {
                            ZoneName = evt.Zone.Name,
                            Lines = lines,
                            ZoneX = cacheEntry.Position.X,
                            ZoneY = cacheEntry.Position.Y,
                            ZoneZ = cacheEntry.Position.Z,
                            QuestDistance = evt.Zone.QuestDistance,
                            Timestamp = DateTime.UtcNow.Ticks,
                            EvictionPhase = (int)evt.EvictionState,
                            EvictionTimeRemaining = evt.EvictionTimeRemaining,
                            Clear = false
                        };

                        foreach (var sid in steamIds)
                        {
                            SendRichHudMessage(msg, sid);
                            tracked.Add(sid);
                        }
                    }
                }

                if (evt.Zone.DisplayMode == QuestDisplayMode.RichHUD)
                {
                    if (_richHudTracked.TryGetValue(evt, out var tracked))
                    {
                        var allSteamIds = factionPlayers.Values.SelectMany(x => x).Select(p => p.SteamUserId).ToHashSet();
                        var left = tracked.Where(s => !allSteamIds.Contains(s)).ToList();
                        if (left.Count > 0)
                        {
                            var clear = new QuestUpdateMessage { ZoneName = evt.Zone.Name, Clear = true };
                            foreach (var sid in left)
                                SendRichHudMessage(clear, sid);
                        }

                        tracked.Clear();
                        foreach (var s in allSteamIds)
                            tracked.Add(s);
                    }
                }
            }
        }

        private static void SendRichHudMessage(QuestUpdateMessage msg, ulong steamId)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(msg);
            MyAPIGateway.Multiplayer.SendMessageTo(RICH_HUD_CHANNEL, bytes, steamId);
        }

        private static List<string> BuildQuestLines(ZonePointEvent evt, long enemiesInside, long enemiesOutside)
        {
            var lines = new List<string>();
            var pct = evt.Zone.CapturePointsNeeded > 0
                ? evt.CaptureProgress * 100 / evt.Zone.CapturePointsNeeded
                : 0;

            switch (evt.State)
            {
                case CaptureState.Neutral:
                    lines.Add("Neutral");
                    break;

                case CaptureState.Capturing:
                    lines.Add("Capturing [" + pct + "% by " + evt.CaptureFactionName + "]");
                    lines.Add("Time Remaining [" + ComputeTimeRemaining(evt, true) + "]");
                    break;

                case CaptureState.Contested:
                    lines.Add("Contested [" + pct + "% by " + evt.CaptureFactionName + "]");
                    lines.Add("Enemies Inside [" + enemiesInside + "]");
                    break;

                case CaptureState.Decaying:
                    lines.Add("Decay [" + pct + "% by " + evt.CaptureFactionName + "]");
                    lines.Add("Time Remaining [" + ComputeTimeRemaining(evt, false) + "]");
                    break;

                case CaptureState.Captured:
                    lines.Add("Held by [" + evt.CaptureFactionName + "]");
                    lines.Add("Points Earned [" + evt.CapturePointsEarned + "]");
                    break;
            }

            if (evt.Zone.ShowEnemiesOutside && enemiesOutside > 0)
                lines.Add("Enemies Outside [" + enemiesOutside + "]");

            return lines;
        }

        private static string ComputeTimeRemaining(ZonePointEvent evt, bool capturing)
        {
            if (evt.AutoDecayActive)
            {
                return FormatSeconds(evt.AutoDecayTimeRemaining);
            }

            if (capturing)
            {
                int gainPerTick = ((int)evt.SuitCount * evt.Zone.PointsPerSuit)
                                + ((int)evt.GridCount * evt.Zone.PointsPerGrid);
                if (gainPerTick <= 0) return "--";
                int remaining = evt.Zone.CapturePointsNeeded - evt.CaptureProgress;
                if (remaining <= 0) return "0s";
                int ticksNeeded = remaining / gainPerTick + (remaining % gainPerTick > 0 ? 1 : 0);
                return FormatSeconds(ticksNeeded * evt.Zone.CapturePointIntervalSeconds);
            }
            else
            {
                int lossPerTick = ((int)evt.EnemySuitCount * evt.Zone.PointsPerSuit)
                                + ((int)evt.EnemyGridCount * evt.Zone.PointsPerGrid);
                if (lossPerTick <= 0) return "--";
                int remaining = evt.CaptureProgress;
                if (remaining <= 0) return "0s";
                int ticksNeeded = remaining / lossPerTick + (remaining % lossPerTick > 0 ? 1 : 0);
                return FormatSeconds(ticksNeeded * evt.Zone.CapturePointIntervalSeconds);
            }
        }

        private static string FormatSeconds(int seconds)
        {
            if (seconds < 60) return seconds + "s";
            int minutes = seconds / 60;
            int secs = seconds % 60;
            if (secs == 0) return minutes + "m";
            return minutes + "m " + secs + "s";
        }
    }
}
