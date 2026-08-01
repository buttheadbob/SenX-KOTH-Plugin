using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Services;
using SenX_KOTH_Plugin.Utils;
using SenX_KOTH_Plugin.Nexus;
using SenX_KOTH_Plugin.Models;
using System.Globalization;
using System.Threading;
using DrawingColor = System.Drawing.Color;
// ReSharper disable InconsistentNaming

namespace SenX_KOTH_Plugin
{
    public partial class SenX_KOTH_PluginControl : UserControl
    {
        private KothZone? _editingZone;
        private bool _isNewZone;
        private ZoneRewardConfig? _editingZoneReward;
        private LiveCommandReward? _editingZoneCmd;
        private RankRewardEntry? _editingRank;
        private int _editingRankPeriod;
        private CommandRewardEntry? _editingRankCmd;
        private ThresholdRewardEntry? _editingThreshold;
        private int _editingThresholdPeriod;

        public SenX_KOTH_PluginControl()
        {
            var config = SenX_KOTH_PluginMain.Instance?.Config;
            DataContext = config;
            InitializeComponent();
            RefreshZoneNames();
            ZonesList.ItemsSource = SenX_KOTH_PluginMain.ZonePersist?.Data.Zones;

            if (config != null && string.IsNullOrEmpty(config.SharedDataPath))
                config.SharedDataPath = SenX_KOTH_PluginMain.LocalDataPath;

            var pendingData = SenX_KOTH_PluginMain.PendingCreditsPersist?.Data;
            if (pendingData != null)
            {
                PendingGrid.ItemsSource = pendingData.Credits;
                pendingData.Credits.CollectionChanged += (_, _) =>
                    PendingCount.Text = pendingData.Credits.Count + " pending";
                PendingCount.Text = pendingData.Credits.Count + " pending";
            }
        }

        public ObservableConcurrentUiSafeCollection<string> ZoneNames { get; } = new();

        private void RefreshZoneNames()
        {
            ZoneNames.Clear();
            var persist = SenX_KOTH_PluginMain.ZonePersist;
            if (persist == null) return;
            foreach (var z in persist.Data.Zones)
                ZoneNames.Add(z.Name);
        }

        private SenX_KOTH_PluginConfig? GetConfig() => SenX_KOTH_PluginMain.Instance?.Config;

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SenX_KOTH_PluginMain.ConfigPersist?.Save();
        }

        private void CreateZoneButton_Click(object sender, RoutedEventArgs e)
        {
            _isNewZone = true;
            _editingZone = new KothZone();
            ZoneEditor_Name.Text = "";
            ZoneEditor_PosX.Text = "";
            ZoneEditor_PosY.Text = "";
            ZoneEditor_PosZ.Text = "";
            ZoneEditor_Radius.Text = "50";
            ZoneEditor_ColorR.Text = "0.53";
            ZoneEditor_ColorG.Text = "0.81";
            ZoneEditor_ColorB.Text = "0.92";
            ZoneEditor_Texture.Text = "SafeZone_Texture_Default";
            ZoneEditor_Enabled.IsChecked = true;
            ZoneEditor_PersistVisual.IsChecked = true;
            ZoneEditor_Dynamic.IsChecked = false;
            ZoneEditor_CapturePtsNeeded.Text = "100";
            ZoneEditor_CapInterval.Text = "5";
            ZoneEditor_AwardInterval.Text = "5";
            ZoneEditor_PtsSuit.Text = "1";
            ZoneEditor_PtsGrid.Text = "0";
            ZoneEditor_MinDist.Text = "100";
            ZoneEditor_MaxDist.Text = "500";
            ZoneEditor_MoveInterval.Text = "60";
            ZoneEditor_AnnounceGps.IsChecked = true;
            ZoneEditor_EvictionEnabled.IsChecked = false;
            ZoneEditor_EvictionFreq.Text = "60";
            ZoneEditor_EvictionDuration.Text = "30";
            ZoneEditor_EvictionResetCapture.IsChecked = false;
            ZoneEditor_EvictionDowntime.IsChecked = false;
            ZoneEditor_EvictionColorR.Text = "1";
            ZoneEditor_EvictionColorG.Text = "0";
            ZoneEditor_EvictionColorB.Text = "0";
            ZoneEditor_EvictionTexture.Text = "";
            ZoneEditor_QuestDist.Text = "25000";
            ZoneEditor_QuestMode.SelectedIndex = 1;
            ZoneEditor_ShowEnemiesOutside.IsChecked = false;
            ZoneEditor_EnableWinFireworks.IsChecked = true;
            ZoneEditor_EnableLoseFireworks.IsChecked = true;
            ZoneEditor_DiscordCapture.IsChecked = true;
            ZoneEditor_DiscordDecay.IsChecked = true;
            ZoneEditor_DiscordPoints.IsChecked = true;
            ZoneEditor_DiscordEnter.IsChecked = true;
            ZoneEditor_SchedMon.IsChecked = true;
            ZoneEditor_SchedTue.IsChecked = true;
            ZoneEditor_SchedWed.IsChecked = true;
            ZoneEditor_SchedThu.IsChecked = true;
            ZoneEditor_SchedFri.IsChecked = true;
            ZoneEditor_SchedSat.IsChecked = true;
            ZoneEditor_SchedSun.IsChecked = true;
            ZoneEditor_SchedStartH.Text = "0";
            ZoneEditor_SchedStartM.Text = "0";
            ZoneEditor_SchedEndH.Text = "23";
            ZoneEditor_SchedEndM.Text = "59";
            ZoneEditorPanel.Visibility = Visibility.Visible;
        }

