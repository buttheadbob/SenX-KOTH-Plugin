using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage;
using VRageMath;

namespace SenX_KOTH_Plugin.Nexus
{
    public class NexusGlobalAPI
    {
        private const long MessageId = 20240902;
        private static readonly object DummyParam = new object();

        public List<Cluster>? Clusters { get; private set; }

        public List<Server>? Servers { get; private set; }

        public List<Sector>? Sectors { get; private set; }

        public byte CurrentServerID { get; private set; }

        public byte CurrentClusterID { get; private set; }

        public bool Enabled { get; private set; }
        private Action? _onEnabled;

        public NexusGlobalAPI(Action? onEnabled = null)
        {
            _onEnabled = onEnabled;

            MyAPIGateway.Utilities.RegisterMessageHandler(MessageId, ReceiveData);
        }

        public void Unload()
        {
            Enabled = false;
            _isPlayerOnline = null;
            _getTargetServer = null;
            _getTargetSector = null;
            _isServerOnline = null;
            _sendModMsgToServer = null;
            _sendModMsgToAllServers = null;
            _getAllOnlineServers = null;
            _getAllOnlinePlayers = null;
            _sendChatToDiscord = null;
            _onEnabled = null;
            MyAPIGateway.Utilities.UnregisterMessageHandler(MessageId, ReceiveData);
        }

        private void ReceiveData(object obj)
        {
            try
            {
                MyTuple<byte[], Func<int, Func<object, object>>> data = (MyTuple<byte[], Func<int, Func<object, object>>>)obj;
                Func<int, Func<object, object>> getMethod = data.Item2;

                _isPlayerOnline = getMethod((int)Methods.IsPlayerOnline);
                _getTargetServer = getMethod((int)Methods.GetTargetServer);
                _getTargetSector = getMethod((int)Methods.GetTargetSector);
                _isServerOnline = getMethod((int)Methods.IsServerOnline);
                _sendModMsgToServer = getMethod((int)Methods.SendModMsgToServer);
                _sendModMsgToAllServers = getMethod((int)Methods.SendModMsgToAllServers);
                _getAllOnlineServers = getMethod((int)Methods.GetAllOnlineServers);
                _getAllOnlinePlayers = getMethod((int)Methods.GetAllOnlinePlayers);
                _sendChatToDiscord = getMethod((int)Methods.SendChatToDiscord);

                ServerDataMsgAPI? serverData = MyAPIGateway.Utilities.SerializeFromBinary<ServerDataMsgAPI>(data.Item1);
                if (serverData != null)
                {
                    Clusters = serverData.clusters;
                    Servers = serverData.servers;
                    Sectors = serverData.sectors;
                    CurrentServerID = serverData.thisServerID;
                    CurrentClusterID = serverData.thisClusterID;
                }

                Enabled = true;
                _onEnabled?.Invoke();
            }
            catch
            { }
        }

        public bool IsPlayerOnline(ulong playerId)
        {
            return Enabled && _isPlayerOnline != null && (bool)_isPlayerOnline(playerId);
        }
        private Func<object, object>? _isPlayerOnline;

        public byte GetTargetServer(Vector3D position)
        {
            if (Enabled && _getTargetServer != null)
                return (byte)_getTargetServer(position);
            return byte.MinValue;
        }
        private Func<object, object>? _getTargetServer;

        public int GetTargetSector(Vector3D position)
        {
            if (Enabled && _getTargetSector != null)
                return (int)_getTargetSector(position);
            return int.MinValue;
        }
        private Func<object, object>? _getTargetSector;

        public bool IsServerOnline(byte serverID)
        {
            return Enabled && _isServerOnline != null && (bool)_isServerOnline(serverID);
        }
        private Func<object, object>? _isServerOnline;

        public bool SendModMsgToServer(byte[] data, long modChannelID, byte targetServer)
        {
            return Enabled && _sendModMsgToServer != null && (bool)_sendModMsgToServer(MyTuple.Create(data, modChannelID, targetServer));
        }
        private Func<object, object>? _sendModMsgToServer;

        public bool SendModMsgToAllServers(byte[] data, long modChannelID)
        {
            return Enabled && _sendModMsgToAllServers != null && (bool)_sendModMsgToAllServers(MyTuple.Create(data, modChannelID));
        }
        private Func<object, object>? _sendModMsgToAllServers;

        public List<byte>? GetAllOnlineServers()
        {
            if (Enabled && _getAllOnlineServers != null)
                return (List<byte>)_getAllOnlineServers(DummyParam);
            return null;
        }
        private Func<object, object>? _getAllOnlineServers;

        public List<ulong>? GetAllOnlinePlayers()
        {
            if (Enabled && _getAllOnlinePlayers != null)
                return (List<ulong>)_getAllOnlinePlayers(DummyParam);
            return null;
        }
        private Func<object, object>? _getAllOnlinePlayers;

        public void SendChatToDiscord(string message, ulong discordChannelID, bool isEmbed = false, string EmbedTitle = "", string EmbedFooter = "")
        {
            if (Enabled && _sendChatToDiscord != null)
                _sendChatToDiscord(MyTuple.Create(message, discordChannelID, isEmbed, EmbedTitle, EmbedFooter));
        }
        private Func<object, object>? _sendChatToDiscord;

