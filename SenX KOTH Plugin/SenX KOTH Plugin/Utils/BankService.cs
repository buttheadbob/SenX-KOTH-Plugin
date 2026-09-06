using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Models;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class BankService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => BankService");
        private static readonly Random _rng = new();

        internal static string BankPath => Path.Combine(SenX_KOTH_PluginMain.DataPath, "FactionBanks.json");
        private static string RafflePath => Path.Combine(SenX_KOTH_PluginMain.DataPath, "RaffleTickets.json");

        public static async Task<string> AdminAdjustPointsAsync(string input, int value)
        {
            string localMsg = "";
            bool localSuccess = false;

            try
            {
                var (ok, _) = await SharedFile.ReadModifyWriteAsync<BanksData>(BankPath, data =>
                {
                    var d = data ?? new BanksData();
                    FactionBankEntry? entry;
                    if (long.TryParse(input, out long id))
                        entry = d.Banks.FirstOrDefault(b => b.FactionId == id);
                    else
                        entry = d.Banks.FirstOrDefault(b => string.Equals(b.FactionTag, input, StringComparison.OrdinalIgnoreCase));

                    if (entry == null) { localMsg = "Faction not found: " + input; return d; }
                    int before = entry.Points;
                    entry.Points += value;
                    string sign = value >= 0 ? "+" : "";
                    localMsg = entry.FactionTag + ": " + before + " -> " + entry.Points + " (" + sign + value + ")";
                    localSuccess = true;
                    return d;
                }, maxRetries: 0).ConfigureAwait(false);

                if (!ok && !localSuccess)
                    localMsg = "Data is busy, please try again.";
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Failed to adjust bank points.");
                localMsg = "Failed to adjust bank points.";
            }

            return localMsg;
        }

        public static async Task<(bool ok, string msg)> BuyTicketsAsync(long factionId, string factionName, string factionTag, int count, int ticketCost)
        {
            int totalCost = count * ticketCost;
            int balance = 0;
            string bankError = "";

            var (bankOk, _) = await SharedFile.ReadModifyWriteAsync<BanksData>(BankPath, data =>
            {
                var d = data ?? new BanksData();
                var bank = d.Banks.FirstOrDefault(b => b.FactionId == factionId);
                if (bank == null) { bankError = "Your faction has no bank points."; return d; }
                if (bank.Points < totalCost)
                { bankError = "Insufficient points. Cost is " + totalCost + " (" + count + " x " + ticketCost + "), you have " + bank.Points + "."; return d; }
                bank.Points -= totalCost;
                balance = bank.Points;
                return d;
            }, maxRetries: 0).ConfigureAwait(false);

            if (!bankOk)
                return (false, "Data is busy, please try again.");
            if (bankError.Length > 0)
                return (false, bankError);

            int totalTickets = 0;
            var (raffleOk, _) = await SharedFile.ReadModifyWriteAsync<RaffleTicketsData>(RafflePath, data =>
            {
                var d = data ?? new RaffleTicketsData();
                var ticket = d.Tickets.FirstOrDefault(t => t.FactionId == factionId);
                if (ticket == null)
                {
                    ticket = new RaffleTicketEntry { FactionId = factionId, FactionName = factionName, FactionTag = factionTag };
                    d.Tickets.Add(ticket);
                }
                ticket.Count += count;
                ticket.FactionName = factionName;
                ticket.FactionTag = factionTag;
                totalTickets = ticket.Count;
                return d;
            }, maxRetries: 0).ConfigureAwait(false);

            if (!raffleOk)
                return (false, "Points deducted but tickets could not be recorded. Contact an admin.");

            return (true, factionTag + " bought " + count + " ticket(s) for " + totalCost + " points. Balance: " + balance + ". Total tickets: " + totalTickets + ".");
        }

        public static async Task<(bool ok, int balance, int ticketCount)> GetBalanceAsync(long factionId)
        {
            var bankData = await SharedFile.ReadAsync<BanksData>(BankPath, maxRetries: 0).ConfigureAwait(false);
            if (!bankData.ok)
                return (false, 0, 0);

            int balance = 0;
            var bank = bankData.data?.Banks.FirstOrDefault(b => b.FactionId == factionId);
            if (bank != null) balance = bank.Points;

            var raffleData = await SharedFile.ReadAsync<RaffleTicketsData>(RafflePath, maxRetries: 0).ConfigureAwait(false);
            if (!raffleData.ok)
                return (false, 0, 0);

            int ticketCount = 0;
            var ticket = raffleData.data?.Tickets.FirstOrDefault(t => t.FactionId == factionId);
            if (ticket != null) ticketCount = ticket.Count;

            return (true, balance, ticketCount);
        }

        public static async Task<bool> CheckRaffleDrawAsync(SenX_KOTH_PluginConfig config)
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

            List<RaffleTicketEntry>? winners = null;
            bool drew = false;

            try
            {
                var (ok, _) = await SharedFile.ReadModifyWriteAsync<RaffleTicketsData>(RafflePath, data =>
                {
                    var raffleData = data ?? new RaffleTicketsData();
                    if (raffleData.LastDrawDate.Date == now.Date) return raffleData;

                    if (raffleData.Tickets.Count == 0)
                    {
                        raffleData.LastDrawDate = now.Date;
                        return raffleData;
                    }

                    var flatPool = new List<RaffleTicketEntry>();
                    foreach (var t in raffleData.Tickets)
                        for (int i = 0; i < t.Count; i++) flatPool.Add(t);

                    var won = new List<RaffleTicketEntry>();
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
                            poolCopy.RemoveAt(idx); drawAttempts++;
                        }
                        if (picked == null && poolCopy.Count > 0) picked = poolCopy[0];
                        if (picked != null) { won.Add(picked); usedFactions.Add(picked.FactionId); }
                    }

                    winners = won;
                    drew = won.Count > 0;
                    raffleData.Tickets.Clear();
                    raffleData.LastDrawDate = now.Date;
                    return raffleData;
                }).ConfigureAwait(false);

                if (!ok) return false;
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Failed to run raffle draw.");
                return false;
            }

            if (drew && winners != null) AnnounceDrawResults(winners, config);
            return drew;
        }

        private static void AnnounceDrawResults(List<RaffleTicketEntry> winners, SenX_KOTH_PluginConfig config)
        {
            for (int i = 0; i < winners.Count && i < 3; i++)
            {
                var winner = winners[i];
                string place = i == 0 ? "FIRST" : i == 1 ? "SECOND" : "THIRD";
                string msg = place + " PLACE Raffle Winner: [" + winner.FactionTag + "] " + winner.FactionName;
                var color = i == 0 ? System.Drawing.Color.Gold : i == 1 ? System.Drawing.Color.Silver : System.Drawing.Color.SandyBrown;
                AnnouncementService.RaffleWinner(msg, color);

                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(winner.FactionId);
                if (faction == null) continue;

                var rewards = i == 0 ? config.RaffleFirstRewards.ToList()
                    : i == 1 ? config.RaffleSecondRewards.ToList()
                    : config.RaffleThirdRewards.ToList();

                foreach (var cmd in rewards)
                {
                    if (string.IsNullOrEmpty(cmd.CommandText)) continue;
                    RewardService.ExecutePlayerCommands(cmd.CommandText, faction, faction.Members.Keys, cmd.OnlyOnlineMembers);
                }
            }
        }
    }
}
