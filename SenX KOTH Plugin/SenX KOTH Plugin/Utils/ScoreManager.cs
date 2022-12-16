using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public class ResetScores
    {
        public void ProcessScores()
        {
            var kothdata = SenX_KOTH_PluginMain.ScoresFromStorage();

            foreach (var score in kothdata.PlanetScores)
            {
                foreach (var Location in score.PlanetDescription)
                {
                    // The original Author is an idiot, he has a list inside of a list for no reason...
                    foreach (var ListOfScores in Location.Scores)
                    {
                        foreach (var Score in ListOfScores.ScoreDescription)
                        {
                            ScoreData data = new ScoreData()
                            {
                                KothName = Score.PlanetId,
                                Creation = DateTime.Now,
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

            Network.NetworkService.SendPacket("clear"); // Tells the KOTH mod to clear the scores.
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
        public DateTime Creation { get; set; }
    }

    public static class GenerateQuickReport
    {
        public static Dictionary<string,int> WeekToDate()
        {
            Dictionary<string, int> Report = new Dictionary<string, int>();
            
            foreach(ScoreData Score in SenX_KOTH_PluginMain.Instance.Config.WeekScoreData)
            {
                if (Report.ContainsKey(Score.FactionName))
                    Report[Score.FactionName] += Score.Points;
                else
                    Report[Score.FactionName] = Score.Points;
            }

            return Report;
        }

        public static Dictionary<string, int> MonthToDate()
        {
            Dictionary<string, int> Report = new Dictionary<string, int>();
            foreach (ScoreData Score in SenX_KOTH_PluginMain.Instance.Config.WeekScoreData)
            {
                if (Report.ContainsKey(Score.FactionName))
                    Report[Score.FactionName] += Score.Points;
                else
                    Report[Score.FactionName] = Score.Points;
            }

            foreach (ScoreData Score in SenX_KOTH_PluginMain.Instance.Config.MonthScoreRecord)
            {
                if (Report.ContainsKey(Score.FactionName))
                    Report[Score.FactionName] += Score.Points;
                else
                    Report[Score.FactionName] = Score.Points;
            }

            return Report;
        }

        public static Dictionary<string, int> YearToDate()
        {
            Dictionary<string, int> Report = new Dictionary<string, int>();
            foreach (ScoreData Score in SenX_KOTH_PluginMain.Instance.Config.WeekScoreData)
            {
                if (Report.ContainsKey(Score.FactionName))
                    Report[Score.FactionName] += Score.Points;
                else
                    Report[Score.FactionName] = Score.Points;
            }

            foreach (ScoreData Score in SenX_KOTH_PluginMain.Instance.Config.MonthScoreRecord)
            {
                if (Report.ContainsKey(Score.FactionName))
                    Report[Score.FactionName] += Score.Points;
                else
                    Report[Score.FactionName] = Score.Points;
            }

            foreach (ScoreData Score in SenX_KOTH_PluginMain.Instance.Config.YearlyScoreRecord)
            {
                if (Report.ContainsKey(Score.FactionName))
                    Report[Score.FactionName] += Score.Points;
                else
                    Report[Score.FactionName] = Score.Points;
            }

            return Report;
        }
    }
}
