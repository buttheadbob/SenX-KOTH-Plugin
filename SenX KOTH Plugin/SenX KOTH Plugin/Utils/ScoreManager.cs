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
        public List<PlanetDescription> PlanetScores { get; set; } = new ();
    }

    [ProtoContract]
    public sealed class PlanetDescription
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public List<ScoreDescription> Scores { get; set; } = new ();
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
            Session? kothData = ScoresFromStorage();

            if (kothData == null)
                return;

            foreach (PlanetDescription? Planet in kothData.PlanetScores)
            {
                foreach (ScoreDescription? Points in Planet.Scores)
                {
                    if (Points == null)
                        continue;
                    
                    KeyValuePair<string, int> weekScore = MasterScore.WeekScores.Find(x => x.Key == Points.FactionName);
                    if (weekScore.Key != null)
                    {
                        MasterScore.WeekScores.Remove(weekScore);
                        weekScore = new KeyValuePair<string, int>(weekScore.Key, weekScore.Value + Points.Points);
                        MasterScore.WeekScores.Add(weekScore);
                    }
                    else
                    {
                        MasterScore.WeekScores.Add(new KeyValuePair<string, int>(Points.FactionName, Points.Points));
                    }
                    
                    KeyValuePair<string, int> monthScore = MasterScore.MonthScores.Find(x => x.Key == Points.FactionName);
                    if (monthScore.Key != null)
                    {
                        MasterScore.MonthScores.Remove(monthScore);
                        monthScore = new KeyValuePair<string, int>(monthScore.Key, monthScore.Value + Points.Points);
                        MasterScore.MonthScores.Add(monthScore);
                    }
                    else
                    {
                        MasterScore.MonthScores.Add(new KeyValuePair<string, int>(Points.FactionName, Points.Points));
                    }
                    
                    KeyValuePair<string, int> yearScore = MasterScore.YearScores.Find(x => x.Key == Points.FactionName);
                    if (yearScore.Key != null)
                    {
                        MasterScore.YearScores.Remove(yearScore);
                        yearScore = new KeyValuePair<string, int>(yearScore.Key, yearScore.Value + Points.Points);
                        MasterScore.YearScores.Add(yearScore);
                    }
                    else
                    {
                        MasterScore.YearScores.Add(new KeyValuePair<string, int>(Points.FactionName, Points.Points));
                    }
                }
            }

            Save_MasterData(MasterScore);
            // Tells the KOTH mod to clear the scores.  Plugin will provide commands for players to see current scores.
            NetworkService.SendPacket("clear");
        }
    }

    public class ScoreFile
    {
        public List<KeyValuePair<string, int>> WeekScores { get; set; } = new();
        public List<KeyValuePair<string, int>> MonthScores { get; set; } = new();
        public List<KeyValuePair<string, int>> YearScores { get; set; } = new();

    }
}
