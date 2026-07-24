using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using VRage.Game.ModAPI;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class RewardService
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => RewardService");

        public static void CheckLiveRewards(PointEarned point)
        {
            SenX_KOTH_PluginConfig? config = SenX_KOTH_PluginMain.Instance?.Config;
            if (config == null)
                return;

            if (string.IsNullOrEmpty(point.ZoneName))
                return;

            ZoneRewardConfig? zoneConfig = config.ZoneRewards.FirstOrDefault(z =>
                string.Equals(z.ZoneName, point.ZoneName, StringComparison.OrdinalIgnoreCase));

            if (zoneConfig == null || zoneConfig.CommandRewards.Count == 0)
                return;

            try
            {
                IMyFaction? faction = null;
                MyAPIGateway.Session.Factions.Factions.TryGetValue(point.FactionId, out faction);
                if (faction == null)
                    return;

                foreach (LiveCommandReward reward in zoneConfig.CommandRewards)
                {
                    if (!reward.Enabled || string.IsNullOrEmpty(reward.CommandText))
                        continue;

                    ExecuteRewardCommand(reward, faction);
                }
            }
            catch (Exception ex)
            {
                KoTHLog.Error(Log,ex, "Error executing live rewards for zone: " + point.ZoneName);
            }
        }

        public static void ExecutePeriodRewards(
            List<RankRewardEntry> rankRewards,
            List<ThresholdRewardEntry> thresholdRewards,
            List<KeyValuePair<string, int>> sortedScores)
        {
            int currentRank = 1;
            for (int i = 0; i < sortedScores.Count; i++)
            {
                if (i > 0 && sortedScores[i].Value < sortedScores[i - 1].Value)
                    currentRank = i + 1;

                KeyValuePair<string, int> scoreEntry = sortedScores[i];
                IMyFaction? faction = null;
                foreach (IMyFaction f in MyAPIGateway.Session.Factions.Factions.Values)
                {
                    if (f.Tag == scoreEntry.Key || f.Name == scoreEntry.Key)
                    {
                        faction = f;
                        break;
                    }
                }

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

        private static void ExecuteRewardCommand(LiveCommandReward reward, IMyFaction faction)
        {
            if (reward.PerFactionMember)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);

                foreach (IMyPlayer player in players)
                {
                    if (player.IdentityId == 0)
                        continue;

                    IMyFaction? playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
                    if (playerFaction == null || playerFaction.FactionId != faction.FactionId)
                        continue;

                    if (reward.OnlyOnlineMembers && player.Character == null)
                        continue;

                    string command = reward.CommandText.Replace("{playerid}", player.SteamUserId.ToString());
                    command = command.Replace("{factionid}", faction.FactionId.ToString());
                    command = command.Replace("{factionname}", faction.Name);
                    command = command.Replace("{factiontag}", faction.Tag);

                    RunTorchCommand(command);
                }
            }
            else
            {
                string command = reward.CommandText;
                command = command.Replace("{factionid}", faction.FactionId.ToString());
                command = command.Replace("{factionname}", faction.Name);
                command = command.Replace("{factiontag}", faction.Tag);

                RunTorchCommand(command);
            }
        }

        private static void ExecuteCommandRewards(List<CommandRewardEntry> commands, IMyFaction? faction)
        {
            if (faction == null)
                return;

            foreach (CommandRewardEntry cmd in commands)
            {
                if (string.IsNullOrEmpty(cmd.CommandText))
                    continue;

                if (cmd.PerFactionMember)
                {
                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);

                    foreach (IMyPlayer player in players)
                    {
                        if (player.IdentityId == 0)
                            continue;

                        IMyFaction? playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
                        if (playerFaction == null || playerFaction.FactionId != faction.FactionId)
                            continue;

                        if (cmd.OnlyOnlineMembers && player.Character == null)
                            continue;

                        string command = cmd.CommandText.Replace("{playerid}", player.SteamUserId.ToString());
                        command = command.Replace("{factionid}", faction.FactionId.ToString());
                        command = command.Replace("{factionname}", faction.Name);
                        command = command.Replace("{factiontag}", faction.Tag);

                        RunTorchCommand(command);
                    }
                }
                else
                {
                    string command = cmd.CommandText;
                    command = command.Replace("{factionid}", faction.FactionId.ToString());
                    command = command.Replace("{factionname}", faction.Name);
                    command = command.Replace("{factiontag}", faction.Tag);

                    RunTorchCommand(command);
                }
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
