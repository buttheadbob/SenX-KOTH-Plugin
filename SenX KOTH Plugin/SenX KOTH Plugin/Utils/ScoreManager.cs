using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SenX_KOTH_Plugin.Utils
{
    [Serializable()]
    [XmlRoot(ElementName = "Session")]
    public class session
    {
        [XmlElement("PlanetScores")] public List<PlanetScores> PlanetScores { get; set; }
    }

    [Serializable()]
    [XmlRoot(ElementName = "PlanetScores")]
    public class PlanetScores
    {
        [XmlElement] public List<PlanetDescription> PlanetDescription { get; set; }
    }

    [Serializable()]
    [XmlRoot(ElementName = "PlanetDescription")]
    public class PlanetDescription
    {
        [XmlElement] public string Name { get; set; }
        [XmlElement] public List<Scores> Scores { get; set; }
    }

    [Serializable()]
    [XmlRoot(ElementName = "Scores")]
    public class Scores
    {
        [XmlElement] public List<ScoreDescription> ScoreDescription { get; set; }
    }

    [Serializable()]
    [XmlRoot(ElementName = "ScoreDescription")]
    public class ScoreDescription
    {
        [XmlElement] public long FactionId { get; set; }
        [XmlElement] public string FactionName { get; set; }
        [XmlElement] public string FactionTag { get; set; }
        [XmlElement] public int Points { get; set; }
        [XmlElement] public string PlanetId { get; set; }
        [XmlElement] public string Gridname { get; set; }
    }

    public sealed class ResetScores
    {
        public static void ProcessScoresAndReset()
        {
            var kothdata = SenX_KOTH_PluginMain.ScoresFromStorage();

            foreach (var score in kothdata.PlanetScores)
            {
                foreach (var Location in score.PlanetDescription)
                {
                    foreach (var ListOfScores in Location.Scores)
                    {
                        foreach (var Score in ListOfScores.ScoreDescription)
                        {
                            ScoreData data = new ScoreData()
                            {
                                KothName = Score.PlanetId,
                                LogTime = DateTime.Now,
                                FactionId = Score.FactionId,
                                FactionName = Score.FactionName,
                                FactionTAG = Score.FactionTag,
                                GridName = Score.Gridname,
                                Points = Score.Points
                            };

                            SenX_KOTH_PluginMain.Instance.Config.WeekScoreData.Add(data);
                            SenX_KOTH_PluginMain.Instance.Config.MonthScoreRecord.Add(data);
                            SenX_KOTH_PluginMain.Instance.Config.YearlyScoreRecord.Add(data);
                        }
                    }
                }
            }

            // Tells the KOTH mod to clear the scores.  Plugin will provide commands for players.
            Network.NetworkService.SendPacket("clear");
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
}
