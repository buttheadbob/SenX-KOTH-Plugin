using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
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
            NexusAPI.CrossServerMessage nMessage = MyAPIGateway.Utilities.SerializeFromBinary<NexusAPI.CrossServerMessage>(data);
            NexusMessage? message = MyAPIGateway.Utilities.SerializeFromBinary<NexusMessage>(nMessage.Message);
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
            }
        }
    }

    [ProtoContract]
    public class NexusMessage
    {
        [ProtoMember(100)] public int fromServerID;
        [ProtoMember(101)] public int toServerID;
        [ProtoMember(102)] public bool isTestAnnouncement;
        [ProtoMember(103)] public bool requestLobbyServer;
        [ProtoMember(105)] public bool isLobbyReply;

        public NexusMessage(int _fromServerId, int _toServerId, bool _isTestAnnouncement, bool _requestLobbyServer, bool _isLobbyReply)
        {
            fromServerID = _fromServerId;
            toServerID = _toServerId;
            isTestAnnouncement = _isTestAnnouncement;
            requestLobbyServer = _requestLobbyServer;
            isLobbyReply = _isLobbyReply;
        }

        public NexusMessage()
        {
            
        }
    }
}