        private enum Methods
        {
            None = 0,
            IsPlayerOnline,
            GetTargetServer,
            GetTargetSector,
            IsServerOnline,
            SendModMsgToServer,
            SendModMsgToAllServers,
            GetAllOnlineServers,
            GetAllOnlinePlayers,
            SendChatToDiscord
        }

        [ProtoContract]
        private class ServerDataMsgAPI
        {
            public ServerDataMsgAPI()
            {
                clusters = new List<Cluster>();
                servers = new List<Server>();
                sectors = new List<Sector>();
                thisServerID = 0;
                thisClusterID = 0;
            }

            [ProtoMember(10)]
            public List<Cluster>? clusters;

            [ProtoMember(20)]
            public List<Server>? servers;

            [ProtoMember(30)]
            public List<Sector>? sectors;

            [ProtoMember(40)]
            public byte thisServerID;

            [ProtoMember(50)]
            public byte thisClusterID;
        }

        [ProtoContract]
        public class Cluster
        {
            [ProtoMember(5)] public byte ClusterID { get; set; }
            [ProtoMember(10), DefaultValue("New Cluster")] public string? ClusterName { get; set; } = "New Cluster";
            [ProtoMember(15), DefaultValue("Cluster Description")] public string? ClusterDescription { get; set; } = "Cluster Description";
            [ProtoMember(20)] public byte LobbyServerID { get; set; }
            [ProtoMember(25), DefaultValue((ushort)10)] public ushort GeneralSectorID { get; set; } = 10;
        }

        [ProtoContract]
        public class Sector
        {
            [ProtoMember(1), DefaultValue("NewSector")]
            public string? SectorName { get; set; } = "NewSector";

            [ProtoMember(2), DefaultValue("NewSectorDescription")]
            public string? SectorDescription { get; set; } = "NewSectorDescription";

            [ProtoMember(3)]
            public byte OnServerID { get; set; }

            [ProtoMember(4), DefaultValue(SectorShape.Sphere)]
            public SectorShape SectorShape { get; set; } = SectorShape.Sphere;

            [ProtoMember(5)]
            public double X { get; set; }

            [ProtoMember(6)]
            public double Y { get; set; }

            [ProtoMember(7)]
            public double Z { get; set; }

            [ProtoMember(8)]
            public double DX { get; set; }

            [ProtoMember(9)]
            public double DY { get; set; }

            [ProtoMember(10)]
            public double DZ { get; set; }

            [ProtoMember(11)]
            public float RadiusKM { get; set; }

            [ProtoMember(12)]
            public float RingRadiusKM { get; set; }

            [ProtoMember(13)]
            public ushort SectorID { get; set; }

            [ProtoMember(14)]
            public string? SectorBoundaryScript { get; set; }

            [ProtoMember(16)]
            public bool HiddenSector { get; set; }

            [ProtoMember(17), DefaultValue(true)]
            public bool EnableSectorInfoProvider { get; set; } = true;

            [ProtoMember(18)]
            public SectorBorderTexture BorderTexture { get; set; }

            [ProtoMember(19, IsRequired = true)]
            public Color BorderColor { get; set; } = Color.White;
        }

        [ProtoContract]
        public class Server
        {
            [ProtoMember(1), DefaultValue("NewServer")] public string? Name { get; set; } = "NewServer";
            [ProtoMember(2), DefaultValue((byte)1)] public byte ServerID { get; set; } = 1;
            [ProtoMember(3)] public byte OnClusterID { get; set; }
            [ProtoMember(4), DefaultValue("127.0.0.1")] public string? GameIPAddress { get; set; } = "127.0.0.1";
            [ProtoMember(5), DefaultValue((ushort)27018)] public ushort GamePort { get; set; } = 27018;
            [ProtoMember(6), DefaultValue(ServerType.SyncedSectored)] public ServerType SectorType { get; set; } = ServerType.SyncedSectored;
            [ProtoMember(7)] public ushort SelectedConfigGroup { get; set; }
            [ProtoMember(8), DefaultValue("XYZ")] public string? ServerAbbreviation { get; set; } = "XYZ";
            [ProtoMember(9), DefaultValue(0)] public byte LobbyServerID { get; set; } = 0;
        }

        public enum SectorShape
        {
            Sphere,
            Cuboid,
            Torus
        }

        public enum ServerType
        {
            SyncedSectored,
            SyncedNonSectored,
            NonSyncedNonSectored,
            StartSyncedNonSectored
        }

        public enum SectorBorderTexture
        {
            Circle,
            Cross,
            Hex,
        }

        [ProtoContract]
        public class ModAPIMsg
        {
            [ProtoMember(10)]
            public byte fromServerID;

            [ProtoMember(20)]
            public byte toServerID;

            [ProtoMember(25)]
            public long targetModMessageID;

            [ProtoMember(30)]
            public byte[]? msgData;
        }
    }
}
