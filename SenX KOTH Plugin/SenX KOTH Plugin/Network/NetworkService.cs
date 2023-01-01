﻿using Sandbox.ModAPI;
using System;
using System.Text;
using NLog;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Network
{
    public class NetworkService
    {
        public static readonly Logger Log = LogManager.GetLogger("KOTH => Network Log");
        public static void NetworkInit()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8008, HandleIncomingPacket);
        }

        private static void HandleIncomingPacket(ushort comId, byte[] msg, ulong id, bool relible)
        {
            try
            {
                if (!msg.IsNullOrEmpty())
                {
                    var message = Encoding.ASCII.GetString(msg);
                    if (message.Equals("clear")) return;

                    if (SenX_KOTH_PluginMain.Instance.Config.CustomMessegeEnable)
                    {
                        DiscordService.SendDiscordWebHook(SenX_KOTH_PluginMain.Instance.Config.CustomMessege);
                    } else
                    {
                        DiscordService.SendDiscordWebHook(message);
                    }                    
                }
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
