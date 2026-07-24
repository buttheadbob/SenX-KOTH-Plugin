using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Messages;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace SenX_KOTH_Plugin.Services
{
    internal sealed class QuestManager
    {
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

                long outsideCount = 0;
                if (evt.Zone.ShowEnemiesOutside && evt.CaptureFactionId != 0)
                {
                    try
                    {
                        var outsideSphere = new BoundingSphereD(cacheEntry.Position, evt.Zone.QuestDistance);
                        var entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref outsideSphere);

                        foreach (var ent in entities)
                        {
                            if (ent.Closed || ent.MarkedForClose) continue;
                            if (ent is MySafeZone || ent is MyPlanet || ent is MyVoxelBase) continue;

                            var distSq = Vector3D.DistanceSquared(ent.PositionComp.GetPosition(), cacheEntry.Position);
                            var captureRadiusSq = (double)(cacheEntry.Radius * 0.8f);
                            captureRadiusSq *= captureRadiusSq;
                            if (distSq <= captureRadiusSq) continue;

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
                                if (grid.BigOwners == null || grid.BigOwners.Count == 0) continue;
                                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
                                if (faction == null) continue;
                                factionId = faction.FactionId;
                            }

                            if (factionId != 0 && factionId != evt.CaptureFactionId)
                                outsideCount++;
                        }
                    }
                    catch
                    {
                        outsideCount = 0;
                    }
                }

                var lines = BuildQuestLines(evt, outsideCount);

                if (evt.Zone.DisplayMode == QuestDisplayMode.Notifications)
                {
                    foreach (var p in players)
                    {
                        if (Vector3D.DistanceSquared(p.GetPosition(), cacheEntry.Position) > questDistanceSq)
                            continue;
                        foreach (var line in lines)
                            MyVisualScriptLogicProvider.ShowNotification(line, 1000, "White", p.IdentityId);
                    }
                }
                else if (evt.Zone.DisplayMode == QuestDisplayMode.RichHUD)
                {
                    if (!_richHudTracked.TryGetValue(evt, out var tracked))
                    {
                        tracked = new HashSet<ulong>();
                        _richHudTracked[evt] = tracked;
                    }

                    var inRangeSteamIds = new HashSet<ulong>();
                    foreach (var p in players)
                    {
                        if (Vector3D.DistanceSquared(p.GetPosition(), cacheEntry.Position) > questDistanceSq)
                            continue;
                        inRangeSteamIds.Add(p.SteamUserId);
                    }

                    var msg = new QuestUpdateMessage
                    {
                        ZoneName = evt.Zone.Name,
                        Lines = lines,
                        ZoneX = cacheEntry.Position.X,
                        ZoneY = cacheEntry.Position.Y,
                        ZoneZ = cacheEntry.Position.Z,
                        QuestDistance = evt.Zone.QuestDistance,
                        Timestamp = DateTime.UtcNow.Ticks,
                        Clear = false
                    };

                    foreach (var sid in inRangeSteamIds)
                        SendRichHudMessage(msg, sid);

                    var left = tracked.Where(s => !inRangeSteamIds.Contains(s)).ToList();
                    if (left.Count > 0)
                    {
                        var clear = new QuestUpdateMessage { ZoneName = evt.Zone.Name, Clear = true };
                        foreach (var sid in left)
                            SendRichHudMessage(clear, sid);
                    }

                    tracked.Clear();
                    foreach (var s in inRangeSteamIds)
                        tracked.Add(s);
                }
            }
        }

        private static void SendRichHudMessage(QuestUpdateMessage msg, ulong steamId)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(msg);
            MyAPIGateway.Multiplayer.SendMessageTo(RICH_HUD_CHANNEL, bytes, steamId);
        }

        private static List<string> BuildQuestLines(ZonePointEvent evt, long outsideCount)
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
                    lines.Add("Capturing [" + pct + "%]");
                    lines.Add("Time Remaining [" + ComputeTimeRemaining(evt, true) + "]");
                    if (evt.Zone.ShowEnemiesOutside && outsideCount > 0)
                        lines.Add("Enemies Outside [" + outsideCount + "]");
                    break;

                case CaptureState.Contested:
                    lines.Add("Contested [" + pct + "%]");
                    lines.Add("Enemies Inside [" + evt.TotalEnemiesInside + "]");
                    if (evt.Zone.ShowEnemiesOutside && outsideCount > 0)
                        lines.Add("Enemies Outside [" + outsideCount + "]");
                    break;

                case CaptureState.Decaying:
                    lines.Add("Decay [" + pct + "%]");
                    lines.Add("Time Remaining [" + ComputeTimeRemaining(evt, false) + "]");
                    break;

                case CaptureState.Captured:
                    lines.Add("Held by [" + evt.GetCaptureTag() + "]");
                    break;
            }

            return lines;
        }

        private static string ComputeTimeRemaining(ZonePointEvent evt, bool capturing)
        {
            if (capturing)
            {
                int gainPerTick = ((int)evt.SuitCount * evt.Zone.CaptureRatePerSuit)
                                + ((int)evt.GridCount * evt.Zone.CaptureRatePerGrid);
                if (gainPerTick <= 0) return "--";
                int remaining = evt.Zone.CapturePointsNeeded - evt.CaptureProgress;
                if (remaining <= 0) return "0s";
                int ticksNeeded = remaining / gainPerTick + (remaining % gainPerTick > 0 ? 1 : 0);
                return FormatSeconds(ticksNeeded * evt.Zone.CapturePointIntervalSeconds);
            }
            else
            {
                int lossPerTick = ((int)evt.EnemySuitCount * evt.Zone.CaptureDecayPerSuit)
                                + ((int)evt.EnemyGridCount * evt.Zone.CaptureDecayPerGrid);
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
