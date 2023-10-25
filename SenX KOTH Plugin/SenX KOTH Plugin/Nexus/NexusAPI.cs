using Nexus.BoundarySystem;
using Nexus.Commands;
using Nexus.PubSub;
using Nexus.Sync;
using NLog;
using ProtoBuf;
using Sandbox;
using Sandbox.Game.World;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;
using static Nexus.API.NexusAPI;

namespace Nexus.API
{
    public class NexusServerSideAPI
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static Main Plugin;
        public int CrossServerModID;

        public NexusServerSideAPI(int SocketID)
        {
            CrossServerModID = SocketID;
        }

        /*  ServerSide 
         * 
         * 
         */

        public static void IsRunningNexus(ref bool __result)
        {
            __result = true;
        }

        public static void IsPlayerOnline(ref bool __result, long IdentityID)
        {
            __result = OnlineServerSync.IsPlayerOnline(IdentityID);
        }

        public static void GetSectorsObject(ref List<object[]> __result)
        {
            List<object[]> APISectors = new ();
            foreach (var P in RegionHandler.Regions)
            {
                if (P.SectorType == SectorTypes.LobbyServer)
                    continue;

                bool flag = false;
                if (P.SectorType == SectorTypes.GeneralSpaceSector)
                    flag = true;

                object[] X2 = { P.Name, P.IPAddress, P.Port, flag, P.GetCenter(), P.Radius, P.ServerID };

                //Sector X = new Sector(P.Name, P.IPAddress, P.Port, flag, P.Center, P.Radius, P.ServerID);
                APISectors.Add(X2);
            }

            __result = APISectors;
        }
        
        public static void GetServerIDFromPosition(ref int __result, Vector3D Position)
        {
            __result =  RegionHandler.GetSector(Position).ServerID;
        }
        
        public static void GetAllOnlinePlayersObject(ref List<object[]> __result)
        {
            List<object[]> OnlinePlayers = new List<object[]>();

            foreach (var A in OnlineServerSync.GetAllOnlinePlayers())
            {
                object[] P2 = new object[] { A.PlayerName, A.SteamID, A.IdentityID, A.OnServer };
                //Player P = new Player(A.PlayerName, A.SteamID, A.IdentityID, A.OnServer);
                OnlinePlayers.Add(P2);
            }

            __result = OnlinePlayers;
        }

        public static void GetAllOnlineServersObject(ref List<object[]> __result)
        {
            List<object[]> Servers = new List<object[]>();
            foreach (var S in OnlineServerSync.OnlineServers)
            {
                object[] Ser2 = new object[] { S.ServerName, S.ServerID, S.MaxPlayers, S.ServerSS, S.TotalGrids, S.ReservedPlayers };
                //Server Ser = new Server(S.Name, S.ServerID);
                Servers.Add(Ser2);
            }

            __result = Servers;
        }

        public static void GetAllServersObject(ref List<object[]> __result)
        {
            List<object[]> Servers = new List<object[]>();
            foreach (var S in RegionHandler.Servers)
            {



                object[] Ser2 = new object[] { S.Name, S.ServerID, (int)S.SelectedServerType, S.IPAddress + ":" + S.Port, S.ServerAbbreviation};
                //Server Ser = new Server(S.Name, S.ServerID);
                Servers.Add(Ser2);
            }

            __result = Servers;
        }

        public static void GetThisServerObject(ref object[] __result)
        {
            BoundarySystem.Server S = RegionHandler.ThisServer;
            __result = new object[6] { S.Name, S.ServerID, MySession.Static.Settings.MaxPlayers, 0, 0, MySandboxGame.ConfigDedicated.Reserved };

        }

        public static void IsServerOnline(ref bool __result, int ServerID)
        {
            __result = OnlineServerSync.IsServerOnline(ServerID);
        }

        /* Plugin connections */
        public static void BackupGrid(List<MyObjectBuilder_CubeGrid> GridObjectBuilders, long OnwerIdentity)
        {
            PluginDependencyManager.GridBackupInvoke(GridObjectBuilders, OnwerIdentity);
        }