        private void EditZone_Click(object sender, RoutedEventArgs e)
        {
            _editingZone = (sender as Button)?.Tag as KothZone;
            if (_editingZone == null) return;
            _isNewZone = false;
            ZoneEditor_Name.Text = _editingZone.Name;
            ZoneEditor_PosX.Text = _editingZone.X.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_PosY.Text = _editingZone.Y.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_PosZ.Text = _editingZone.Z.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_PosX.IsEnabled = false;
            ZoneEditor_PosY.IsEnabled = false;
            ZoneEditor_PosZ.IsEnabled = false;
            ZoneEditor_Radius.Text = _editingZone.Radius.ToString("F0", CultureInfo.InvariantCulture);
            ZoneEditor_ColorR.Text = _editingZone.ColorR.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_ColorG.Text = _editingZone.ColorG.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_ColorB.Text = _editingZone.ColorB.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_Texture.Text = _editingZone.Texture;
            ZoneEditor_Enabled.IsChecked = _editingZone.Enabled;
            ZoneEditor_PersistVisual.IsChecked = _editingZone.PersistVisual;
            ZoneEditor_Dynamic.IsChecked = _editingZone.DynamicZone;
            ZoneEditor_CapturePtsNeeded.Text = _editingZone.CapturePointsNeeded.ToString();
            ZoneEditor_CapInterval.Text = _editingZone.CapturePointIntervalSeconds.ToString();
            ZoneEditor_AwardInterval.Text = _editingZone.PointAwardIntervalSeconds.ToString();
            ZoneEditor_PtsSuit.Text = _editingZone.PointsPerSuit.ToString();
            ZoneEditor_PtsGrid.Text = _editingZone.PointsPerGrid.ToString();
            ZoneEditor_MinDist.Text = _editingZone.MinDistance.ToString("F0", CultureInfo.InvariantCulture);
            ZoneEditor_MaxDist.Text = _editingZone.MaxDistance.ToString("F0", CultureInfo.InvariantCulture);
            ZoneEditor_MoveInterval.Text = _editingZone.DynamicMoveIntervalSeconds.ToString();
            ZoneEditor_AnnounceGps.IsChecked = _editingZone.AnnounceGps;
            ZoneEditor_EvictionEnabled.IsChecked = _editingZone.EvictionEnabled;
            ZoneEditor_EvictionFreq.Text = _editingZone.EvictionFrequencyMinutes.ToString();
            ZoneEditor_EvictionDuration.Text = _editingZone.EvictionDurationSeconds.ToString();
            ZoneEditor_EvictionResetCapture.IsChecked = _editingZone.EvictionResetCapture;
            ZoneEditor_EvictionDowntime.IsChecked = _editingZone.EvictionDuringDowntime;
            ZoneEditor_EvictionColorR.Text = _editingZone.EvictionColorR.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_EvictionColorG.Text = _editingZone.EvictionColorG.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_EvictionColorB.Text = _editingZone.EvictionColorB.ToString("F2", CultureInfo.InvariantCulture);
            ZoneEditor_EvictionTexture.Text = _editingZone.EvictionTexture;
            ZoneEditor_QuestDist.Text = _editingZone.QuestDistance.ToString("F0", CultureInfo.InvariantCulture);
            ZoneEditor_QuestMode.SelectedIndex = (int)_editingZone.DisplayMode;
            ZoneEditor_ShowEnemiesOutside.IsChecked = _editingZone.ShowEnemiesOutside;
            ZoneEditor_EnableWinFireworks.IsChecked = _editingZone.EnableWinFireworks;
            ZoneEditor_EnableLoseFireworks.IsChecked = _editingZone.EnableLoseFireworks;
            ZoneEditor_DiscordCapture.IsChecked = _editingZone.DiscordAnnounceCapture;
            ZoneEditor_DiscordDecay.IsChecked = _editingZone.DiscordAnnounceDecay;
            ZoneEditor_DiscordPoints.IsChecked = _editingZone.DiscordAnnouncePoints;
            ZoneEditor_DiscordEnter.IsChecked = _editingZone.DiscordAnnounceEnter;
            ZoneEditor_SchedMon.IsChecked = _editingZone.ScheduleMonday;
            ZoneEditor_SchedTue.IsChecked = _editingZone.ScheduleTuesday;
            ZoneEditor_SchedWed.IsChecked = _editingZone.ScheduleWednesday;
            ZoneEditor_SchedThu.IsChecked = _editingZone.ScheduleThursday;
            ZoneEditor_SchedFri.IsChecked = _editingZone.ScheduleFriday;
            ZoneEditor_SchedSat.IsChecked = _editingZone.ScheduleSaturday;
            ZoneEditor_SchedSun.IsChecked = _editingZone.ScheduleSunday;
            ZoneEditor_SchedStartH.Text = _editingZone.ScheduleStartHour.ToString();
            ZoneEditor_SchedStartM.Text = _editingZone.ScheduleStartMinute.ToString();
            ZoneEditor_SchedEndH.Text = _editingZone.ScheduleEndHour.ToString();
            ZoneEditor_SchedEndM.Text = _editingZone.ScheduleEndMinute.ToString();
            ZoneEditorPanel.Visibility = Visibility.Visible;
        }

