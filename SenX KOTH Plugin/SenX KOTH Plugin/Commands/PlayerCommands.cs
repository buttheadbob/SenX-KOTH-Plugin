using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NLog;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Commands
{
    [Category("KoTH")]
    public sealed class KothPlayerCommands : CommandModule
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => PlayerCommands");

        private bool CheckCooldown()
        {
            if (Context.Player == null) return true;
            if (!CommandCooldown.TryUse(Context.Player.SteamUserId))
            {
                Context.Respond("Please wait 5 seconds before using another command.");
                return false;
            }
            return true;
        }

        [Command("Week", "Shows the current KoTH ranking and points for the week.")]
        [Permission(MyPromoteLevel.None)]
        public async void ShowWeek()
        {
            if (!CheckCooldown()) return;

            try
            {
                var (scoreOk, scores) = await SenX_KOTH_PluginMain.LoadScoreFileAsync();
                if (!scoreOk)
                {
                    GameThread.Invoke(() => Context.Respond("Data is busy, please try again."), "KoTH");
                    return;
                }

                var weekList = (scores?.WeekScores ?? new List<KeyValuePair<long, ulong>>())
                    .OrderByDescending(x => x.Value).ToList();

                var results = new StringBuilder();
                results.AppendLine();
                string prefix = "Standing Weekly Results";

                if (weekList.Count > 0)
                {
                    results.AppendLine("=== Weekly Leaderboard ===");
                    foreach (var result in weekList)
                        results.AppendLine(FactionLookup.GetName(result.Key) + " => " + result.Value);

                    var (eventOk, scoreData) = await SenX_KOTH_PluginMain.LoadEventDataAsync();
                    if (eventOk && scoreData != null)
                        AppendZoneBreakdown(results, scoreData.WeekEvents, "Weekly Zone Breakdown");
                }
                else
                    results.AppendLine("No results for this week to show.");

                string message = results.ToString();
                GameThread.Invoke(() => SendResult(message, prefix), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Week command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }

        [Command("Month", "Shows the current KoTH ranking and points for the month.")]
        [Permission(MyPromoteLevel.None)]
        public async void ShowMonth()
        {
            if (!CheckCooldown()) return;

            try
            {
                var (scoreOk, scores) = await SenX_KOTH_PluginMain.LoadScoreFileAsync();
                if (!scoreOk)
                {
                    GameThread.Invoke(() => Context.Respond("Data is busy, please try again."), "KoTH");
                    return;
                }

                var monthList = (scores?.MonthScores ?? new List<KeyValuePair<long, ulong>>())
                    .OrderByDescending(x => x.Value).ToList();

                var results = new StringBuilder();
                results.AppendLine();
                string prefix = "Standing Results for " + DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);

                if (monthList.Count > 0)
                {
                    results.AppendLine("=== Monthly Leaderboard ===");
                    foreach (var result in monthList)
                        results.AppendLine(FactionLookup.GetName(result.Key) + " => " + result.Value);

                    var (eventOk, scoreData) = await SenX_KOTH_PluginMain.LoadEventDataAsync();
                    if (eventOk && scoreData != null)
                        AppendZoneBreakdown(results, scoreData.MonthEvents, "Monthly Zone Breakdown");
                }
                else
                    results.AppendLine("No results for the month.. now's your chance!");

                string message = results.ToString();
                GameThread.Invoke(() => SendResult(message, prefix), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Month command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }

        [Command("Year", "Shows the current KoTH ranking and points for the year.")]
        [Permission(MyPromoteLevel.None)]
        public async void ShowYear()
        {
            if (!CheckCooldown()) return;

            try
            {
                var (scoreOk, scores) = await SenX_KOTH_PluginMain.LoadScoreFileAsync();
                if (!scoreOk)
                {
                    GameThread.Invoke(() => Context.Respond("Data is busy, please try again."), "KoTH");
                    return;
                }

                var yearList = (scores?.YearScores ?? new List<KeyValuePair<long, ulong>>())
                    .OrderByDescending(x => x.Value).ToList();

                var results = new StringBuilder();
                results.AppendLine();
                string prefix = "Standing Results for " + DateTime.Now.Year;

                if (yearList.Count > 0)
                {
                    results.AppendLine("=== Yearly Leaderboard ===");
                    foreach (var result in yearList)
                        results.AppendLine(FactionLookup.GetName(result.Key) + " => " + result.Value);

                    var (eventOk, scoreData) = await SenX_KOTH_PluginMain.LoadEventDataAsync();
                    if (eventOk && scoreData != null)
                        AppendZoneBreakdown(results, scoreData.YearEvents, "Yearly Zone Breakdown");
                }
                else
                    results.AppendLine("No scores for the.. whole.. year....... hmmm?");

                string message = results.ToString();
                GameThread.Invoke(() => SendResult(message, prefix), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Year command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }

        [Command("Zone", "Shows the current KoTH ranking for a specific zone across all periods.")]
        [Permission(MyPromoteLevel.None)]
        public async void ShowZone(string zoneName)
        {
            if (!CheckCooldown()) return;
            if (string.IsNullOrWhiteSpace(zoneName))
            {
                Context.Respond("Usage: !KoTH Zone <zone name>");
                return;
            }

            try
            {
                var (ok, eventData) = await SenX_KOTH_PluginMain.LoadEventDataAsync();
                if (!ok)
                {
                    GameThread.Invoke(() => Context.Respond("Data is busy, please try again."), "KoTH");
                    return;
                }
                if (eventData == null) return;

                var results = new StringBuilder();
                results.AppendLine();
                string prefix = "Zone: " + zoneName;

                List<PointEarned> weekZoneEvents = eventData.WeekEvents.Where(e =>
                        string.Equals(e.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) && !e.LastWipe.HasValue).ToList();

                List<PointEarned> monthZoneEvents = eventData.MonthEvents.Where(e =>
                        string.Equals(e.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) && !e.LastWipe.HasValue).ToList();

                List<PointEarned> yearZoneEvents = eventData.YearEvents.Where(e =>
                        string.Equals(e.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) && !e.LastWipe.HasValue).ToList();

                AppendZoneBreakdown(results, weekZoneEvents, "Weekly Zone: " + zoneName);
                results.AppendLine();
                AppendZoneBreakdown(results, monthZoneEvents, "Monthly Zone: " + zoneName);
                results.AppendLine();
                AppendZoneBreakdown(results, yearZoneEvents, "Yearly Zone: " + zoneName);

                string message = results.ToString();
                GameThread.Invoke(() => SendResult(message, prefix), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Zone command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }

        [Command("About", "Shows plugin information and command help.")]
        [Permission(MyPromoteLevel.None)]
        public void About()
        {
            if (!CheckCooldown()) return;
            var results = new StringBuilder();
            results.AppendLine();
            string prefix = "KoTH Plugin v2.0.0.0";

            results.AppendLine("=== KoTH Plugin ===");
            results.AppendLine("By SentorX");
            results.AppendLine();
            results.AppendLine("Player Commands:");
            results.AppendLine("  !KoTH Week    - Show weekly leaderboard");
            results.AppendLine("  !KoTH Month   - Show monthly leaderboard");
            results.AppendLine("  !KoTH Year    - Show yearly leaderboard");
            results.AppendLine("  !KoTH Zone <name> - Show per-zone breakdowns");
            results.AppendLine("  !KoTH Bank    - Show faction bank balance");
            results.AppendLine("  !KoTH BuyTicket [count] - Buy raffle tickets");
            results.AppendLine();
            results.AppendLine("Admin Commands:");
            results.AppendLine("  !KoTH GivePoint <id_or_tag> <value> - Adjust bank points");
            results.AppendLine("  !KoTH ForceTest    - Test Discord webhook");

            SendResult(results.ToString(), prefix);
        }

        [Command("Bank", "Show faction bank balance and raffle tickets.")]
        [Permission(MyPromoteLevel.None)]
        public async void Bank()
        {
            if (!CheckCooldown()) return;
            if (Context.Player == null) return;

            var config = SenX_KOTH_PluginMain.Instance?.Config;
            int ticketCost = config?.TicketCost ?? 10;

            var faction = Sandbox.ModAPI.MyAPIGateway.Session.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null) { Context.Respond("You are not in a faction."); return; }
            if (!faction.IsFounder(Context.Player.IdentityId) && !faction.IsLeader(Context.Player.IdentityId))
            { Context.Respond("Only faction founders and leaders can view bank details."); return; }

            long factionId = faction.FactionId;
            string factionTag = faction.Tag;

            try
            {
                var (ok, balance, tickets) = await BankService.GetBalanceAsync(factionId);
                if (!ok)
                {
                    GameThread.Invoke(() => Context.Respond("Data is busy, please try again."), "KoTH");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("=== " + factionTag + " Bank ===");
                sb.AppendLine("Balance: " + balance + " pts");
                sb.AppendLine("Raffle Tickets Purchased: " + tickets);
                sb.AppendLine("Ticket Cost: " + ticketCost + " pts each");
                sb.AppendLine("Max Tickets You Can Buy: " + (ticketCost > 0 ? balance / ticketCost : 0));

                string message = sb.ToString();
                string prefix = factionTag + " Bank";
                GameThread.Invoke(() => SendResult(message, prefix), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Bank command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }

        [Command("BuyTicket", "Buy raffle tickets. Usage: !KoTH BuyTicket [count]. Founder/leader only.")]
        [Permission(MyPromoteLevel.None)]
        public async void BuyTicket(int count = 1)
        {
            if (!CheckCooldown()) return;
            if (Context.Player == null)
            {
                Context.Respond("Tickets can only be purchased in-game.");
                return;
            }
            if (count < 1) count = 1;

            var config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null) return;

            long playerIdentityId = Context.Player.IdentityId;
            var faction = Sandbox.ModAPI.MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerIdentityId);
            if (faction == null) { Context.Respond("You are not in a faction."); return; }
            if (!faction.IsFounder(playerIdentityId) && !faction.IsLeader(playerIdentityId))
            { Context.Respond("Only faction founders and leaders can buy tickets."); return; }

            int ticketCost = Math.Max(1, config.TicketCost);
            long factionId = faction.FactionId;
            string factionName = faction.Name;
            string factionTag = faction.Tag;

            try
            {
                var (_, msg) = await BankService.BuyTicketsAsync(factionId, factionName, factionTag, count, ticketCost);
                GameThread.Invoke(() => Context.Respond(msg), "KoTH");
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "BuyTicket command failed");
                GameThread.Invoke(() => Context.Respond("An error occurred, please try again."), "KoTH");
            }
        }

        private void SendResult(string message, string prefix)
        {
            if (Context.Player != null)
                ModCommunication.SendMessageTo(new DialogMessage("KoTH", prefix, message), Context.Player.SteamUserId);
            else
                Context.Respond(message, "KoTH");
        }

        private static void AppendZoneBreakdown(StringBuilder results, IEnumerable<PointEarned> events, string header)
        {
            if (!events.Any()) return;

            results.AppendLine();
            results.AppendLine("--- " + header + " ---");

            var zoneFactionPoints = new Dictionary<string, Dictionary<long, int>>();
            foreach (var p in events)
            {
                string zone = string.IsNullOrEmpty(p.ZoneName) ? "Unknown" : p.ZoneName;
                if (!zoneFactionPoints.ContainsKey(zone))
                    zoneFactionPoints[zone] = new Dictionary<long, int>();
                if (!zoneFactionPoints[zone].ContainsKey(p.FactionId))
                    zoneFactionPoints[zone][p.FactionId] = 0;
                zoneFactionPoints[zone][p.FactionId] += p.Points;
            }

            foreach (var zoneEntry in zoneFactionPoints)
            {
                results.AppendLine("  [" + zoneEntry.Key + "]");
                var sorted = zoneEntry.Value.OrderByDescending(x => x.Value).ToList();
                foreach (var factionEntry in sorted)
                    results.AppendLine("    " + FactionLookup.GetName(factionEntry.Key) + " => " + factionEntry.Value);
            }
        }
    }
}
