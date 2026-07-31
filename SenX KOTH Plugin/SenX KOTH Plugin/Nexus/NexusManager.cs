using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Discord;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;
using Timer = System.Timers.Timer;

namespace SenX_KOTH_Plugin.Nexus
{
    internal static class NexusManager
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => NexusManager");
        public const ushort NexusChannelId = 50672;

        private static byte _authorityId;
        private static Timer? _authorityAnnounceTimer;
        private static Timer? _syncRequestTimer;
        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<AuthorityResponse>> _pending = new();

        public static bool HasAuthority => _authorityId != 0;

        public static bool IsAuthorityLocal()
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.IsDataModeNexus != true) return false;
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            return api is { Enabled: true };
        }

        public static void Initialize()
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true } || config == null) return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NexusChannelId, HandleNexusMessage);
            KoTHLog.Info(Log, "Nexus message handler registered. Authority=" + IsAuthorityLocal());

            if (IsAuthorityLocal())
            {
                _authorityAnnounceTimer = new Timer(30000);
                _authorityAnnounceTimer.Elapsed += (_, _) => BroadcastAuthorityAnnouncement();
                _authorityAnnounceTimer.Start();

                _syncRequestTimer = new Timer(900000);
                _syncRequestTimer.Elapsed += (_, _) => BroadcastSyncRequest();
                _syncRequestTimer.Start();

                BroadcastAuthorityAnnouncement();
            }
        }

        public static void Shutdown()
        {
            _authorityAnnounceTimer?.Dispose();
            _authorityAnnounceTimer = null;
            _syncRequestTimer?.Dispose();
            _syncRequestTimer = null;

            if (SenX_KOTH_PluginMain.NexusGlobalAPI is { Enabled: true })
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NexusChannelId, HandleNexusMessage);
        }

        // --- Authority broadcasts ---

        private static void BroadcastAuthorityAnnouncement()
        {
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;
            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(new AuthorityAnnouncement());
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to broadcast authority announcement."); }
        }

        private static void BroadcastSyncRequest()
        {
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;
            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(new SyncRequest());
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to broadcast sync request."); }
        }

        // --- Non-authority sends ---

        public static void SaveAndSendPointCredit(PointCreditEntry entry)
        {
            var persist = SenX_KOTH_PluginMain.PendingCreditsPersist;
            if (persist != null)
            {
                persist.Data.Credits.Add(entry);
                persist.Save();
            }
            if (_authorityId != 0)
                SendPointCredit(entry);
        }

        private static void SendPointCredit(PointCreditEntry entry)
        {
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true } || _authorityId == 0) return;
            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(entry);
                api.SendModMsgToServer(data, NexusChannelId, _authorityId);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to send point credit."); }
        }

        public static async Task<AuthorityResponse> SendToAuthority<T>(T request) where T : class
        {
            if (_authorityId == 0)
                return new AuthorityResponse { Approved = false, Message = "No authority server available." };

            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true })
                return new AuthorityResponse { Approved = false, Message = "Nexus not available." };

            var requestId = (Guid)typeof(T).GetProperty("RequestId")!.GetValue(request)!;
            var tcs = new TaskCompletionSource<AuthorityResponse>();
            _pending[requestId] = tcs;

            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(request);
                api.SendModMsgToServer(data, NexusChannelId, _authorityId);

                var timeout = Task.Delay(10_000);
                if (await Task.WhenAny(tcs.Task, timeout) == timeout)
                    return new AuthorityResponse { Approved = false, Message = "Bank unavailable - try again later." };

                return await tcs.Task;
            }
            catch
            {
                return new AuthorityResponse { Approved = false, Message = "Bank unavailable - try again later." };
            }
            finally
            {
                _pending.TryRemove(requestId, out _);
            }
        }

        // --- Discord relay broadcasts (unchanged) ---

        public static void BroadcastDiscordZoneRelay(string zoneName, ProtoEmbed embed, ulong knownChannelId)
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;
            try
            {
                var relay = new DiscordZoneRelay { FromServerID = api.CurrentServerID, ZoneName = zoneName, Embed = embed, KnownChannelId = knownChannelId };
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(relay);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to broadcast zone relay."); }
        }

        public static void BroadcastDiscordRewardRelay(string title, string description, uint color)
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;
            try
            {
                var relay = new DiscordRewardRelay { FromServerID = api.CurrentServerID, Embed = new ProtoEmbed { Title = title, Description = description, Color = color } };
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(relay);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to broadcast reward relay."); }
        }

        private static void BroadcastDiscordChannelResponse(byte targetServer, string zoneName, ulong channelId)
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config?.NexusEnabled != true) return;
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;
            try
            {
                var resp = new DiscordChannelResponse { FromServerID = api.CurrentServerID, TargetServerID = targetServer, ZoneName = zoneName, ChannelId = channelId };
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(resp);
                api.SendModMsgToServer(data, NexusChannelId, targetServer);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to send channel response."); }
        }

        // --- Message handler ---

        private static void HandleNexusMessage(ushort handlerId, byte[] data, ulong steamId, bool fromServer)
        {
            try
            {
                var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
                if (api is not { Enabled: true }) return;

                var msg = MyAPIGateway.Utilities.SerializeFromBinary<NexusGlobalAPI.ModAPIMsg>(data);
                if (msg?.msgData == null) return;
                if (msg.fromServerID == api.CurrentServerID) return;

                if (IsAuthorityLocal())
                    HandleAsAuthority(msg);
                else
                    HandleAsNonAuthority(msg);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Error handling Nexus message."); }
        }

        // --- Authority handlers ---

        private static void HandleAsAuthority(NexusGlobalAPI.ModAPIMsg msg)
        {
            TryHandlePointCredit(msg);
            TryHandleTicketPurchase(msg);
            TryHandleBankBalance(msg);
            TryHandleDiscordZoneRelay(msg);
            TryHandleDiscordRewardRelay(msg);
            TryHandleDiscordChannelResponse(msg);
        }

        private static void TryHandlePointCredit(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var entry = MyAPIGateway.Utilities.SerializeFromBinary<PointCreditEntry>(msg.msgData);
                if (entry == null) return;

                KoTHLog.Info(Log, "Point credit: " + entry.FactionTag + " +" + entry.Points + " in " + entry.ZoneName);
                BankService.CreditPoints(entry.FactionId, entry.FactionName, entry.FactionTag, entry.Points);

                // Update event data (local file)
                var eventPath = Path.Combine(SenX_KOTH_PluginMain.LocalDataPath, "EventData.json");
                EventData eventData;
                if (File.Exists(eventPath))
                    eventData = Newtonsoft.Json.JsonConvert.DeserializeObject<EventData>(File.ReadAllText(eventPath)) ?? new EventData();
                else
                    eventData = new EventData();

                eventData.WeekEvents.Add(new PointEarned
                {
                    Points = entry.Points, FactionId = entry.FactionId, FactionName = entry.FactionName, FactionTag = entry.FactionTag,
                    FromServerID = msg.fromServerID, EarnedAt = entry.EarnedAt, ZoneName = entry.ZoneName
                });

                var dir = Path.GetDirectoryName(eventPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(eventPath, Newtonsoft.Json.JsonConvert.SerializeObject(eventData, Newtonsoft.Json.Formatting.Indented));

                // Update scores
                var scorePath = Path.Combine(SenX_KOTH_PluginMain.DataPath, "ScoreData.json");
                ScoreFile scores;
                if (File.Exists(scorePath))
                    scores = Newtonsoft.Json.JsonConvert.DeserializeObject<ScoreFile>(File.ReadAllText(scorePath)) ?? new ScoreFile();
                else
                    scores = new ScoreFile();

                var existing = scores.WeekScores.FirstOrDefault(s => s.Key == entry.FactionName);
                if (existing.Key != null)
                {
                    var idx = scores.WeekScores.IndexOf(existing);
                    scores.WeekScores[idx] = new KeyValuePair<string, ulong>(entry.FactionName, existing.Value + (ulong)entry.Points);
                }
                else
                    scores.WeekScores.Add(new KeyValuePair<string, ulong>(entry.FactionName, (ulong)entry.Points));

                File.WriteAllText(scorePath, Newtonsoft.Json.JsonConvert.SerializeObject(scores, Newtonsoft.Json.Formatting.Indented));

                SendAuthorityResponse(entry.RequestId, true, "Credit applied.", entry.FactionTag, entry.FactionName, 0, 0);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle point credit."); }
        }

        private static void TryHandleTicketPurchase(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var req = MyAPIGateway.Utilities.SerializeFromBinary<TicketPurchaseRequest>(msg.msgData);
                if (req == null) return;

                var config = SenX_KOTH_PluginMain.Instance?.Config;
                if (config == null) return;

                if (BankService.BuyTickets(config, req.PlayerIdentityId, req.Count, out string resultMsg))
                {
                    var faction = Sandbox.ModAPI.MyAPIGateway.Session.Factions.TryGetPlayerFaction(req.PlayerIdentityId);
                    var (balance, ticketCount) = faction != null ? BankService.GetBalance(faction.FactionId) : (0, 0);
                    SendAuthorityResponse(req.RequestId, true, resultMsg, faction?.Tag ?? "", faction?.Name ?? "", balance, ticketCount);
                }
                else
                {
                    SendAuthorityResponse(req.RequestId, false, resultMsg, "", "", 0, 0);
                }
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle ticket purchase."); }
        }

        private static void TryHandleBankBalance(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var req = MyAPIGateway.Utilities.SerializeFromBinary<BankBalanceRequest>(msg.msgData);
                if (req == null) return;

                var faction = Sandbox.ModAPI.MyAPIGateway.Session.Factions.TryGetPlayerFaction(req.PlayerIdentityId);
                if (faction == null)
                {
                    SendAuthorityResponse(req.RequestId, false, "Not in a faction.", "", "", 0, 0);
                    return;
                }

                var (balance, ticketCount) = BankService.GetBalance(faction.FactionId);
                SendAuthorityResponse(req.RequestId, true, "", faction.Tag, faction.Name, balance, ticketCount);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle balance query."); }
        }

        // --- Non-authority handlers ---

        private static void HandleAsNonAuthority(NexusGlobalAPI.ModAPIMsg msg)
        {
            TryHandleAuthorityAnnouncement(msg);
            TryHandleSyncRequest(msg);
            TryHandleAuthorityResponse(msg);
            TryHandleDiscordZoneRelay(msg);
            TryHandleDiscordRewardRelay(msg);
            TryHandleDiscordChannelResponse(msg);
        }

        private static void TryHandleAuthorityAnnouncement(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var announcement = MyAPIGateway.Utilities.SerializeFromBinary<AuthorityAnnouncement>(msg.msgData);
                if (announcement == null) return;

                _authorityId = msg.fromServerID;
                KoTHLog.Info(Log, "Authority discovered: server " + _authorityId);
                FlushPendingCredits();
            }
            catch { }
        }

        private static void TryHandleSyncRequest(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var req = MyAPIGateway.Utilities.SerializeFromBinary<SyncRequest>(msg.msgData);
                if (req == null) return;
                KoTHLog.Info(Log, "Sync request received, flushing pending credits.");
                FlushPendingCredits();
            }
            catch { }
        }

        private static void TryHandleAuthorityResponse(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var resp = MyAPIGateway.Utilities.SerializeFromBinary<AuthorityResponse>(msg.msgData);
                if (resp == null) return;

                if (_pending.TryRemove(resp.RequestId, out var tcs))
                    tcs.TrySetResult(resp);

                var persist = SenX_KOTH_PluginMain.PendingCreditsPersist;
                if (persist != null)
                {
                    int before = persist.Data.Credits.Count;
                    persist.Data.Credits.RemoveAll(c => c.RequestId == resp.RequestId);
                    if (persist.Data.Credits.Count < before) persist.Save();
                }
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle authority response."); }
        }

        private static void FlushPendingCredits()
        {
            var persist = SenX_KOTH_PluginMain.PendingCreditsPersist;
            if (persist == null) return;
            foreach (var entry in persist.Data.Credits.ToList())
                SendPointCredit(entry);
        }

        // --- Discord relay handlers (unchanged) ---

        private static void TryHandleDiscordZoneRelay(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var relay = MyAPIGateway.Utilities.SerializeFromBinary<DiscordZoneRelay>(msg.msgData);
                if (relay == null) return;

                var config = SenX_KOTH_PluginMain.Instance?.Config;
                if (config?.NexusReceiveDiscord != true) return;

                KoTHLog.Info(Log, "Received zone relay from server " + relay.FromServerID + " for " + relay.ZoneName);
                _ = DiscordBotService.PostRelayedZoneEmbedAsync(relay).ContinueWith(async t =>
                {
                    var newChannelId = await t;
                    if (newChannelId != 0)
                        BroadcastDiscordChannelResponse(relay.FromServerID, relay.ZoneName, newChannelId);
                });
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle zone relay."); }
        }

        private static void TryHandleDiscordRewardRelay(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var relay = MyAPIGateway.Utilities.SerializeFromBinary<DiscordRewardRelay>(msg.msgData);
                if (relay == null) return;

                var config = SenX_KOTH_PluginMain.Instance?.Config;
                if (config?.NexusReceiveDiscord != true) return;

                KoTHLog.Info(Log, "Received reward relay from server " + relay.FromServerID);
                _ = DiscordBotService.PostRelayedRewardAsync(relay);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle reward relay."); }
        }

        private static void TryHandleDiscordChannelResponse(NexusGlobalAPI.ModAPIMsg msg)
        {
            try
            {
                var resp = MyAPIGateway.Utilities.SerializeFromBinary<DiscordChannelResponse>(msg.msgData);
                if (resp == null) return;

                var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
                if (api is not { Enabled: true } || resp.TargetServerID != api.CurrentServerID) return;

                KoTHLog.Info(Log, "Received channel response for " + resp.ZoneName + " channelId=" + resp.ChannelId);
                var zonePersist = SenX_KOTH_PluginMain.ZonePersist;
                var zone = zonePersist?.Data.Zones.FirstOrDefault(z => string.Equals(z.Name, resp.ZoneName, StringComparison.OrdinalIgnoreCase));
                if (zone != null) { zone.DiscordChannelId = resp.ChannelId; zonePersist!.Save(); }
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to handle channel response."); }
        }

        // --- Helpers ---

        private static void SendAuthorityResponse(Guid requestId, bool approved, string message,
            string factionTag, string factionName, int balance, int ticketCount)
        {
            var api = SenX_KOTH_PluginMain.NexusGlobalAPI;
            if (api is not { Enabled: true }) return;
            try
            {
                var resp = new AuthorityResponse
                {
                    RequestId = requestId, Approved = approved, Message = message,
                    FactionTag = factionTag, FactionName = factionName,
                    Balance = balance, TicketCount = ticketCount
                };
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(resp);
                api.SendModMsgToAllServers(data, NexusChannelId);
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to send authority response."); }
        }
    }
}