        private void DeleteZone_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as KothZone;
            var persist = SenX_KOTH_PluginMain.ZonePersist;
            if (item == null || persist == null) return;

            ZoneManager.DeleteZone(persist.Data, item.Name);
            persist.Save();
            RefreshZoneNames();
        }
        private void FireworkWin_Click(object sender, RoutedEventArgs e)
        {
            var zone = (sender as Button)?.Tag as KothZone;
            if (zone == null) return;
            var evt = SenX_KOTH_PluginMain.Events.OfType<ZonePointEvent>()
                .FirstOrDefault(z => string.Equals(z.Name, zone.Name, StringComparison.OrdinalIgnoreCase));
            if (evt != null)
                QuestManager.SendFireworkToAll(evt, 1);
        }

        private void FireworkLose_Click(object sender, RoutedEventArgs e)
        {
            var zone = (sender as Button)?.Tag as KothZone;
            if (zone == null) return;
            var evt = SenX_KOTH_PluginMain.Events.OfType<ZonePointEvent>()
                .FirstOrDefault(z => string.Equals(z.Name, zone.Name, StringComparison.OrdinalIgnoreCase));
            if (evt != null)
                QuestManager.SendFireworkToAll(evt, 2);
        }

        private void EvictZone_Click(object sender, RoutedEventArgs e)
        {
            var zone = (sender as Button)?.Tag as KothZone;
            if (zone == null) return;
            var evt = SenX_KOTH_PluginMain.Events.OfType<ZonePointEvent>()
                .FirstOrDefault(z => string.Equals(z.Name, zone.Name, StringComparison.OrdinalIgnoreCase));
            evt?.ManualEvict();
        }

        private void ResetZone_Click(object sender, RoutedEventArgs e)
        {
            var zone = (sender as Button)?.Tag as KothZone;
            if (zone == null) return;
            var evt = SenX_KOTH_PluginMain.Events.OfType<ZonePointEvent>()
                .FirstOrDefault(z => string.Equals(z.Name, zone.Name, StringComparison.OrdinalIgnoreCase));
            evt?.ManualReset();
        }

        private void RefreshZonesList()
        {
            ZonesList.ItemsSource = SenX_KOTH_PluginMain.ZonePersist?.Data.Zones;
        }

        private void SaveZone_Click(object sender, RoutedEventArgs e)
        {
            if (_editingZone == null) return;

            string name = ZoneEditor_Name.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Zone name is required.");
                return;
            }

            if (!float.TryParse(ZoneEditor_Radius.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float radius) || radius < 10f || radius > 500f)
            {
                MessageBox.Show("Radius must be a number between 10 and 500.");
                return;
            }

            if (!float.TryParse(ZoneEditor_ColorR.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float cr)) cr = 0.53f;
            if (!float.TryParse(ZoneEditor_ColorG.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float cg)) cg = 0.81f;
            if (!float.TryParse(ZoneEditor_ColorB.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float cb)) cb = 0.92f;

            if (_isNewZone)
            {
                if (!double.TryParse(ZoneEditor_PosX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double px) ||
                    !double.TryParse(ZoneEditor_PosY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double py) ||
                    !double.TryParse(ZoneEditor_PosZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double pz))
                {
                    MessageBox.Show("All position fields must be valid numbers.");
                    return;
                }

                var persist = SenX_KOTH_PluginMain.ZonePersist;
                if (persist == null) return;

                if (persist.Data.Zones.Any(z => string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("A zone with this name already exists.");
                    return;
                }

                _editingZone.Name = name;
                _editingZone.X = px;
                _editingZone.Y = py;
                _editingZone.Z = pz;
                _editingZone.OriginX = px;
                _editingZone.OriginY = py;
                _editingZone.OriginZ = pz;
                _editingZone.Radius = radius;
                _editingZone.ColorR = cr;
                _editingZone.ColorG = cg;
                _editingZone.ColorB = cb;
                _editingZone.Texture = ZoneEditor_Texture.Text.Trim();
                _editingZone.Enabled = ZoneEditor_Enabled.IsChecked == true;
                _editingZone.PersistVisual = ZoneEditor_PersistVisual.IsChecked == true;
                _editingZone.DynamicZone = ZoneEditor_Dynamic.IsChecked == true;
                SaveCaptureFields();
                SaveDynamicFields();
                SaveEvictionFields();
                SaveQuestFields();
                SaveScheduleFields();

                ZoneManager.CreateSafeZoneEntity(_editingZone, new VRageMath.Vector3D(px, py, pz));
                persist.Data.Zones.Add(_editingZone);
                persist.Save();
            }
            else
            {
                _editingZone.Name = name;
                _editingZone.Radius = radius;
                _editingZone.ColorR = cr;
                _editingZone.ColorG = cg;
                _editingZone.ColorB = cb;
                _editingZone.Texture = ZoneEditor_Texture.Text.Trim();
                _editingZone.Enabled = ZoneEditor_Enabled.IsChecked == true;
                _editingZone.PersistVisual = ZoneEditor_PersistVisual.IsChecked == true;
                _editingZone.DynamicZone = ZoneEditor_Dynamic.IsChecked == true;
                SaveCaptureFields();
                SaveDynamicFields();
                SaveEvictionFields();
                SaveQuestFields();
                SaveScheduleFields();
                ZoneManager.UpdateZoneEntity(_editingZone);
                SenX_KOTH_PluginMain.ZonePersist?.Save();
            }

            ZoneEditorPanel.Visibility = Visibility.Collapsed;
            _editingZone = null;
            RefreshZoneNames();
        }

        private void SaveDynamicFields()
        {
            if (_editingZone == null) return;
            double.TryParse(ZoneEditor_MinDist.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double minDist);
            double.TryParse(ZoneEditor_MaxDist.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double maxDist);
            _editingZone.MinDistance = minDist;
            _editingZone.MaxDistance = maxDist;
            int.TryParse(ZoneEditor_MoveInterval.Text, out int interval);
            _editingZone.DynamicMoveIntervalSeconds = interval;
            _editingZone.AnnounceGps = ZoneEditor_AnnounceGps.IsChecked == true;
        }

        private void SaveEvictionFields()
        {
            if (_editingZone == null) return;
            _editingZone.EvictionEnabled = ZoneEditor_EvictionEnabled.IsChecked == true;
            int.TryParse(ZoneEditor_EvictionFreq.Text, out int freq);
            _editingZone.EvictionFrequencyMinutes = Math.Max(1, freq);
            int.TryParse(ZoneEditor_EvictionDuration.Text, out int duration);
            _editingZone.EvictionDurationSeconds = Math.Max(1, duration);
            _editingZone.EvictionResetCapture = ZoneEditor_EvictionResetCapture.IsChecked == true;
            _editingZone.EvictionDuringDowntime = ZoneEditor_EvictionDowntime.IsChecked == true;
            float.TryParse(ZoneEditor_EvictionColorR.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float cr);
            float.TryParse(ZoneEditor_EvictionColorG.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float cg);
            float.TryParse(ZoneEditor_EvictionColorB.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float cb);
            _editingZone.EvictionColorR = cr;
            _editingZone.EvictionColorG = cg;
            _editingZone.EvictionColorB = cb;
            _editingZone.EvictionTexture = ZoneEditor_EvictionTexture.Text.Trim();
        }

        private void SaveQuestFields()
        {
            if (_editingZone == null) return;
            _editingZone.DisplayMode = (QuestDisplayMode)Math.Max(0, Math.Min(2, ZoneEditor_QuestMode.SelectedIndex));
            var qdText = ZoneEditor_QuestDist.Text;
            if (float.TryParse(qdText, NumberStyles.Float, CultureInfo.InvariantCulture, out float qd) && qd >= 0f)
                _editingZone.QuestDistance = qd;
            _editingZone.ShowEnemiesOutside = ZoneEditor_ShowEnemiesOutside.IsChecked == true;
            _editingZone.EnableWinFireworks = ZoneEditor_EnableWinFireworks.IsChecked == true;
            _editingZone.EnableLoseFireworks = ZoneEditor_EnableLoseFireworks.IsChecked == true;
            _editingZone.DiscordAnnounceCapture = ZoneEditor_DiscordCapture.IsChecked == true;
            _editingZone.DiscordAnnounceDecay = ZoneEditor_DiscordDecay.IsChecked == true;
            _editingZone.DiscordAnnouncePoints = ZoneEditor_DiscordPoints.IsChecked == true;
            _editingZone.DiscordAnnounceEnter = ZoneEditor_DiscordEnter.IsChecked == true;
        }

        private void SaveScheduleFields()
        {
            if (_editingZone == null) return;
            _editingZone.ScheduleMonday = ZoneEditor_SchedMon.IsChecked == true;
            _editingZone.ScheduleTuesday = ZoneEditor_SchedTue.IsChecked == true;
            _editingZone.ScheduleWednesday = ZoneEditor_SchedWed.IsChecked == true;
            _editingZone.ScheduleThursday = ZoneEditor_SchedThu.IsChecked == true;
            _editingZone.ScheduleFriday = ZoneEditor_SchedFri.IsChecked == true;
            _editingZone.ScheduleSaturday = ZoneEditor_SchedSat.IsChecked == true;
            _editingZone.ScheduleSunday = ZoneEditor_SchedSun.IsChecked == true;
            int.TryParse(ZoneEditor_SchedStartH.Text, out int sh);
            int.TryParse(ZoneEditor_SchedStartM.Text, out int sm);
            int.TryParse(ZoneEditor_SchedEndH.Text, out int eh);
            int.TryParse(ZoneEditor_SchedEndM.Text, out int em);
            _editingZone.ScheduleStartHour = Math.Max(0, Math.Min(23, sh));
            _editingZone.ScheduleStartMinute = Math.Max(0, Math.Min(59, sm));
            _editingZone.ScheduleEndHour = Math.Max(0, Math.Min(23, eh));
            _editingZone.ScheduleEndMinute = Math.Max(0, Math.Min(59, em));
        }

        private void SaveCaptureFields()
        {
            if (_editingZone == null) return;
            int.TryParse(ZoneEditor_CapturePtsNeeded.Text, out int cp);
            _editingZone.CapturePointsNeeded = Math.Max(1, cp);
            int.TryParse(ZoneEditor_CapInterval.Text, out int ci);
            _editingZone.CapturePointIntervalSeconds = Math.Max(1, ci);
            int.TryParse(ZoneEditor_AwardInterval.Text, out int ai);
            _editingZone.PointAwardIntervalSeconds = Math.Max(1, ai);
            int.TryParse(ZoneEditor_PtsSuit.Text, out int ps);
            _editingZone.PointsPerSuit = ps;
            int.TryParse(ZoneEditor_PtsGrid.Text, out int pg);
            _editingZone.PointsPerGrid = pg;
        }

        private void CancelZone_Click(object sender, RoutedEventArgs e)
        {
            ZoneEditorPanel.Visibility = Visibility.Collapsed;
            _editingZone = null;
        }

        private void AddZoneReward_Click(object sender, RoutedEventArgs e)
        {
            _editingZoneReward = new ZoneRewardConfig();
            PopulateZoneRewardEditor(_editingZoneReward);
            ZoneRewardEditorPanel.Visibility = Visibility.Visible;
            HideZoneRewardCmdEditor();
        }

        private void EditZoneReward_Click(object sender, RoutedEventArgs e)
        {
            _editingZoneReward = (sender as Button)?.Tag as ZoneRewardConfig;
            if (_editingZoneReward == null) return;
            PopulateZoneRewardEditor(_editingZoneReward);
            ZoneRewardEditorPanel.Visibility = Visibility.Visible;
            HideZoneRewardCmdEditor();
        }

        private void DeleteZoneReward_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ZoneRewardConfig;
            var config = GetConfig();
            if (item != null && config != null)
                config.ZoneRewards.Remove(item);
        }

        private void PopulateZoneRewardEditor(ZoneRewardConfig zrc)
        {
            ZoneRewardEditor_Name.Text = zrc.ZoneName;
            ZoneRewardEditor_Threshold.Text = zrc.PointsForPrizeThreshold.ToString();
            ZoneRewardEditor_EveryCap.IsChecked = zrc.TriggerOnEveryCap;
            ZoneRewardCmdList.ItemsSource = zrc.CommandRewards;
        }

        private void SaveZoneReward_Click(object sender, RoutedEventArgs e)
        {
            if (_editingZoneReward == null) return;
            _editingZoneReward.ZoneName = ZoneRewardEditor_Name.Text;
            int.TryParse(ZoneRewardEditor_Threshold.Text, out int threshold);
            _editingZoneReward.PointsForPrizeThreshold = threshold;
            _editingZoneReward.TriggerOnEveryCap = ZoneRewardEditor_EveryCap.IsChecked == true;

            var config = GetConfig();
            if (config != null && !config.ZoneRewards.Contains(_editingZoneReward))
                config.ZoneRewards.Add(_editingZoneReward);

            ZoneRewardEditorPanel.Visibility = Visibility.Collapsed;
            _editingZoneReward = null;
        }

        private void CancelZoneReward_Click(object sender, RoutedEventArgs e)
        {
            ZoneRewardEditorPanel.Visibility = Visibility.Collapsed;
            HideZoneRewardCmdEditor();
            _editingZoneReward = null;
        }

        private void AddZoneRewardCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingZoneCmd = new LiveCommandReward();
            PopulateZoneRewardCmdEditor();
            ZoneRewardCmdEditor.Visibility = Visibility.Visible;
        }

        private void EditZoneRewardCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingZoneCmd = (sender as Button)?.Tag as LiveCommandReward;
            if (_editingZoneCmd == null) return;
            PopulateZoneRewardCmdEditor();
            ZoneRewardCmdEditor.Visibility = Visibility.Visible;
        }

        private void DeleteZoneRewardCmd_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as LiveCommandReward;
            if (item != null)
                _editingZoneReward?.CommandRewards.Remove(item);
            ZoneRewardCmdList.Items.Refresh();
        }

        private void PopulateZoneRewardCmdEditor()
        {
            if (_editingZoneCmd == null) return;
            ZoneCmd_PerFaction.IsChecked = _editingZoneCmd.PerFactionMember;
            ZoneCmd_OnlineOnly.IsChecked = _editingZoneCmd.OnlyOnlineMembers;
            ZoneCmd_Command.Text = _editingZoneCmd.CommandText;
        }

        private void SaveZoneRewardCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_editingZoneCmd == null) return;
            _editingZoneCmd.PerFactionMember = ZoneCmd_PerFaction.IsChecked == true;
            _editingZoneCmd.OnlyOnlineMembers = ZoneCmd_OnlineOnly.IsChecked == true;
            _editingZoneCmd.CommandText = ZoneCmd_Command.Text;

            if (_editingZoneReward != null && !_editingZoneReward.CommandRewards.Contains(_editingZoneCmd))
                _editingZoneReward.CommandRewards.Add(_editingZoneCmd);

            HideZoneRewardCmdEditor();
            ZoneRewardCmdList.Items.Refresh();
        }

        private void CancelZoneRewardCmd_Click(object sender, RoutedEventArgs e)
        {
            HideZoneRewardCmdEditor();
        }

        private void HideZoneRewardCmdEditor()
        {
            ZoneRewardCmdEditor.Visibility = Visibility.Collapsed;
            _editingZoneCmd = null;
        }

        private void AddRank_Click(object sender, RoutedEventArgs e)
        {
            _editingRank = new RankRewardEntry();
            _editingRankPeriod = 0;
            _editingThreshold = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void EditRank_Click(object sender, RoutedEventArgs e)
        {
            _editingRank = (sender as Button)?.Tag as RankRewardEntry;
            if (_editingRank == null) return;
            _editingRankPeriod = 0;
            _editingThreshold = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void DeleteRank_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as RankRewardEntry;
            var config = GetConfig();
            if (item != null && config != null)
                config.WeeklyRankRewards.Remove(item);
        }

        private void AddMonthlyRank_Click(object sender, RoutedEventArgs e)
        {
            _editingRank = new RankRewardEntry();
            _editingRankPeriod = 1;
            _editingThreshold = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void EditMonthlyRank_Click(object sender, RoutedEventArgs e)
        {
            _editingRank = (sender as Button)?.Tag as RankRewardEntry;
            if (_editingRank == null) return;
            _editingRankPeriod = 1;
            _editingThreshold = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void DeleteMonthlyRank_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as RankRewardEntry;
            var config = GetConfig();
            if (item != null && config != null)
                config.MonthlyRankRewards.Remove(item);
        }

        private void AddYearlyRank_Click(object sender, RoutedEventArgs e)
        {
            _editingRank = new RankRewardEntry();
            _editingRankPeriod = 2;
            _editingThreshold = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void EditYearlyRank_Click(object sender, RoutedEventArgs e)
        {
            _editingRank = (sender as Button)?.Tag as RankRewardEntry;
            if (_editingRank == null) return;
            _editingRankPeriod = 2;
            _editingThreshold = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void DeleteYearlyRank_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as RankRewardEntry;
            var config = GetConfig();
            if (item != null && config != null)
                config.YearlyRankRewards.Remove(item);
        }

        private void PopulateRankEditor()
        {
            if (_editingRank != null)
            {
                RankEditor_Label.Text = "Rank Number:";
                RankEditorPanel.Header = "Rank Editor";
                RankEditor_Rank.Text = _editingRank.Rank.ToString();
                RankCommandList.ItemsSource = _editingRank.Commands;
            }
            else if (_editingThreshold != null)
            {
                RankEditor_Label.Text = "Min Points:";
                RankEditorPanel.Header = "Threshold Editor";
                RankEditor_Rank.Text = _editingThreshold.MinPoints.ToString();
                RankCommandList.ItemsSource = _editingThreshold.Commands;
            }
        }

        private void SaveRank_Click(object sender, RoutedEventArgs e)
        {
            if (_editingThreshold != null)
            {
                SaveThreshold_Click(sender, e);
                return;
            }

            if (_editingRank == null) return;
            int.TryParse(RankEditor_Rank.Text, out int rank);
            _editingRank.Rank = rank;

            var config = GetConfig();
            if (config == null) return;

            if (_editingRankPeriod == 0 && !config.WeeklyRankRewards.Contains(_editingRank))
                config.WeeklyRankRewards.Add(_editingRank);
            else if (_editingRankPeriod == 1 && !config.MonthlyRankRewards.Contains(_editingRank))
                config.MonthlyRankRewards.Add(_editingRank);
            else if (_editingRankPeriod == 2 && !config.YearlyRankRewards.Contains(_editingRank))
                config.YearlyRankRewards.Add(_editingRank);

            RankEditorPanel.Visibility = Visibility.Collapsed;
            HideRankCmdEditor();
            _editingRank = null;
        }

        private void CancelRank_Click(object sender, RoutedEventArgs e)
        {
            RankEditorPanel.Visibility = Visibility.Collapsed;
            HideRankCmdEditor();
            _editingRank = null;
            _editingThreshold = null;
        }

        private void AddRankCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingRankCmd = new CommandRewardEntry();
            PopulateRankCmdEditor();
            RankCmdEditor.Visibility = Visibility.Visible;
        }

        private void EditRankCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingRankCmd = (sender as Button)?.Tag as CommandRewardEntry;
            if (_editingRankCmd == null) return;
            PopulateRankCmdEditor();
            RankCmdEditor.Visibility = Visibility.Visible;
        }

        private void DeleteRankCmd_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as CommandRewardEntry;
            if (item != null)
            {
                _editingRank?.Commands.Remove(item);
                _editingThreshold?.Commands.Remove(item);
            }
            RankCommandList.Items.Refresh();
        }

        private void PopulateRankCmdEditor()
        {
            if (_editingRankCmd == null) return;
            RankCmd_PerFaction.IsChecked = _editingRankCmd.PerFactionMember;
            RankCmd_OnlineOnly.IsChecked = _editingRankCmd.OnlyOnlineMembers;
            RankCmd_Command.Text = _editingRankCmd.CommandText;
        }

        private void SaveRankCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRaffleCmd != null)
            {
                SaveRaffleCmd_Click(sender, e);
                return;
            }

            if (_editingRankCmd == null) return;
            _editingRankCmd.PerFactionMember = RankCmd_PerFaction.IsChecked == true;
            _editingRankCmd.OnlyOnlineMembers = RankCmd_OnlineOnly.IsChecked == true;
            _editingRankCmd.CommandText = RankCmd_Command.Text;

            if (_editingRank != null && !_editingRank.Commands.Contains(_editingRankCmd))
                _editingRank.Commands.Add(_editingRankCmd);
            else if (_editingThreshold != null && !_editingThreshold.Commands.Contains(_editingRankCmd))
                _editingThreshold.Commands.Add(_editingRankCmd);

            HideRankCmdEditor();
            RankCommandList.Items.Refresh();
        }

        private void CancelRankCmd_Click(object sender, RoutedEventArgs e)
        {
            HideRankCmdEditor();
        }

        private void HideRankCmdEditor()
        {
            RankCmdEditor.Visibility = Visibility.Collapsed;
            _editingRankCmd = null;
        }

        private void CancelRaffleCmd_Click(object sender, RoutedEventArgs e)
        {
            HideRaffleCmdEditor();
        }

        private void HideRaffleCmdEditor()
        {
            RaffleCmdEditor.Visibility = Visibility.Collapsed;
            _editingRaffleCmd = null;
        }

        private void AddThreshold_Click(object sender, RoutedEventArgs e)
        {
            _editingThreshold = new ThresholdRewardEntry();
            _editingThresholdPeriod = 0;
            _editingRank = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void EditThreshold_Click(object sender, RoutedEventArgs e)
        {
            _editingThreshold = (sender as Button)?.Tag as ThresholdRewardEntry;
            if (_editingThreshold == null) return;
            _editingThresholdPeriod = 0;
            _editingRank = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void DeleteThreshold_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ThresholdRewardEntry;
            var config = GetConfig();
            if (item != null && config != null)
                config.WeeklyThresholdRewards.Remove(item);
        }

        private void AddMonthlyThreshold_Click(object sender, RoutedEventArgs e)
        {
            _editingThreshold = new ThresholdRewardEntry();
            _editingThresholdPeriod = 1;
            _editingRank = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void EditMonthlyThreshold_Click(object sender, RoutedEventArgs e)
        {
            _editingThreshold = (sender as Button)?.Tag as ThresholdRewardEntry;
            if (_editingThreshold == null) return;
            _editingThresholdPeriod = 1;
            _editingRank = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void DeleteMonthlyThreshold_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ThresholdRewardEntry;
            var config = GetConfig();
            if (item != null && config != null)
                config.MonthlyThresholdRewards.Remove(item);
        }

        private void AddYearlyThreshold_Click(object sender, RoutedEventArgs e)
        {
            _editingThreshold = new ThresholdRewardEntry();
            _editingThresholdPeriod = 2;
            _editingRank = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void EditYearlyThreshold_Click(object sender, RoutedEventArgs e)
        {
            _editingThreshold = (sender as Button)?.Tag as ThresholdRewardEntry;
            if (_editingThreshold == null) return;
            _editingThresholdPeriod = 2;
            _editingRank = null;
            PopulateRankEditor();
            RankEditorPanel.Visibility = Visibility.Visible;
            HideRankCmdEditor();
        }

        private void DeleteYearlyThreshold_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ThresholdRewardEntry;
            var config = GetConfig();
            if (item != null && config != null)
                config.YearlyThresholdRewards.Remove(item);
        }

        private void SaveThreshold_Click(object sender, RoutedEventArgs e)
        {
            if (_editingThreshold == null) return;
            int.TryParse(RankEditor_Rank.Text, out int min);
            _editingThreshold.MinPoints = min;

            var config = GetConfig();
            if (config == null) return;

            if (_editingThresholdPeriod == 0 && !config.WeeklyThresholdRewards.Contains(_editingThreshold))
                config.WeeklyThresholdRewards.Add(_editingThreshold);
            else if (_editingThresholdPeriod == 1 && !config.MonthlyThresholdRewards.Contains(_editingThreshold))
                config.MonthlyThresholdRewards.Add(_editingThreshold);
            else if (_editingThresholdPeriod == 2 && !config.YearlyThresholdRewards.Contains(_editingThreshold))
                config.YearlyThresholdRewards.Add(_editingThreshold);

            RankEditorPanel.Visibility = Visibility.Collapsed;
            HideRankCmdEditor();
            _editingThreshold = null;
        }

        private CommandRewardEntry? _editingRaffleCmd;
        private int _editingRafflePlace;

        private void AddRaffleFirstCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingRaffleCmd = new CommandRewardEntry();
            _editingRafflePlace = 0;
            PopulateRaffleCmdEditor();
            RaffleCmdEditor.Visibility = Visibility.Visible;
        }

        private void AddRaffleSecondCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingRaffleCmd = new CommandRewardEntry();
            _editingRafflePlace = 1;
            PopulateRaffleCmdEditor();
            RaffleCmdEditor.Visibility = Visibility.Visible;
        }

        private void AddRaffleThirdCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingRaffleCmd = new CommandRewardEntry();
            _editingRafflePlace = 2;
            PopulateRaffleCmdEditor();
            RaffleCmdEditor.Visibility = Visibility.Visible;
        }

        private void EditRaffleCmd_Click(object sender, RoutedEventArgs e)
        {
            _editingRaffleCmd = (sender as Button)?.Tag as CommandRewardEntry;
            if (_editingRaffleCmd == null) return;
            _editingRafflePlace = -1;
            PopulateRaffleCmdEditor();
            RaffleCmdEditor.Visibility = Visibility.Visible;
        }

        private void DeleteRaffleCmd_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as CommandRewardEntry;
            var config = GetConfig();
            if (item == null || config == null) return;
            config.RaffleFirstRewards.Remove(item);
            config.RaffleSecondRewards.Remove(item);
            config.RaffleThirdRewards.Remove(item);
        }

        private void PopulateRaffleCmdEditor()
        {
            if (_editingRaffleCmd == null) return;
            RaffleCmd_PerFaction.IsChecked = _editingRaffleCmd.PerFactionMember;
            RaffleCmd_OnlineOnly.IsChecked = _editingRaffleCmd.OnlyOnlineMembers;
            RaffleCmd_Command.Text = _editingRaffleCmd.CommandText;
        }

        private void SaveRaffleCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRaffleCmd == null) return;
            _editingRaffleCmd.PerFactionMember = RaffleCmd_PerFaction.IsChecked == true;
            _editingRaffleCmd.OnlyOnlineMembers = RaffleCmd_OnlineOnly.IsChecked == true;
            _editingRaffleCmd.CommandText = RaffleCmd_Command.Text;

            var config = GetConfig();
            if (config == null) return;

            if (_editingRafflePlace == 0 && !config.RaffleFirstRewards.Contains(_editingRaffleCmd))
                config.RaffleFirstRewards.Add(_editingRaffleCmd);
            else if (_editingRafflePlace == 1 && !config.RaffleSecondRewards.Contains(_editingRaffleCmd))
                config.RaffleSecondRewards.Add(_editingRaffleCmd);
            else if (_editingRafflePlace == 2 && !config.RaffleThirdRewards.Contains(_editingRaffleCmd))
                config.RaffleThirdRewards.Add(_editingRaffleCmd);

            HideRaffleCmdEditor();
            _editingRaffleCmd = null;
        }

        private void SendSampleAttackWebHook_Click(object sender, RoutedEventArgs e)
        {
            DiscordService.SendDiscordWebHook(WebhookEventType.Capture, "This is a test... test test test... you've just been tested... did it work?");
        }

        private void SendSampleRankWebHook_Click(object sender, RoutedEventArgs e)
        {
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, "First Place Vengeful Idiots with 2565 Points!", DrawingColor.Gold, 1);
            Thread.Sleep(5000);
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, "Second Place Space Nuggets with 1954 Points!", DrawingColor.Silver, 1);
            Thread.Sleep(5000);
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, "Third Place Legionly Legions with 584 Points!", DrawingColor.SandyBrown, 1);
            Thread.Sleep(5000);

            var sb = new StringBuilder();
            sb.AppendLine("The Other People....");
            sb.AppendLine("Hamsters of Europa with 486 Points!");
            sb.AppendLine("TRex's with 386 Points!");
            sb.AppendLine("Muppet Empire with 212 Points!");
            DiscordService.SendDiscordWebHook(WebhookEventType.RankResult, sb.ToString(), DrawingColor.Brown, 1);
        }

        private void AddWebhook_Click(object sender, RoutedEventArgs e)
        {
            SenX_KOTH_PluginConfig? config = GetConfig();
            config?.Webhooks.Add(new ());
            WebhookList.Items.Refresh();
        }

        private void DeleteWebhook_Click(object sender, RoutedEventArgs e)
        {
            var entry = (sender as Button)?.Tag as WebhookEntry;
            var config = GetConfig();
            if (entry != null && config != null)
            {
                config.Webhooks.Remove(entry);
                WebhookList.Items.Refresh();
            }
        }

        private void ClearPending_Click(object sender, RoutedEventArgs e)
        {
            var persist = SenX_KOTH_PluginMain.PendingCreditsPersist;
            if (persist == null) return;
            persist.Data.Credits.Clear();
            persist.Save();
        }

        private void BrowseSharedPath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select shared data folder";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var config = GetConfig();
                if (config != null) config.SharedDataPath = dialog.SelectedPath;
            }
        }

        private void SendSampleEnterAlertWebHook_Click(object sender, RoutedEventArgs e)
        {
            var config = GetConfig();
            if (config == null) return;
            var testMsg = config.EnterAlert_MessageTemplate
                ?.Replace("{player}", "TestPlayer")
                ?.Replace("{steamId}", "123456789")
                ?.Replace("{factionTag}", "[TEST]")
                ?.Replace("{factionName}", "Test Faction")
                ?.Replace("{zoneName}", "Arena 1")
                ?.Replace("{x}", "15000").Replace("{y}", "25000").Replace("{z}", "35000")
                ?.Replace("{radius}", "500")
                ?.Replace("{time}", DateTime.UtcNow.ToString("HH:mm:ss UTC"))
                ?? "TestPlayer entered zone";
            DiscordService.SendAlertWebHook(testMsg);
        }
    }
}

namespace KoTH.Converters
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString || value is null || !Enum.IsDefined(value.GetType(), value)) return DependencyProperty.UnsetValue;
            object parameterValue = Enum.Parse(value.GetType(), parameterString);
            return parameterValue.Equals(value);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string parameterString) return DependencyProperty.UnsetValue;
            return Enum.Parse(targetType, parameterString);
        }
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is false;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is false;
    }
}
