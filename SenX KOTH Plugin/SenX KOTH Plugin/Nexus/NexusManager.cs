using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin;
using SenX_KOTH_Plugin.Utils;

namespace Nexus.API
{
    public static class NexusManager
    {
        private static SenX_KOTH_PluginConfig? Config => SenX_KOTH_PluginMain.Instance?.Config;
        private static NexusAPI.Server? LobbyServer;
        private static NexusAPI.Server? ThisServer;
        
        public static void SetServerData(NexusAPI.Server server)
        {
            ThisServer = server;
        }
        
        internal static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamID, bool fromServer)
        {
            NexusMessage? message = MyAPIGateway.Utilities.SerializeFromBinary<NexusMessage>(data);
            if (message == null)
                return;

            if (Config!.isLobby)
            {
                if (message.isTestAnnouncement)
                {
                    DiscordService.SendDiscordWebHook("First Place: [Vengeful Idiots] with 2565 Points!", Color.Gold, 1);
                    Thread.Sleep(5000);
                    DiscordService.SendDiscordWebHook("Second Place: [Space Nuggets] with 1954 Points!", Color.Silver, 1);
                    Thread.Sleep(5000);
                    DiscordService.SendDiscordWebHook("Third Place: [Keyboard Warriors] with 584 Points!", Color.SandyBrown, 1);
                    Thread.Sleep(5000);
            
                    StringBuilder sb = new ();
                    sb.AppendLine("The Other People....");
                    sb.AppendLine("Hamsters of Europa with 486 Points!");
                    sb.AppendLine("TRex's with 386 Points!");
                    sb.AppendLine("Muppet Empire with 212 Points!");
                    DiscordService.SendDiscordWebHook(sb.ToString(), Color.Brown, 1);
                }

                if (message.requestLobbyServer)
                    GetLobbyServer();
            }
        }
        
        public static Task<bool> SendMessageToLobbyServer()
        {
            if (ThisServer is null) return Task.FromResult(false); // IF not properly configured, this could happen.
            if (LobbyServer is null)
            {
                GetLobbyServer();
                Task.Delay(5000); // Wait 5 seconds for the lobby to reply with its data.

                if (LobbyServer is null)
                {
                    return Task.FromResult(false); // No response from lobby, is it offline?
                }
            }
            
            NexusMessage message = new(ThisServer.ServerID, LobbyServer.ServerID, false, null, true, false);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            SenX_KOTH_PluginMain.nexusAPI?.SendMessageToServer(LobbyServer.ServerID, data);
            return Task.FromResult(true);
        }
        
        private static void GetLobbyServer()
        {
            if (ThisServer is null) return;
            NexusMessage message = new(ThisServer!.ServerID, ThisServer.ServerID, false, null, true, false);
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(message);
            SenX_KOTH_PluginMain.nexusAPI?.SendMessageToServer(ThisServer.ServerID, data);
        }
    }

    public class NexusMessage
    {
        public readonly int fromServerID;
        public readonly int toServerID;
        public readonly bool isTestAnnouncement;
        public readonly bool requestLobbyServer;
        public readonly NexusAPI.Server? lobbyServerData;
        public readonly bool isLobbyReply;

        public NexusMessage(int _fromServerId, int _toServerId, bool _isTestAnnouncement, NexusAPI.Server? _lobbyServerData, bool _requestLobbyServer, bool _isLobbyReply)
        {
            fromServerID = _fromServerId;
            toServerID = _toServerId;
            isTestAnnouncement = _isTestAnnouncement;
            requestLobbyServer = _requestLobbyServer;
            isLobbyReply = _isLobbyReply;
            lobbyServerData = _lobbyServerData;
        }
    }
}