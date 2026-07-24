using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Messages;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

namespace SenX_KOTH_Plugin.Mod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class KoTHModSession : MySessionComponentBase
    {
        private const ushort CHANNEL = 43521;

        private KoTHHudManager _hud;
        private readonly Dictionary<string, QuestUpdateMessage> _zoneData = new Dictionary<string, QuestUpdateMessage>();
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
                    _zoneData.Remove(msg.ZoneName);
                else
                    _zoneData[msg.ZoneName] = msg;
            }
            catch { }
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

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(CHANNEL, OnQuestMessage);
            _richHudReady = false;
        }
    }
}
