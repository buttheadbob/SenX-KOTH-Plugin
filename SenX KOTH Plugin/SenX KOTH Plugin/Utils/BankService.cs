using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Sandbox.ModAPI;
using SenX_KOTH_Plugin.Models;
using VRage.Game.ModAPI;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class BankService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => BankService");
        private static readonly ReaderWriterLockSlim _fileLock = new();
        private static readonly Random _rng = new();

        private static string BankPath => Path.Combine(SenX_KOTH_PluginMain.DataPath, "FactionBanks.json");
        private static string RafflePath => Path.Combine(SenX_KOTH_PluginMain.DataPath, "RaffleTickets.json");

        // --- File helpers ---

        private static T ReadFile<T>(string path) where T : new()
        {
            _fileLock.EnterReadLock();
            try
            {
                if (!File.Exists(path)) return new T();
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) ?? new T();
            }
            finally { _fileLock.ExitReadLock(); }
        }

        private static void WriteFile<T>(string path, T data)
        {
            _fileLock.EnterWriteLock();
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            finally { _fileLock.ExitWriteLock(); }
        }

        private static void AtomicModifyBank(Action<BanksData> modify)
        {
            _fileLock.EnterWriteLock();
            try
            {
                var data = File.Exists(BankPath)
                    ? JsonConvert.DeserializeObject<BanksData>(File.ReadAllText(BankPath)) ?? new BanksData()
                    : new BanksData();
                modify(data);
                var dir = Path.GetDirectoryName(BankPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(BankPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to modify bank file."); }
            finally { _fileLock.ExitWriteLock(); }
        }

        private static void AtomicModifyRaffle(Action<RaffleTicketsData> modify)
        {
            _fileLock.EnterWriteLock();
            try
            {
                var data = File.Exists(RafflePath)
                    ? JsonConvert.DeserializeObject<RaffleTicketsData>(File.ReadAllText(RafflePath)) ?? new RaffleTicketsData()
                    : new RaffleTicketsData();
                modify(data);
                var dir = Path.GetDirectoryName(RafflePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(RafflePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to modify raffle file."); }
            finally { _fileLock.ExitWriteLock(); }
        }

        // --- Public API ---

        public static void CreditPoints(long factionId, string factionName, string factionTag, int points)
        {
            if (points <= 0) return;
            AtomicModifyBank(data =>
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
            });
        }

        public static FactionBankEntry? GetBank(long factionId)
        {
            _fileLock.EnterReadLock();
            try
            {
                if (!File.Exists(BankPath)) return null;
                var data = JsonConvert.DeserializeObject<BanksData>(File.ReadAllText(BankPath));
                if (data == null) return null;
                return data.Banks.FirstOrDefault(b => b.FactionId == factionId);
            }
            finally { _fileLock.ExitReadLock(); }
        }

        public static FactionBankEntry? GetBankByTagOrId(string input)
        {
            _fileLock.EnterReadLock();
            try
            {
                if (!File.Exists(BankPath)) return null;
                var data = JsonConvert.DeserializeObject<BanksData>(File.ReadAllText(BankPath));
                if (data == null) return null;
                if (long.TryParse(input, out long id))
                    return data.Banks.FirstOrDefault(b => b.FactionId == id);
                return data.Banks.FirstOrDefault(b => string.Equals(b.FactionTag, input, StringComparison.OrdinalIgnoreCase));
            }
            finally { _fileLock.ExitReadLock(); }
        }

        public static bool AdminAdjustPoints(string input, int value, out string resultMsg)
        {
            var localMsg = "";
            var localSuccess = false;
            AtomicModifyBank(data =>
            {
                FactionBankEntry? entry;
                if (long.TryParse(input, out long id))
                    entry = data.Banks.FirstOrDefault(b => b.FactionId == id);
                else
                    entry = data.Banks.FirstOrDefault(b => string.Equals(b.FactionTag, input, StringComparison.OrdinalIgnoreCase));

                if (entry == null) { localMsg = "Faction not found: " + input; return; }
                int before = entry.Points;
                entry.Points += value;
                string sign = value >= 0 ? "+" : "";
                localMsg = entry.FactionTag + ": " + before + " -> " + entry.Points + " (" + sign + value + ")";
                localSuccess = true;
            });
            resultMsg = localMsg;
            return localSuccess;
        }

        public static bool BuyTickets(SenX_KOTH_PluginConfig config, long playerIdentityId, int count, out string resultMsg)
        {
            resultMsg = "";
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerIdentityId);
            if (faction == null) { resultMsg = "You are not in a faction."; return false; }
            if (!faction.IsFounder(playerIdentityId) && !faction.IsLeader(playerIdentityId))
            { resultMsg = "Only faction founders and leaders can buy tickets."; return false; }

            int ticketCost = Math.Max(1, config.TicketCost);
            int totalCost = count * ticketCost;
            bool success = false;

            _fileLock.EnterWriteLock();
            try
            {
                var bankData = File.Exists(BankPath)
                    ? JsonConvert.DeserializeObject<BanksData>(File.ReadAllText(BankPath)) ?? new BanksData()
                    : new BanksData();
                var raffleData = File.Exists(RafflePath)
                    ? JsonConvert.DeserializeObject<RaffleTicketsData>(File.ReadAllText(RafflePath)) ?? new RaffleTicketsData()
                    : new RaffleTicketsData();

                var bank = bankData.Banks.FirstOrDefault(b => b.FactionId == faction.FactionId);
                if (bank == null) { resultMsg = "Your faction has no bank points."; return false; }
                if (bank.Points < totalCost)
                { resultMsg = "Insufficient points. Cost is " + totalCost + " (" + count + " x " + ticketCost + "), you have " + bank.Points + "."; return false; }

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
                success = true;

                var dir = Path.GetDirectoryName(BankPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(BankPath, JsonConvert.SerializeObject(bankData, Formatting.Indented));
                File.WriteAllText(RafflePath, JsonConvert.SerializeObject(raffleData, Formatting.Indented));
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to buy tickets."); }
            finally { _fileLock.ExitWriteLock(); }

            return success;
        }

        public static bool CheckRaffleDraw(SenX_KOTH_PluginConfig config)
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

            _fileLock.EnterWriteLock();
            try
            {
                if (!File.Exists(RafflePath)) return false;
                var raffleData = JsonConvert.DeserializeObject<RaffleTicketsData>(File.ReadAllText(RafflePath));
                if (raffleData == null) return false;

                if (raffleData.LastDrawDate.Date == now.Date) return false;
                if (raffleData.Tickets.Count == 0) { raffleData.LastDrawDate = now.Date; WriteFile(RafflePath, raffleData); return false; }

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
                        poolCopy.RemoveAt(idx); drawAttempts++;
                    }
                    if (picked == null && poolCopy.Count > 0) picked = poolCopy[0];
                    if (picked != null) { winners.Add(picked); usedFactions.Add(picked.FactionId); }
                }

                if (winners.Count > 0) AnnounceDrawResults(winners, config);
                raffleData.Tickets.Clear();
                raffleData.LastDrawDate = now.Date;

                var dir = Path.GetDirectoryName(RafflePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(RafflePath, JsonConvert.SerializeObject(raffleData, Formatting.Indented));
            }
            catch (Exception ex) { KoTHLog.Error(Log, ex, "Failed to run raffle draw."); }
            finally { _fileLock.ExitWriteLock(); }

            return true;
        }

        public static (int balance, int ticketCount) GetBalance(long factionId)
        {
            int balance = 0, ticketCount = 0;
            _fileLock.EnterReadLock();
            try
            {
                if (File.Exists(BankPath))
                {
                    var bankData = JsonConvert.DeserializeObject<BanksData>(File.ReadAllText(BankPath));
                    if (bankData != null)
                    {
                        var bank = bankData.Banks.FirstOrDefault(b => b.FactionId == factionId);
                        if (bank != null) balance = bank.Points;
                    }
                }
                if (File.Exists(RafflePath))
                {
                    var raffleData = JsonConvert.DeserializeObject<RaffleTicketsData>(File.ReadAllText(RafflePath));
                    if (raffleData != null)
                    {
                        var ticket = raffleData.Tickets.FirstOrDefault(t => t.FactionId == factionId);
                        if (ticket != null) ticketCount = ticket.Count;
                    }
                }
            }
            finally { _fileLock.ExitReadLock(); }
            return (balance, ticketCount);
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
                            KoTHLog.Info(Log, "Raffle Reward Command: " + c);
                        }
                    }
                    else
                    {
                        string c = cmd.CommandText.Replace("{factionid}", faction.FactionId.ToString())
                            .Replace("{factionname}", faction.Name)
                            .Replace("{factiontag}", faction.Tag);
                        KoTHLog.Info(Log, "Raffle Reward Command: " + c);
                    }
                }
            }
        }
    }
}
