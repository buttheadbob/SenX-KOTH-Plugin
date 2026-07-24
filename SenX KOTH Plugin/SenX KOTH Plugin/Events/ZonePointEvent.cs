using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Nexus;
using SenX_KOTH_Plugin.Utils;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace SenX_KOTH_Plugin.Events
{
    internal enum CaptureState { Neutral, Capturing, Captured, Contested, Decaying }
    internal enum EvictionPhase { Idle, Warning30, Warning10, Active }

    internal sealed class ZonePointEvent : IKothEvent
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => ZonePointEvent");
        private static readonly EnterAlertService EnterAlerts = new();
        private readonly KothAudioManager _audio = new();

        private readonly KothZone _zone;
        private readonly SenX_KOTH_PluginConfig _config;
        private readonly BanksData _bankData;
        private readonly EventData _eventData;

        private Timer? _captureTimer;
        private Timer? _awardTimer;

        private CaptureState _state = CaptureState.Neutral;
        private long _captureFactionId;
        private long _suitCount;
        private long _gridCount;
        private long _enemySuitCount;
        private long _enemyGridCount;
        private int _captureProgress;
        private int _lastAnnouncedProgress;

        private Timer? _evictionTickTimer;
        private EvictionPhase _evictionPhase = EvictionPhase.Idle;
        private DateTime _evictionPhaseEntered;
        private DateTime _lastEvictionEnded;
        private int _integrityFrameCounter;
        private int _dynamicMoveFrameCounter;

        public string Name => _zone.Name;
        public bool ShouldRun => _zone.Enabled && MySession.Static != null && IsWithinSchedule();
        public string ShouldRunStatus =>
            "Enabled=" + _zone.Enabled + " Session=" + (MySession.Static != null) + " Schedule=" + IsWithinSchedule();
        public bool IsRunning { get; private set; }

        internal CaptureState State => _state;
        internal long CaptureFactionId => _captureFactionId;
        internal long SuitCount => _suitCount;
        internal long GridCount => _gridCount;
        internal long EnemySuitCount => _enemySuitCount;
        internal long EnemyGridCount => _enemyGridCount;
        internal long TotalEnemiesInside => _enemySuitCount + _enemyGridCount;
        internal int CaptureProgress => _captureProgress;
        internal KothZone Zone => _zone;

        public ZonePointEvent(KothZone zone, SenX_KOTH_PluginConfig config, BanksData bankData, EventData eventData)
        {
            _zone = zone;
            _config = config;
            _bankData = bankData;
            _eventData = eventData;
        }

        public void Start()
        {
            Log.Info("Zone event starting: " + _zone.Name);
            DiscoverZone();
            _captureTimer = new Timer(_zone.CapturePointIntervalSeconds * 1000);
            _captureTimer.Elapsed += CaptureTick;
            _captureTimer.Start();

            _awardTimer = new Timer(_zone.PointAwardIntervalSeconds * 1000);
            _awardTimer.Elapsed += AwardTick;
            _awardTimer.Start();

            _evictionTickTimer = new Timer(1000);
            _evictionTickTimer.Elapsed += EvictionTick;
            _evictionTickTimer.Start();

            IsRunning = true;
        }

        public void Stop()
        {
            Log.Info("Zone event stopping: " + _zone.Name);
            _captureTimer?.Stop();
            _captureTimer?.Dispose();
            _captureTimer = null;
            _awardTimer?.Stop();
            _awardTimer?.Dispose();
            _awardTimer = null;
            _evictionTickTimer?.Stop();
            _evictionTickTimer?.Dispose();
            _evictionTickTimer = null;

            if (_evictionPhase == EvictionPhase.Active)
                RestoreZoneFromEviction();

            _evictionPhase = EvictionPhase.Idle;
            IsRunning = false;
        }

        public void Update()
        {
            if (MySession.Static == null || MySession.Static.IsSaveInProgress) return;
            if (!_zone.Enabled) return;

            try
            {
                var cacheEntry = ZoneManager.ZoneCache.FirstOrDefault(z =>
                    string.Equals(z.ZoneName, _zone.Name, StringComparison.OrdinalIgnoreCase));
                if (cacheEntry == null) return;

                var captureRadius = cacheEntry.Radius * 0.8f;
                var sphere = new BoundingSphereD(cacheEntry.Position, captureRadius);
                var entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                var factionCounts = new Dictionary<long, (int suits, int grids)>();

                foreach (var ent in entities)
                {
                    if (ent.Closed || ent.MarkedForClose) continue;
                    if (ent is MySafeZone || ent is MyPlanet || ent is MyVoxelBase) continue;

                    if (ent is MyCharacter character)
                    {
                        var identityId = character.GetPlayerIdentityId();
                        if (identityId == 0) continue;
                        var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(identityId);
                        if (faction == null) continue;
                        if (!factionCounts.ContainsKey(faction.FactionId))
                            factionCounts[faction.FactionId] = (0, 0);
                        var fc = factionCounts[faction.FactionId];
                        factionCounts[faction.FactionId] = (fc.suits + 1, fc.grids);

                        var player = FindPlayer(identityId);
                        if (player != null && EnterAlerts.ShouldAlert(player, _zone.Name))
                        {
                            var msg = EnterAlerts.BuildMessage(player, faction, _zone.Name, cacheEntry.Position, _zone.Radius);
                            DiscordService.SendAlertWebHook(msg);
                        }
                    }
                    else if (ent is MyCubeGrid grid)
                    {
                        if (grid.BigOwners == null || grid.BigOwners.Count == 0) continue;
                        var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
                        if (faction == null) continue;
                        if (!factionCounts.ContainsKey(faction.FactionId))
                            factionCounts[faction.FactionId] = (0, 0);
                        var fc = factionCounts[faction.FactionId];
                        factionCounts[faction.FactionId] = (fc.suits, fc.grids + 1);
                    }
                }

                var prevState = _state;
                DetermineState(factionCounts);

                if (_state != prevState)
                    AnnounceStateChange();

                if (_zone.DynamicZone)
                {
                    _dynamicMoveFrameCounter++;
                    var moveIntervalFrames = _zone.DynamicMoveIntervalSeconds * 60;
                    if (_dynamicMoveFrameCounter >= moveIntervalFrames)
                    {
                        _dynamicMoveFrameCounter = 0;
                        if (cacheEntry != null)
                            ZoneManager.MoveDynamicZone(cacheEntry, _zone);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ZonePointEvent.Update: " + _zone.Name);
            }
        }

        public void IntegrityCheck()
        {
            if (MySession.Static == null || MySession.Static.IsSaveInProgress) return;

            _integrityFrameCounter++;
            if (_integrityFrameCounter < 300) return;
            _integrityFrameCounter = 0;

            try
            {
                var existing = ZoneManager.FindExistingSafeZone(_zone.Name);

                if (_zone is { Enabled: false, PersistVisual: false })
                {
                    if (existing != null)
                    {
                        GameThread.Invoke(existing.Close);
                        Log.Info("IntegrityCheck [" + _zone.Name + "]: Removed safe zone (disabled, not persisted)");
                    }
                    return;
                }

                if (existing == null)
                {
                    var pos = _zone.Position;
                    ZoneManager.CreateSafeZoneEntity(_zone, pos);
                    ZoneManager.CacheZone(_zone.Name, pos, _zone.Radius, 0, _zone);
                    Log.Info("IntegrityCheck [" + _zone.Name + "]: Created missing safe zone at " + pos);
                }
                else
                {
                    var cacheEntry = ZoneManager.ZoneCache.FirstOrDefault(z => string.Equals(z.ZoneName, _zone.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (!existing.InScene)
                    {
                        Log.Warn($"SafeZone {existing.DisplayName} not added to scene.");
                        GameThread.Invoke(() => MyEntities.Add(existing));
                    }
                    
                    if (cacheEntry != null && cacheEntry.SafeZoneEntityId != existing.EntityId)
                    {
                        cacheEntry.SafeZoneEntityId = existing.EntityId;
                        Log.Info("IntegrityCheck [" + _zone.Name + "]: Updated cache entity ID to " + existing.EntityId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in IntegrityCheck: " + _zone.Name);
            }
        }

        private bool IsWithinSchedule()
        {
            var now = DateTime.Now;
            var day = now.DayOfWeek;

            bool activeDay = day switch
            {
                DayOfWeek.Monday => _zone.ScheduleMonday,
                DayOfWeek.Tuesday => _zone.ScheduleTuesday,
                DayOfWeek.Wednesday => _zone.ScheduleWednesday,
                DayOfWeek.Thursday => _zone.ScheduleThursday,
                DayOfWeek.Friday => _zone.ScheduleFriday,
                DayOfWeek.Saturday => _zone.ScheduleSaturday,
                DayOfWeek.Sunday => _zone.ScheduleSunday,
                _ => false
            };

            if (!activeDay) return false;

            var start = new TimeSpan(_zone.ScheduleStartHour, _zone.ScheduleStartMinute, 0);
            var end = new TimeSpan(_zone.ScheduleEndHour, _zone.ScheduleEndMinute, 0);
            var current = now.TimeOfDay;

            if (end <= start)
                return current >= start || current <= end;

            return current >= start && current <= end;
        }

        public void Save() { }

        private void DetermineState(Dictionary<long, (int suits, int grids)> factionCounts)
        {
            if (factionCounts.Count == 0)
            {
                _captureFactionId = 0;
                _suitCount = 0;
                _gridCount = 0;
                _enemySuitCount = 0;
                _enemyGridCount = 0;

                if (_state == CaptureState.Captured || _state == CaptureState.Decaying)
                    _state = CaptureState.Decaying;
                else
                    _state = CaptureState.Neutral;
                return;
            }

            var dominant = factionCounts.OrderByDescending(kv => kv.Value.suits + kv.Value.grids).First();
            var prevFactionId = _captureFactionId;

            _captureFactionId = dominant.Key;
            _suitCount = dominant.Value.suits;
            _gridCount = dominant.Value.grids;
            _enemySuitCount = 0;
            _enemyGridCount = 0;

            foreach (var kv in factionCounts)
            {
                if (kv.Key == dominant.Key) continue;
                _enemySuitCount += kv.Value.suits;
                _enemyGridCount += kv.Value.grids;
            }

            bool hasEnemies = factionCounts.Count > 1;

            if (hasEnemies)
            {
                if (_state == CaptureState.Captured || _state == CaptureState.Decaying)
                    _state = CaptureState.Decaying;
                else
                    _state = CaptureState.Contested;
            }
            else
            {
                if (_captureProgress >= _zone.CapturePointsNeeded)
                    _state = CaptureState.Captured;
                else if (_captureProgress > 0 || prevFactionId == _captureFactionId)
                    _state = CaptureState.Capturing;
                else
                    _state = CaptureState.Capturing;
            }
        }

        private void CaptureTick(object? sender, ElapsedEventArgs e)
        {
            try
            {
                switch (_state)
                {
                    case CaptureState.Capturing:
                    {
                        int gain = ((int)_suitCount * _zone.CaptureRatePerSuit)
                                 + ((int)_gridCount * _zone.CaptureRatePerGrid);
                        if (gain <= 0) break;

                        _captureProgress += gain;
                        if (_captureProgress > _zone.CapturePointsNeeded)
                            _captureProgress = _zone.CapturePointsNeeded;

                        int pct = _captureProgress * 100 / _zone.CapturePointsNeeded;
                        int threshold = pct / 10 * 10;
                        if (threshold > _lastAnnouncedProgress)
                        {
                            _lastAnnouncedProgress = threshold;
                            _audio.Play2DSound(_captureFactionId, SoundCueType.ZoneLocking);
                        }

                        if (_captureProgress >= _zone.CapturePointsNeeded)
                        {
                            _state = CaptureState.Captured;
                            _lastAnnouncedProgress = 0;
                            Log.Info("Zone captured: " + _zone.Name + " by factionId " + _captureFactionId);
                            _audio.Play2DSound(_captureFactionId, SoundCueType.MatchWon);
                        }
                        break;
                    }

                    case CaptureState.Decaying:
                    {
                        int loss = ((int)_enemySuitCount * _zone.CaptureDecayPerSuit)
                                 + ((int)_enemyGridCount * _zone.CaptureDecayPerGrid);
                        if (loss <= 0) break;

                        _captureProgress -= loss;

                        if (_captureProgress <= 0)
                        {
                            _captureProgress = 0;
                            _lastAnnouncedProgress = 0;
                            _state = CaptureState.Neutral;
                            Log.Info("Zone capture lost: " + _zone.Name);
                            _audio.Play2DSound(_captureFactionId, SoundCueType.ZoneLost);
                        }
                        break;
                    }

                    case CaptureState.Contested:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CaptureTick: " + _zone.Name);
            }
        }

        private void AwardTick(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (_state != CaptureState.Captured) return;

                int points = ((int)_suitCount * _zone.PointsPerSuit) + ((int)_gridCount * _zone.PointsPerGrid);
                if (points <= 0) return;

                IMyFaction? faction = null;
                MyAPIGateway.Session.Factions.Factions.TryGetValue(_captureFactionId, out faction);
                if (faction == null) return;

                var pt = new PointEarned
                {
                    Points = points,
                    FactionId = faction.FactionId,
                    FactionName = faction.Name,
                    FactionTag = faction.Tag,
                    FromServerID = SenX_KOTH_PluginMain.NexusGlobalAPI is { Enabled: true } api ? api.CurrentServerID : (byte)0,
                    EarnedAt = DateTime.UtcNow,
                    ZoneName = _zone.Name,
                    EventId = NexusManager.GenerateEventId()
                };

                NexusManager.AddPointEvent(_eventData, pt);
                NexusManager.BroadcastPointDelta(pt);
                BankService.CreditPoints(_bankData, faction.FactionId, faction.Name, faction.Tag, points);

                Log.Info("Points: [" + faction.Tag + "] +" + points + "pts in " + _zone.Name);

                _audio.Play2DSound(0, SoundCueType.PointEarned);

                if (_config.Show_AttackMessages && _config.WebHookEnabled)
                    DiscordService.SendDiscordWebHook(faction.Tag + " earned " + points + "pts in " + _zone.Name + "!",
                        System.Drawing.Color.Orange, 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AwardTick: " + _zone.Name);
            }
        }

        private void EvictionTick(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_zone.EvictionEnabled) return;
                if (_zone is { Enabled: false, EvictionDuringDowntime: false }) return;

                var now = DateTime.UtcNow;

                switch (_evictionPhase)
                {
                    case EvictionPhase.Idle:
                    {
                        var interval = TimeSpan.FromMinutes(_zone.EvictionFrequencyMinutes);
                        if (interval.TotalMinutes <= 0) return;
                        if (_lastEvictionEnded != default && (now - _lastEvictionEnded) < interval) return;

                        _evictionPhase = EvictionPhase.Warning30;
                        _evictionPhaseEntered = now;
                        Log.Info("Eviction warning 30s: " + _zone.Name);
                        NotifyWithinRange(_zone.Name + " — zone eviction in 30 seconds!", "Red");
                        break;
                    }

                    case EvictionPhase.Warning30:
                    {
                        if ((now - _evictionPhaseEntered).TotalSeconds < 20) return;
                        _evictionPhase = EvictionPhase.Warning10;
                        _evictionPhaseEntered = now;
                        NotifyWithinRange(_zone.Name + " — zone eviction in 10 seconds! Leave immediately!", "Red");
                        break;
                    }

                    case EvictionPhase.Warning10:
                    {
                        if ((now - _evictionPhaseEntered).TotalSeconds < 10) return;

                        if (_zone.EvictionResetCapture)
                        {
                            _captureProgress = 0;
                            _captureFactionId = 0;
                            _state = CaptureState.Neutral;
                            _lastAnnouncedProgress = 0;
                        }

                        var cacheEntry = ZoneManager.ZoneCache.FirstOrDefault(z =>
                            string.Equals(z.ZoneName, _zone.Name, StringComparison.OrdinalIgnoreCase));
                        if (cacheEntry != null)
                        {
                            Log.Info("Eviction: found cache entry for " + _zone.Name + " entityId=" + cacheEntry.SafeZoneEntityId);
                            var r = _zone.EvictionColorR;
                            var g = _zone.EvictionColorG;
                            var b = _zone.EvictionColorB;
                            var tex = string.IsNullOrEmpty(_zone.EvictionTexture) ? null : _zone.EvictionTexture;
                            ZoneManager.ApplyEvictionState(cacheEntry, r, g, b, tex);
                        }
                        else
                        {
                            Log.Warn("Eviction: no cache entry found for " + _zone.Name);
                        }

                        Log.Info("Eviction active for zone: " + _zone.Name);
                        _evictionPhase = EvictionPhase.Active;
                        _evictionPhaseEntered = now;
                        break;
                    }

                    case EvictionPhase.Active:
                    {
                        var duration = TimeSpan.FromSeconds(_zone.EvictionDurationSeconds);
                        if (duration.TotalSeconds <= 0 || (now - _evictionPhaseEntered) < duration) return;

                        RestoreZoneFromEviction();
                        _evictionPhase = EvictionPhase.Idle;
                        _lastEvictionEnded = now;
                        Log.Info("Eviction ended for zone: " + _zone.Name);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EvictionTick: " + _zone.Name);
            }
        }

        private void RestoreZoneFromEviction()
        {
            var cacheEntry = ZoneManager.ZoneCache.FirstOrDefault(z =>
                string.Equals(z.ZoneName, _zone.Name, StringComparison.OrdinalIgnoreCase));
            if (cacheEntry != null)
                ZoneManager.RestoreEvictionState(cacheEntry);
        }

        private void NotifyWithinRange(string msg, string color)
        {
            var cacheEntry = ZoneManager.ZoneCache.FirstOrDefault(z =>
                string.Equals(z.ZoneName, _zone.Name, StringComparison.OrdinalIgnoreCase));
            if (cacheEntry == null) return;

            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var p in players)
            {
                if (p.Character == null) continue;
                var distSq = Vector3D.DistanceSquared(p.Character.WorldMatrix.Translation, cacheEntry.Position);
                if (distSq > 25000 * 25000) continue;
                MyVisualScriptLogicProvider.ShowNotification(msg, 5000, color, p.IdentityId);
            }
        }

        private void AnnounceStateChange()
        {
            switch (_state)
            {
                case CaptureState.Capturing:
                    break;
                case CaptureState.Captured:
                    break;
                case CaptureState.Contested:
                    _audio.Play2DSound(_captureFactionId, SoundCueType.EnemyEnteredZone);
                    break;
                case CaptureState.Decaying:
                    _audio.Play2DSound(_captureFactionId, SoundCueType.EnemyEnteredZone);
                    break;
                case CaptureState.Neutral:
                    break;
            }
        }

        internal string GetCaptureTag()
        {
            if (_captureFactionId == 0) return "??";
            IMyFaction? f = null;
            MyAPIGateway.Session.Factions.Factions.TryGetValue(_captureFactionId, out f);
            return f?.Tag ?? "??";
        }

        private static IMyPlayer? FindPlayer(long identityId)
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var p in players)
                if (p.IdentityId == identityId) return p;
            return null;
        }

        private void DiscoverZone()
        {
            if (MySession.Static == null) return;
            var existing = ZoneManager.FindExistingSafeZone(_zone.Name);
            if (existing != null)
            {
                ZoneManager.CacheZone(_zone.Name, existing.PositionComp.GetPosition(), _zone.Radius, existing.EntityId, _zone);
                Log.Info("Zone [" + _zone.Name + "]: Found existing safe zone entity " + existing.EntityId);
                return;
            }
            var pos = _zone.Position;
            ZoneManager.CreateSafeZoneEntity(_zone, pos);
            ZoneManager.CacheZone(_zone.Name, pos, _zone.Radius, 0, _zone);
            Log.Info("Zone [" + _zone.Name + "]: Created new safe zone entity at " + pos);
        }
    }
}
