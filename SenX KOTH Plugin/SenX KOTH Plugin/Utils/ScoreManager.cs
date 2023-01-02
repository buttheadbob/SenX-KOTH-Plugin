using ProtoBuf;
using SenX_KOTH_Plugin;
using SenX_KOTH_Plugin.Network;
using SenX_KOTH_Plugin.Utils;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SenX_KOTH_Plugin.Utils
{
    [ProtoContract]
    public class Session
    {
        [ProtoMember(1)]
        public List<PlanetDescription> PlanetScores { get; set; } = new List<PlanetDescription>();
    }

    [ProtoContract]
    public class PlanetDescription
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public List<ScoreDescription> Scores { get; set; } = new List<ScoreDescription>();
    }
        
    [ProtoContract]
    public class ScoreDescription
    {
        [ProtoMember(1)]
        public long FactionId { get; set; }

        [ProtoMember(2)]
        public string FactionName { get; set; }

        [ProtoMember(3)]
        public string FactionTag { get; set; }

        [ProtoMember(4)]
        public int Points { get; set; }

        [ProtoMember(5)]
        public string PlanetId { get; set; }

        [ProtoMember(6)]
        public string GridName { get; set; }
    }
}

    public sealed class ResetScores
    {
        public static void ProcessScoresAndReset()
        {
            var kothdata = SenX_KOTH_PluginMain.ScoresFromStorage();

            foreach (PlanetDescription Planet in kothdata.PlanetScores)
            {
                foreach (ScoreDescription Points in Planet.Scores)
                {
                    ScoreData data = new ScoreData()
                    {
                        KothName = Points.PlanetId,
                        FactionId = Points.FactionId,
                        FactionName = Points.FactionName,
                        FactionTAG = Points.FactionTag,
                        GridName = Points.GridName,
                        Points = Points.Points
                    };

                    SenX_KOTH_PluginMain.Instance.Config.WeekScoreData.Add(data);
                    SenX_KOTH_PluginMain.Instance.Config.MonthScoreRecord.Add(data);
                    SenX_KOTH_PluginMain.Instance.Config.YearlyScoreRecord.Add(data);
                }
            }

            // Tells the KOTH mod to clear the scores.  Plugin will provide commands for players.
            NetworkService.SendPacket("clear");
        }
    }

    public struct ScoreData
    {
        public string KothName { get; set; }
        public long FactionId { get; set; }
        public string FactionTAG { get; set; }
        public string FactionName { get; set; }
        public int Points { get; set; }
        public string GridName { get; set; }
        public DateTime LogTime { get; set; }
    }

