using ProtoBuf;
using SenX_KOTH_Plugin.Network;
using System.Collections.Generic;
using static SenX_KOTH_Plugin.SenX_KOTH_PluginMain;

namespace SenX_KOTH_Plugin.Utils
{
    [ProtoContract]
    public sealed class Session
    {
        [ProtoMember(1)]
        public List<PlanetDescription> PlanetScores { get; set; } = new List<PlanetDescription>();
    }

    [ProtoContract]
    public sealed class PlanetDescription
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public List<ScoreDescription> Scores { get; set; } = new List<ScoreDescription>();
    }

    [ProtoContract]
    public sealed class ScoreDescription
    {
        [ProtoMember(1)]
        public long FactionId { get; set; }

        [ProtoMember(2)]
        public string FactionName { get; set;}

        [ProtoMember(3)]
        public string FactionTag { get; set; }

        [ProtoMember(4)]
        public int Points { get; set; }

        [ProtoMember(5)]
        public string PlanetId { get; set; }

        [ProtoMember(6)]
        public string GridName { get; set; }
    }


    public sealed class ResetScores
    {
        public static void ProcessScoresAndReset()
        {
            var kothData = ScoresFromStorage();

            if (kothData == null)
                return;

            foreach (var Planet in kothData.PlanetScores)
            {
                foreach (var Points in Planet.Scores)
                {
                    if (Points == null)
                        continue;

                    if (MasterScore.WeekScores == null)
                    {
                        MasterScore.WeekScores = new List<KeyValuePair<string, int>> {new KeyValuePair<string, int>(Points.FactionName, Points.Points)};
                    }
                    else
                    {
                        for (int i = 0; i < MasterScore.WeekScores.Count; i++)
                        {
                            if (MasterScore.WeekScores[i].Key == Points.FactionName)
                            {
                                MasterScore.WeekScores[i] = new KeyValuePair<string, int>(MasterScore.WeekScores[i].Key,
                                    MasterScore.WeekScores[i].Value + Points.Points);
                                break;
                            }

                            MasterScore.WeekScores.Add(new KeyValuePair<string, int>(Points.FactionName,
                                Points.Points));
                        }
                    }

                    if (MasterScore.MonthScores == null)
                    {
                        MasterScore.MonthScores = new List<KeyValuePair<string, int>> {new KeyValuePair<string, int>(Points.FactionName, Points.Points)};
                    }
                    else
                    {
                        for (int i = 0; i < MasterScore.MonthScores.Count; i++)
                        {
                            if (MasterScore.MonthScores[i].Key == Points.FactionName)
                            {
                                MasterScore.MonthScores[i] = new KeyValuePair<string, int>(
                                    MasterScore.MonthScores[i].Key, MasterScore.MonthScores[i].Value + Points.Points);
                                break;
                            }

                            MasterScore.MonthScores.Add(
                                new KeyValuePair<string, int>(Points.FactionName, Points.Points));
                        }
                    }

                    if (MasterScore.YearScores == null)
                    {
                        MasterScore.YearScores = new List<KeyValuePair<string, int>> {new KeyValuePair<string, int>(Points.FactionName, Points.Points)};
                    }
                    else
                    {
                        for (int i = 0; i < MasterScore.YearScores.Count; i++)
                        {
                            if (MasterScore.YearScores[i].Key == Points.FactionName)
                            {
                                MasterScore.YearScores[i] = new KeyValuePair<string, int>(MasterScore.YearScores[i].Key,
                                    MasterScore.YearScores[i].Value + Points.Points);
                                break;
                            }

                            MasterScore.YearScores.Add(new KeyValuePair<string, int>(Points.FactionName,
                                Points.Points));
                        }
                    }
                }
            }

            Save_MasterData(MasterScore);
            // Tells the KOTH mod to clear the scores.  Plugin will provide commands for players to see current scores.
            NetworkService.SendPacket("clear");
        }
    }

    public struct ScoreFile
    {
        public List<KeyValuePair<string,int>> WeekScores { get; set; }
        public List<KeyValuePair<string, int>> MonthScores { get; set; }
        public List<KeyValuePair<string, int>> YearScores { get; set; }

    }
}