        public static void SendChatMessageToDiscord(ulong ChannelID, string Author, string Message)
        {
            DiscordAPIMessage Discord = new DiscordAPIMessage(ChannelID);
            Discord.CreateChatMessage(Author, Message);
            Discord.Push();
        }
        
        public static void SendEmbedMessageToDiscord(ulong ChannelID, string EmbedTitle, string EmbedMsg, string EmbedFooter, string EmbedColor = null)
        {
            DiscordAPIMessage Discord = new DiscordAPIMessage(ChannelID);
            Discord.CreateEmbedMessage(EmbedTitle, EmbedMsg, EmbedFooter, EmbedColor);
            Discord.Push();
        }

        /* Cross-Server Messaging */
        public static void SendMessageToServer(ref NexusAPI __instance, int ServerID, byte[] Message)
        {
            CrossServerMessage CrossMessage = new CrossServerMessage(__instance.CrossServerModID, ServerID, Main.Config.ServerID, Message);
            Sockets.Publish(DataStructures.SubsriptionType.MessageType.ModAPI, CrossMessage);
        }
        
        public static void SendMessageToAllServers(ref NexusAPI __instance, byte[] Message)
        {
            CrossServerMessage CrossMessage = new CrossServerMessage(__instance.CrossServerModID, 0, Main.Config.ServerID, Message);
            Sockets.Publish(DataStructures.SubsriptionType.MessageType.ModAPI, CrossMessage);
        }
    }

    public class NexusAPI
    {
        public ushort CrossServerModID;

        /*  For recieving custom messages you have to register a message handler with a different unique ID then what you use server to client. (It should be the same as this class)
         *  
         *  NexusAPI(5432){
         *  CrossServerModID = 5432
         *  }
         *  
         *  
         *  Register this somewhere in your comms code. (This will only be raised when it recieves a message from another server)
         *  MyAPIGateway.Multiplayer.RegisterMessageHandler(5432, MessageHandler);
         */

        public NexusAPI(ushort SocketID)
        {
            CrossServerModID = SocketID;
        }

        public static bool IsRunningNexus()
        {
            return false;
        }

        public static bool IsPlayerOnline(long IdentityID)
        {
            return false;
        }

        private static List<object[]> GetSectorsObject()
        {
            List<object[]> APISectors = new ();
            return APISectors;
        }

        private static List<object[]> GetAllOnlinePlayersObject()
        {
            List<object[]> OnlinePlayers = new ();
            return OnlinePlayers;
        }

        private static List<object[]> GetAllServersObject()
        {
            List<object[]> Servers = new ();
            return Servers;

        }
        
        private static List<object[]> GetAllOnlineServersObject()
        {
            List<object[]> Servers = new ();
            return Servers;
        }

        private static object[] GetThisServerObject()
        {
            object[] OnlinePlayers = new object[6];
            return OnlinePlayers;
        }

        public static Server GetThisServer()
        {
            object[] obj = GetThisServerObject();
            return new Server((string)obj[0], (int)obj[1], (short)obj[2], (int)obj[3], (int)obj[4], (List<ulong>)obj[5]);
        }

        public static List<Sector> GetSectors()
        {
            List<object[]> Objs = GetSectorsObject();
            
            List<Sector> Sectors = new ();
            foreach (var obj in Objs)
            {
                Sectors.Add(new Sector((string)obj[0], (string)obj[1], (int)obj[2], (bool)obj[3], (Vector3D)obj[4], (double)obj[5], (int)obj[6]));
            }
            return Sectors;
        }

        public static int GetServerIDFromPosition(Vector3D Position)
        {
            return 0;
        }

        public static List<Player> GetAllOnlinePlayers()
        {
            List<object[]> Objs = GetAllOnlinePlayersObject();

            List<Player> Players = new ();
            foreach (var obj in Objs)
            {
                Players.Add(new Player((string)obj[0], (ulong)obj[1], (long)obj[2], (int)obj[3]));
            }
            return Players;
        }

        public static List<Server> GetAllServers()
        {
            List<object[]> Objs = GetAllServersObject();

            List<Server> Servers = new ();
            foreach (var obj in Objs)
            {
                Servers.Add(new Server((string)obj[0], (int)obj[1], (int)obj[2],  (string)obj[3]));
            }
            return Servers;
        }
        
        public static List<Server> GetAllOnlineServers()
        {
            List<object[]> Objs = GetAllOnlineServersObject();

            List<Server> Servers = new ();
            foreach (var obj in Objs)
            {
                Servers.Add(new Server((string)obj[0], (int)obj[1], (int)obj[2], (float)obj[3], (int)obj[4], (List<ulong>)obj[5]));
            }
            return Servers;
        }

        public static bool IsServerOnline(int ServerID)
        {
            return false;
        }
        public static void BackupGrid(List<MyObjectBuilder_CubeGrid> GridObjectBuilders, long OnwerIdentity)
        {
            return;
        }
        public static void SendChatMessageToDiscord(ulong ChannelID, string Author, string Message) {}
        public static void SendEmbedMessageToDiscord(ulong ChannelID, string EmbedTitle, string EmbedMsg, string EmbedFooter, string? EmbedColor = null) {}

        public void SendMessageToServer(int ServerID, byte[] Message)
        {
            return;
        }

        public void SendMessageToAllServers(byte[] Message)
        {
            return;
        }

        public class Sector
        {
            public readonly string Name;
            public readonly string IPAddress;
            public readonly int Port;
            public readonly bool IsGeneralSpace;
            public readonly Vector3D Center;
            public readonly double Radius;
            public readonly int ServerID;

            public Sector(string Name, string IPAddress, int Port, bool IsGeneralSpace, Vector3D Center, double Radius, int ServerID)
            {
                this.Name = Name;
                this.IPAddress = IPAddress;
                this.Port = Port;
                this.IsGeneralSpace = IsGeneralSpace;
                this.Center = Center;
                this.Radius = Radius;
                this.ServerID = ServerID;
            }
        }

        public class Player
        {
            public readonly string PlayerName;
            public readonly ulong SteamID;
            public readonly long IdentityID;
            public readonly int OnServer;

            public Player(string PlayerName, ulong SteamID, long IdentityID, int OnServer)
            {
                this.PlayerName = PlayerName;
                this.SteamID = SteamID;
                this.IdentityID = IdentityID;
                this.OnServer = OnServer;
            }
        }

        public partial class Server
        {
            public readonly string Name;
            public readonly int ServerID;
            public readonly int ServerType;
            public readonly string ServerIP;

            public readonly int MaxPlayers;
            public readonly float ServerSS;
            public readonly int TotalGrids;
            public readonly List<ulong> ReservedPlayers;

            /*  Possible Server Types
             * 
             *  0 - SyncedSectored
             *  1 - SyncedNon-Sectored
             *  2 - Non-Synced & Non-Sectored
             * 
             */

            public Server(string Name, int ServerID, int ServerType, string IP)
            {
                this.Name = Name;
                this.ServerID = ServerID;
                this.ServerType = ServerType;
                this.ServerIP = IP;
            }

            //Online Server
            public Server(string Name, int ServerID, int MaxPlayers, float SimSpeed, int TotalGrids, List<ulong> ReservedPlayers)
            {
                this.Name = Name;
                this.ServerID = ServerID;
                this.MaxPlayers = MaxPlayers;
                this.ServerSS = SimSpeed;
                this.TotalGrids = TotalGrids;
                this.ReservedPlayers = ReservedPlayers;
            }
        }


        [ProtoContract]
        public class CrossServerMessage
        {
            [ProtoMember(1)] public readonly int ToServerID;
            [ProtoMember(2)] public readonly int FromServerID;
            [ProtoMember(3)] public readonly ushort UniqueMessageID;
            [ProtoMember(4)] public readonly byte[] Message;

            public CrossServerMessage(ushort UniqueMessageID, int ToServerID, int FromServerID, byte[] Message)
            {
                this.UniqueMessageID = UniqueMessageID;
                this.ToServerID = ToServerID;
                this.FromServerID = FromServerID;
                this.Message = Message;
            }

            public CrossServerMessage() { }
        }
    }
}