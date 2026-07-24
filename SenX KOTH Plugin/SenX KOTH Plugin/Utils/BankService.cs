using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin;
using VRage.Game.ModAPI;
using DrawingColor = System.Drawing.Color;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class BankService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => BankService");
        private static readonly object _bankLock = new();
        private static readonly Random _rng = new();

        public static void CreditPoints(BanksData data, long factionId, string factionName, string factionTag, int points)
        {
            if (points <= 0) return;
            lock (_bankLock)
            {
                var entry = data.Banks.FirstOrDefault(b => b.FactionId == factionId);
                if (entry == null)
                {
                    entry = new FactionBankEntry { FactionId = factionId, FactionName = factionName, FactionTag = factionTag };
                    data.Banks.Add(entry);
                }
                entry.FactionName = factionName;
                entry.FactionTag = factionTag;
                entry.Points += points;
                SenX_KOTH_PluginMain.BankPersist?.Save();
            }
        }

        public static FactionBankEntry? GetBank(BanksData data, long factionId)
        {
            lock (_bankLock) return data.Banks.FirstOrDefault(b => b.FactionId == factionId);
        }

        public static FactionBankEntry? GetBankByTagOrId(BanksData data, string input)
        {
            lock (_bankLock)
            {
                if (long.TryParse(input, out long id)) return data.Banks.FirstOrDefault(b => b.FactionId == id);
                return data.Banks.FirstOrDefault(b => string.Equals(b.FactionTag, input, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool AdminAdjustPoints(BanksData data, string input, int value, out string resultMsg)
        {
            lock (_bankLock)
            {
                var entry = GetBankByTagOrId(data, input);
                if (entry == null) { resultMsg = "Faction not found: " + input; return false; }
                int before = entry.Points;
                entry.Points += value;
                string sign = value >= 0 ? "+" : "";
                resultMsg = entry.FactionTag + ": " + before + " \u2192 " + entry.Points + " (" + sign + value + ")";
                return true;
            }
        }

        public static bool BuyTickets(BanksData bankData, RaffleTicketsData raffleData, SenX_KOTH_PluginConfig config,
            long playerIdentityId, int count, out string resultMsg)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerIdentityId);
            if (faction == null) { resultMsg = "You are not in a faction."; return false; }
            if (!faction.IsFounder(playerIdentityId) && !faction.IsLeader(playerIdentityId))
            { resultMsg = "Only faction founders and leaders can buy tickets."; return false; }

            int ticketCost = Math.Max(1, config.TicketCost);
            int totalCost = count * ticketCost;

            lock (_bankLock)
            {
                var bank = bankData.Banks.FirstOrDefault(b => b.FactionId == faction.FactionId);
                if (bank == null) { resultMsg = "Your faction has no bank points. Earn points by controlling zones."; return false; }
                if (bank.Points < totalCost)
                { resultMsg = "Insufficient points. Cost is " + totalCost + " (" + count + " \u00d7 " + ticketCost + "), you have " + bank.Points + "."; return false; }

                bank.Points -= totalCost;
                var ticket = raffleData.Tickets.FirstOrDefault(t => t.FactionId == faction.FactionId);
                if (ticket == null)
                {
                    ticket = new RaffleTicketEntry { FactionId = faction.FactionId, FactionName = faction.Name, FactionTag = faction.Tag };
                    raffleData.Tickets.Add(ticket);
                }
                ticket.Count += count;
                ticket.FactionName = faction.Name;
                ticket.FactionTag = faction.Tag;
                resultMsg = faction.Tag + " bought " + count + " ticket(s) for " + totalCost + " points. Balance: " + bank.Points + ". Total tickets: " + ticket.Count + ".";
            }
            return true;
        }

        public static bool CheckRaffleDraw(SenX_KOTH_PluginConfig config, BanksData bankData, RaffleTicketsData raffleData)
        {
            if (!config.RaffleEnabled) return false;
            DateTime now = DateTime.Now;
            DateTime drawTime;

            if (config.RafflePeriod == RafflePeriod.Weekly)
            {
                var today = now.Date;
                int daysUntil = ((int)config.RaffleDayOfWeek - (int)today.DayOfWeek + 7) % 7;
                drawTime = today.AddDays(daysUntil == 0 ? 0 : daysUntil).AddHours(config.RaffleHour).AddMinutes(config.RaffleMinute);
            }
            else
            {
                var drawDate = new DateTime(now.Year, now.Month, Math.Min(config.RaffleDayOfMonth, DateTime.DaysInMonth(now.Year, now.Month)));
                drawTime = drawDate.AddHours(config.RaffleHour).AddMinutes(config.RaffleMinute);
                if (drawTime < now)
                {
                    var nextMonth = now.AddMonths(1);
                    drawDate = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(config.RaffleDayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));
                    drawTime = drawDate.AddHours(config.RaffleHour).AddMinutes(config.RaffleMinute);
                }
            }

            if (now < drawTime) return false;

            lock (_bankLock)
            {
                if (raffleData.LastDrawDate.Date == now.Date) return false;
                if (raffleData.Tickets.Count == 0) { raffleData.LastDrawDate = now.Date; return false; }

                var flatPool = new List<RaffleTicketEntry>();
                foreach (var t in raffleData.Tickets)
                    for (int i = 0; i < t.Count; i++) flatPool.Add(t);

                var winners = new List<RaffleTicketEntry>();
                var usedFactions = new HashSet<long>();
                var poolCopy = new List<RaffleTicketEntry>(flatPool);

                for (int place = 0; place < 3 && poolCopy.Count > 0; place++)
                {
                    int drawAttempts = 0;
                    RaffleTicketEntry? picked = null;
                    while (drawAttempts < 100 && poolCopy.Count > 0)
                    {
                        int idx = _rng.Next(poolCopy.Count);
                        var candidate = poolCopy[idx];
                        if (!usedFactions.Contains(candidate.FactionId)) { picked = candidate; break; }
                        poolCopy.RemoveAt(idx);
                        drawAttempts++;
                    }
                    if (picked == null && poolCopy.Count > 0) picked = poolCopy[0];
                    if (picked != null) { winners.Add(picked); usedFactions.Add(picked.FactionId); }
                }

                if (winners.Count > 0) AnnounceDrawResults(winners, config, bankData);
                raffleData.Tickets.Clear();
                raffleData.LastDrawDate = now.Date;
            }
            return true;
        }

        private static void AnnounceDrawResults(List<RaffleTicketEntry> winners, SenX_KOTH_PluginConfig config, BanksData bankData)
        {
            for (int i = 0; i < winners.Count && i < 3; i++)
            {
                var winner = winners[i];
                string place = i == 0 ? "FIRST" : i == 1 ? "SECOND" : "THIRD";
                string msg = place + " PLACE Raffle Winner: [" + winner.FactionTag + "] " + winner.FactionName;
                DrawingColor color = i == 0 ? System.Drawing.Color.Gold : i == 1 ? System.Drawing.Color.Silver : System.Drawing.Color.SandyBrown;
                DiscordService.SendDiscordWebHook(msg, color, 1);

                IMyFaction? faction = null;
                MyAPIGateway.Session.Factions.Factions.TryGetValue(winner.FactionId, out faction);
                if (faction == null) continue;

                var rewards = i == 0 ? config.RaffleFirstRewards.ToList()
                    : i == 1 ? config.RaffleSecondRewards.ToList()
                    : config.RaffleThirdRewards.ToList();

                foreach (var cmd in rewards)
                {
                    if (string.IsNullOrEmpty(cmd.CommandText)) continue;
                    if (cmd.PerFactionMember)
                    {
                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        foreach (var p in players)
                        {
                            var pf = MyAPIGateway.Session.Factions.TryGetPlayerFaction(p.IdentityId);
                            if (pf == null || pf.FactionId != faction.FactionId) continue;
                            if (cmd.OnlyOnlineMembers && p.Character == null) continue;
                            string c = cmd.CommandText.Replace("{playerid}", p.SteamUserId.ToString())
                                .Replace("{factionid}", faction.FactionId.ToString())
                                .Replace("{factionname}", faction.Name)
                                .Replace("{factiontag}", faction.Tag);
                            Log.Info("Raffle Reward Command: " + c);
                        }
                    }
                    else
                    {
                        string c = cmd.CommandText.Replace("{factionid}", faction.FactionId.ToString())
                            .Replace("{factionname}", faction.Name)
                            .Replace("{factiontag}", faction.Tag);
                        Log.Info("Raffle Reward Command: " + c);
                    }
                }
            }
        }
    }
}
