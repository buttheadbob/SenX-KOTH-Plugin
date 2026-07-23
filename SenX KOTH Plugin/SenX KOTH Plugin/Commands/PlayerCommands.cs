using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using SenX_KOTH_Plugin.Nexus;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Commands
{
    [Category("KoTH")]
    public sealed class KothPlayerCommands : CommandModule
    {
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
        public void ShowWeek()
        {
            if (!CheckCooldown()) return;
            var weekList = NexusManager.AccumulatedScores.WeekScores
                .OrderByDescending(x => x.Value).ToList();

            var results = new StringBuilder();
            results.AppendLine();
            string prefix = "Standing Weekly Results";

            if (weekList.Count > 0)
            {
                results.AppendLine("=== Weekly Leaderboard ===");
                foreach (var result in weekList)
                    results.AppendLine(result.Key + " => " + result.Value);

                var scoreData = SenX_KOTH_PluginMain.EventPersist?.Data;
                if (scoreData != null)
                    AppendZoneBreakdown(results, scoreData.WeekEvents, "Weekly Zone Breakdown");
            }
            else
                results.AppendLine("No results for this week to show.");

            SendResult(results.ToString(), prefix);
        }

        [Command("Month", "Shows the current KoTH ranking and points for the month.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowMonth()
        {
            if (!CheckCooldown()) return;
            var monthList = NexusManager.AccumulatedScores.MonthScores
                .OrderByDescending(x => x.Value).ToList();

            var results = new StringBuilder();
            results.AppendLine();
            string prefix = "Standing Results for " + DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);

            if (monthList.Count > 0)
            {
                results.AppendLine("=== Monthly Leaderboard ===");
                foreach (var result in monthList)
                    results.AppendLine(result.Key + " => " + result.Value);

                var scoreData = SenX_KOTH_PluginMain.EventPersist?.Data;
                if (scoreData != null)
                    AppendZoneBreakdown(results, scoreData.MonthEvents, "Monthly Zone Breakdown");
            }
            else
                results.AppendLine("No results for the month.. now's your chance!");

            SendResult(results.ToString(), prefix);
        }

        [Command("Year", "Shows the current KoTH ranking and points for the year.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowYear()
        {
            if (!CheckCooldown()) return;
            var yearList = NexusManager.AccumulatedScores.YearScores
                .OrderByDescending(x => x.Value).ToList();

            var results = new StringBuilder();
            results.AppendLine();
            string prefix = "Standing Results for " + DateTime.Now.Year;

            if (yearList.Count > 0)
            {
                results.AppendLine("=== Yearly Leaderboard ===");
                foreach (var result in yearList)
                    results.AppendLine(result.Key + " => " + result.Value);

                var scoreData = SenX_KOTH_PluginMain.EventPersist?.Data;
                if (scoreData != null)
                    AppendZoneBreakdown(results, scoreData.YearEvents, "Yearly Zone Breakdown");
            }
            else
                results.AppendLine("No scores for the.. whole.. year....... hmmm?");

            SendResult(results.ToString(), prefix);
        }

        [Command("Zone", "Shows the current KoTH ranking for a specific zone across all periods.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowZone(string zoneName)
        {
            if (!CheckCooldown()) return;
            if (string.IsNullOrWhiteSpace(zoneName))
            {
                Context.Respond("Usage: !KoTH Zone <zone name>");
                return;
            }

            var results = new StringBuilder();
            results.AppendLine();
            string prefix = "Zone: " + zoneName;

            var eventData = SenX_KOTH_PluginMain.EventPersist?.Data;
            if (eventData == null) return;

            List<PointEarned> weekZoneEvents, monthZoneEvents, yearZoneEvents;

            weekZoneEvents = eventData.WeekEvents.ToList().Where(e =>
                    string.Equals(e.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) && !e.LastWipe.HasValue).ToList();

            monthZoneEvents = eventData.MonthEvents.ToList().Where(e =>
                    string.Equals(e.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) && !e.LastWipe.HasValue).ToList();

            yearZoneEvents = eventData.YearEvents.ToList().Where(e =>
                    string.Equals(e.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) && !e.LastWipe.HasValue).ToList();

            AppendZoneBreakdown(results, weekZoneEvents, "Weekly Zone: " + zoneName);
            results.AppendLine();
            AppendZoneBreakdown(results, monthZoneEvents, "Monthly Zone: " + zoneName);
            results.AppendLine();
            AppendZoneBreakdown(results, yearZoneEvents, "Yearly Zone: " + zoneName);

            SendResult(results.ToString(), prefix);
        }

        [Command("About", "Shows the current KoTH ranking and points for the week/month/year with a few added options.")]
        [Permission(MyPromoteLevel.None)]
        public void About()
        {
            if (!CheckCooldown()) return;
            var results = new StringBuilder();
            results.AppendLine();
            string prefix = "KoTH Plugin v2.0.0.0";

            results.AppendLine("=== KoTH Plugin ===");
            results.AppendLine("Player Commands:");
            results.AppendLine("  !KoTH Week    - Show weekly leaderboard");
            results.AppendLine("  !KoTH Month   - Show monthly leaderboard");
            results.AppendLine("  !KoTH Year    - Show yearly leaderboard");
            results.AppendLine("  !KoTH Zone <name> - Show per-zone breakdowns");
            results.AppendLine("  !KoTH Bank    - Show faction bank balance");
            results.AppendLine("  !KoTH BuyTicket [count] - Buy raffle tickets");
            results.AppendLine();
            results.AppendLine("Admin Commands:");
            results.AppendLine("  !KoTH CreateZone <name> [radius] - Create a zone");
            results.AppendLine("  !KoTH DeleteZone <name> - Delete a zone");
            results.AppendLine("  !KoTH GivePoint <id_or_tag> <value> - Adjust bank points");
            results.AppendLine("  !KoTH ForceUpdate  - Force score update");
            results.AppendLine("  !KoTH ForceTest    - Test Discord webhook");
            results.AppendLine("  !KoTH ForceNexusSync - Force Nexus verification");
            results.AppendLine("  !KoTH ForceRewardSync - Force reward config sync");

            SendResult(results.ToString(), prefix);
        }

        [Command("Bank", "Shows your faction's bank balance. Founder/leader only.")]
        [Permission(MyPromoteLevel.None)]
        public void ShowBank()
        {
            if (!CheckCooldown()) return;
            if (Context.Player == null)
            {
                Context.Respond("Bank info is only available in-game.");
                return;
            }

            var faction = Sandbox.ModAPI.MyAPIGateway.Session.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (faction == null)
            {
                Context.Respond("You are not in a faction.");
                return;
            }

            if (!faction.IsFounder(Context.Player.IdentityId) && !faction.IsLeader(Context.Player.IdentityId))
            {
                Context.Respond("Only faction founders and leaders can view bank details.");
                return;
            }

            var bankPersist = SenX_KOTH_PluginMain.BankPersist;
            if (bankPersist == null) return;

            var config = SenX_KOTH_PluginMain.Instance?.Config;
            int ticketCost = config?.TicketCost ?? 10;

            var bank = BankService.GetBank(bankPersist.Data, faction.FactionId);
            int balance = bank?.Points ?? 0;
            int tickets = SenX_KOTH_PluginMain.RafflePersist?.Data.Tickets
                .FirstOrDefault(t => t.FactionId == faction.FactionId)?.Count ?? 0;

            var sb = new StringBuilder();
            sb.AppendLine("=== " + faction.Tag + " Bank ===");
            sb.AppendLine("Balance: " + balance + " pts");
            sb.AppendLine("Raffle Tickets Purchased: " + tickets);
            sb.AppendLine("Ticket Cost: " + ticketCost + " pts each");
            sb.AppendLine("Max Tickets You Can Buy: " + (ticketCost > 0 ? balance / ticketCost : 0));

            SendResult(sb.ToString(), faction.Tag + " Bank");
        }

        [Command("BuyTicket", "Buy raffle tickets. Usage: !KoTH BuyTicket [count]. Founder/leader only.")]
        [Permission(MyPromoteLevel.None)]
        public void BuyTicket(int count = 1)
        {
            if (!CheckCooldown()) return;
            if (Context.Player == null)
            {
                Context.Respond("Tickets can only be purchased in-game.");
                return;
            }

            if (count < 1) count = 1;

            var bankPersist = SenX_KOTH_PluginMain.BankPersist;
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            var rafflePersist = SenX_KOTH_PluginMain.RafflePersist;
            if (bankPersist == null || rafflePersist == null || config == null) return;

            if (BankService.BuyTickets(bankPersist.Data, rafflePersist.Data, config, Context.Player.IdentityId, count, out string result))
            {
                bankPersist.Save();
                rafflePersist.Save();
                Context.Respond(result);
            }
            else
                Context.Respond(result);
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

            var zoneFactionPoints = new Dictionary<string, Dictionary<string, int>>();
            foreach (var p in events)
            {
                string zone = string.IsNullOrEmpty(p.ZoneName) ? "Unknown" : p.ZoneName;
                if (!zoneFactionPoints.ContainsKey(zone))
                    zoneFactionPoints[zone] = new Dictionary<string, int>();
                if (!zoneFactionPoints[zone].ContainsKey(p.FactionName))
                    zoneFactionPoints[zone][p.FactionName] = 0;
                zoneFactionPoints[zone][p.FactionName] += p.Points;
            }

            foreach (var zoneEntry in zoneFactionPoints)
            {
                results.AppendLine("  [" + zoneEntry.Key + "]");
                var sorted = zoneEntry.Value.OrderByDescending(x => x.Value).ToList();
                foreach (var factionEntry in sorted)
                    results.AppendLine("    " + factionEntry.Key + " => " + factionEntry.Value);
            }
        }
    }
}
