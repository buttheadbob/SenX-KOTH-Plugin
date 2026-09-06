using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Torch.API.Managers;
using Torch.Commands;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class RewardService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => RewardService");

        public static void CheckLiveRewards(string zoneName, long factionId, IReadOnlyCollection<long> zoneIdentityIds)
        {
            SenX_KOTH_PluginConfig? config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null)
                return;

            ZoneRewardConfig? zoneConfig = config.ZoneRewards.FirstOrDefault(z =>
                string.Equals(z.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase));

            if (zoneConfig == null || zoneConfig.CommandRewards.Count == 0)
                return;

            try
            {
                IMyFaction? faction = null;
                MyAPIGateway.Session.Factions.Factions.TryGetValue(factionId, out faction);
                if (faction == null)
                    return;

                foreach (LiveCommandReward reward in zoneConfig.CommandRewards)
                {
                    if (!reward.Enabled || string.IsNullOrEmpty(reward.CommandText))
                        continue;

                    ExecuteRewardCommand(reward, faction, zoneIdentityIds);
                }
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log, ex, "Error executing live rewards for zone: " + zoneName);
            }
        }

        /// <summary>
        /// Deposits the configured cargo reward into the first cargo container that
        /// matches the reward's grid name and container name. Whole units only — items
        /// that do not fit are left undeposited and logged.
        /// </summary>
        public static void DeliverCargoReward(ZoneRewardConfig zoneReward)
        {
            if (zoneReward == null || zoneReward.CargoItems.Count == 0)
                return;

            GameThread.Invoke(() =>
            {
                try
                {
                    MyCubeGrid? grid = null;
                    foreach (var entity in MyEntities.GetEntities())
                    {
                        if (entity is MyCubeGrid candidate && !candidate.Closed && !candidate.MarkedForClose &&
                            string.Equals(candidate.DisplayName, zoneReward.GridName, StringComparison.OrdinalIgnoreCase))
                        {
                            grid = candidate;
                            break;
                        }
                    }

                    if (grid == null)
                    {
                        KoTHLog.Warn(Log, "Cargo reward: grid '" + zoneReward.GridName + "' not found for zone '" + zoneReward.ZoneName + "'.");
                        return;
                    }

                    MyCargoContainer? container = null;
                    foreach (var block in grid.GetFatBlocks<MyCargoContainer>())
                    {
                        if (!block.Closed && !block.MarkedForClose &&
                            string.Equals(block.DisplayNameText, zoneReward.ContainerName, StringComparison.OrdinalIgnoreCase))
                        {
                            container = block;
                            break;
                        }
                    }

                    if (container == null)
                    {
                        KoTHLog.Warn(Log, "Cargo reward: cargo container '" + zoneReward.ContainerName + "' not found on grid '" + grid.DisplayName + "'.");
                        return;
                    }

                    foreach (var item in zoneReward.CargoItems)
                    {
                        if (item == null || item.Quantity <= 0) continue;

                        MyObjectBuilder_PhysicalObject? builder = ItemBuilder.Create(item.TypeId, item.SubtypeId);
                        if (builder == null)
                        {
                            KoTHLog.Warn(Log, "Cargo reward: unsupported item type '" + item.TypeId + "/" + item.SubtypeId + "'.");
                            continue;
                        }

                        long remaining = item.Quantity;
                        long deposited = 0;

                        for (int i = 0; i < container.InventoryCount && remaining > 0; i++)
                        {
                            if (container.GetInventoryBase(i) is not MyInventory inv) continue;

                            MyFixedPoint fits = MyFixedPoint.Floor(inv.ComputeAmountThatFits(builder.GetObjectId()));
                            long fitsWhole = (long)(double)fits;
                            if (fitsWhole <= 0) continue;

                            long toAdd = Math.Min(remaining, fitsWhole);
                            inv.AddItems(Whole(toAdd), builder);
                            inv.Refresh();

                            deposited += toAdd;
                            remaining -= toAdd;
                        }

                        if (deposited > 0)
                            KoTHLog.Info(Log, "Cargo reward: deposited " + deposited + "x " + item.TypeId + "/" + item.SubtypeId + " into '" + container.DisplayNameText + "' on '" + grid.DisplayName + "'.");

                        if (remaining > 0)
                            KoTHLog.Warn(Log, "Cargo reward: only " + deposited + "/" + item.Quantity + "x " + item.TypeId + "/" + item.SubtypeId + " fit in container '" + container.DisplayNameText + "' — " + remaining + " not deposited.");
                    }
                }
                catch (Exception ex)
                {
                    KoTHLog.Error(Log, ex, "Error delivering cargo reward for zone: " + zoneReward.ZoneName);
                }
            });
        }

        private static MyFixedPoint Whole(long value)
        {
            if (value <= 0) return MyFixedPoint.Zero;
            return new MyFixedPoint { RawValue = value * 1_000_000L };
        }

        public static void ExecutePeriodRewards(
            List<RankRewardEntry> rankRewards,
            List<ThresholdRewardEntry> thresholdRewards,
            List<KeyValuePair<long, int>> sortedScores)
        {
            int currentRank = 1;
            for (int i = 0; i < sortedScores.Count; i++)
            {
                if (i > 0 && sortedScores[i].Value < sortedScores[i - 1].Value)
                    currentRank = i + 1;

                KeyValuePair<long, int> scoreEntry = sortedScores[i];
                IMyFaction? faction = null;
                MyAPIGateway.Session.Factions.Factions.TryGetValue(scoreEntry.Key, out faction);

                foreach (RankRewardEntry rankReward in rankRewards)
                {
                    if (rankReward.Rank == currentRank)
                        ExecuteCommandRewards(rankReward.Commands, faction);
                }

                foreach (ThresholdRewardEntry threshold in thresholdRewards)
                {
                    if (scoreEntry.Value >= threshold.MinPoints)
                        ExecuteCommandRewards(threshold.Commands, faction);
                }
            }
        }

        private static void ExecuteRewardCommand(LiveCommandReward reward, IMyFaction faction, IReadOnlyCollection<long> zoneIdentityIds)
        {
            IEnumerable<long> targetIds = reward.PerFactionMember
                ? faction.Members.Keys
                : zoneIdentityIds;

            ExecutePlayerCommands(reward.CommandText, faction, targetIds, reward.OnlyOnlineMembers);
        }

        private static void ExecuteCommandRewards(List<CommandRewardEntry> commands, IMyFaction? faction)
        {
            if (faction == null)
                return;

            foreach (CommandRewardEntry cmd in commands)
            {
                if (string.IsNullOrEmpty(cmd.CommandText))
                    continue;

                ExecutePlayerCommands(cmd.CommandText, faction, faction.Members.Keys, cmd.OnlyOnlineMembers);
            }
        }

        internal static void ExecutePlayerCommands(string commandText, IMyFaction faction, IEnumerable<long> identityIds, bool onlyOnline)
        {
            foreach (long identityId in identityIds)
            {
                ulong steamId = MyAPIGateway.Players.TryGetSteamId(identityId);
                if (steamId == 0)
                    continue;

                if (onlyOnline)
                {
                    IMyPlayer? player = MyAPIGateway.Players.TryGetIdentityId(identityId);
                    if (player == null || player.Character == null)
                        continue;
                }

                string command = commandText
                    .Replace("{playerid}", steamId.ToString())
                    .Replace("{factionid}", faction.FactionId.ToString())
                    .Replace("{factionname}", faction.Name)
                    .Replace("{factiontag}", faction.Tag);

                RunTorchCommand(command);
            }
        }

        private static void RunTorchCommand(string commandText)
        {
            try
            {
                var commandManager = SenX_KOTH_PluginMain.Instance?.Torch.CurrentSession?.Managers
                    .GetManager<CommandManager>();
                if (commandManager != null)
                {
                    commandManager.HandleCommandFromServer(commandText);
                    Log.Info("KoTH Reward Command Executed: " + commandText);
                }
                else
                {
                    Log.Error("Cannot execute reward command — CommandManager not available: " + commandText);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running command: " + commandText);
            }
        }
    }
}
