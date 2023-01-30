using Sandbox.ModAPI;
using System;
using System.Text;
using NLog;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Network
{
    public sealed class NetworkService
    {
        private static readonly Logger Log = LogManager.GetLogger("KOTH => Network Log");
        public static void NetworkInit()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8008, HandleIncomingPacket);
        }

        private static void HandleIncomingPacket(ushort comId, byte[] msg, ulong id, bool reliable)
        {
            try
            {
                if (msg.IsNullOrEmpty()) return;
                string message = Encoding.ASCII.GetString(msg);
                if (message.Equals("clear")) return;

                DiscordService.SendDiscordWebHook(SenX_KOTH_PluginMain.Instance.Config.CustomMessegeEnable
                    ? SenX_KOTH_PluginMain.Instance.Config.CustomMessege
                    : message);
            }
            catch (Exception error)
            {
                Log.Error(error, "Incoming Packet Network error");
            }
        }

        public static void SendPacket(string data)
        {
            try
            {
                var bytes = Encoding.ASCII.GetBytes(data);
                MyAPIGateway.Multiplayer.SendMessageToServer(8008, bytes);
            }
            catch (Exception error)
            {
                Log.Error(error, "Sending Packet Network error");
            }
        }
    }
}
