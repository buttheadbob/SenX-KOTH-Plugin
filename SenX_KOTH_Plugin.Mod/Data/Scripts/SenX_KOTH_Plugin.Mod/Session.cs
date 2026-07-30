using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Messages;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace SenX_KOTH_Plugin.Mod
{
    internal struct ZoneState { public int State; public long FactionId; }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class KoTHModSession : MySessionComponentBase
    {
        private const ushort CHANNEL = 43521;

        private KoTHHudManager _hud;
        private readonly Dictionary<string, QuestUpdateMessage> _zoneData = new Dictionary<string, QuestUpdateMessage>();
        private readonly Dictionary<string, ZoneState> _zonePrevState = new Dictionary<string, ZoneState>();
        private bool _richHudReady;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
                return;

            RichHudFramework.Client.RichHudClient.Init("KoTHHUD", OnRichHudInit, OnRichHudReset);
        }

        private void OnRichHudInit()
        {
            if (!RichHudFramework.Client.RichHudClient.Registered) return;

            RichHudFramework.UI.Client.HudMain.Init();

            _hud = new KoTHHudManager();
            _hud.Init();
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(CHANNEL, OnQuestMessage);
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            _richHudReady = true;
        }

        private void OnRichHudReset()
        {
        }

        private void OnQuestMessage(ushort handlerId, byte[] data, ulong sender, bool fromServer)
        {
            if (!fromServer) return;
            try
            {
                var msg = MyAPIGateway.Utilities.SerializeFromBinary<QuestUpdateMessage>(data);
                if (msg.Clear)
                {
                    _zoneData.Remove(msg.ZoneName);
                    _zonePrevState.Remove(msg.ZoneName);
                }
                else
                {
                    _zoneData[msg.ZoneName] = msg;
                    DetectCaptureEvent(msg);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("KoTH Mod: Failed to deserialize quest message: " + ex.Message);
            }
        }

        private void DetectCaptureEvent(QuestUpdateMessage msg)
        {
            ZoneState prev;
            _zonePrevState.TryGetValue(msg.ZoneName, out prev);

            // State 2 = Captured. Don't fire on same-faction recapture (decay->capture).
            bool justCaptured = prev.State != 2 && msg.StateOrdinal == 2;
            bool sameFactionRecapture = prev.State == 4 && msg.StateOrdinal == 1
                && prev.FactionId == msg.CaptureFactionId;

            _zonePrevState[msg.ZoneName] = new ZoneState { State = msg.StateOrdinal, FactionId = msg.CaptureFactionId };

            if (!justCaptured || sameFactionRecapture || msg.CaptureFactionId == 0) return;

            MyLog.Default.WriteLineAndConsole("KoTH Mod: Capture detected - zone=" + msg.ZoneName + ", factionId=" + msg.CaptureFactionId + ", radius=" + msg.ZoneRadius);

            var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(
                MyAPIGateway.Session?.Player?.IdentityId ?? 0);
            bool bright = playerFaction != null
                && playerFaction.FactionId == msg.CaptureFactionId;

            var zonePos = new Vector3D(msg.ZoneX, msg.ZoneY, msg.ZoneZ);
            ParticleBurst.Spawn(zonePos, msg.ZoneRadius, bright);
        }

        public override void UpdateAfterSimulation()
        {
            if (!_richHudReady || _hud == null || !RichHudFramework.Client.RichHudClient.Registered) return;

            var playerPos = MyAPIGateway.Session?.Player?.GetPosition() ?? Vector3D.Zero;
            var now = DateTime.UtcNow.Ticks;

            QuestUpdateMessage best = null;
            double bestDistSq = double.MaxValue;

            foreach (var msg in _zoneData.Values)
            {
                if (now - msg.Timestamp > TimeSpan.TicksPerSecond * 3) continue;

                var zonePos = new Vector3D(msg.ZoneX, msg.ZoneY, msg.ZoneZ);
                var distSq = Vector3D.DistanceSquared(playerPos, zonePos);
                var maxSq = (double)msg.QuestDistance * msg.QuestDistance;

                if (distSq <= maxSq && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = msg;
                }
            }

            if (best != null)
                _hud.ShowZone(best);
            else
                _hud.Hide();
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/kothdebug", StringComparison.OrdinalIgnoreCase))
                return;

            sendToOthers = false;

            bool isWin = messageText.IndexOf("win", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isLose = messageText.IndexOf("lose", StringComparison.OrdinalIgnoreCase) >= 0;
            bool bright = isWin || (!isWin && !isLose);
            var log = MyLog.Default;

            try
            {
                var player = MyAPIGateway.Session?.Player;
                if (player == null)
                {
                    log.WriteLineAndConsole("KoTH Debug: No player available");
                    return;
                }

                var pos = player.GetPosition();
                var cam = MyAPIGateway.Session.Camera;
                var forward = cam != null ? cam.WorldMatrix.Forward : Vector3D.Forward;
                var spawnPos = pos + forward * 100.0;

                log.WriteLineAndConsole("=== KoTH Debug ===");
                log.WriteLineAndConsole(string.Format("Player: {0:F1} {1:F1} {2:F1}", pos.X, pos.Y, pos.Z));
                log.WriteLineAndConsole(string.Format("Spawn:  {0:F1} {1:F1} {2:F1} (100m fwd)", spawnPos.X, spawnPos.Y, spawnPos.Z));
                log.WriteLineAndConsole(string.Format("Mgr: Enabled={0} Paused={1} Count={2}",
                    MyParticlesManager.Enabled, MyParticlesManager.Paused, MyParticlesManager.InstanceCount));

                log.WriteLineAndConsole(string.Format("Style: {0}", bright ? "WIN (gold)" : "LOSE (blue)"));
                int before = MyParticlesManager.InstanceCount;
                ParticleBurst.Spawn(spawnPos, 50f, bright);
                log.WriteLineAndConsole(string.Format("Count: {0}->{1}", before, MyParticlesManager.InstanceCount));
                log.WriteLineAndConsole("=== KoTH Debug: Done ===");
                MyAPIGateway.Utilities.ShowNotification(
                    string.Format("Fireworks: {0}", bright ? "WIN - gold" : "LOSE - blue"), 5000);
            }
            catch (Exception ex)
            {
                log.WriteLineAndConsole(string.Format("KoTH Debug ERROR: {0}", ex));
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(CHANNEL, OnQuestMessage);
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            _richHudReady = false;
        }
    }
}
