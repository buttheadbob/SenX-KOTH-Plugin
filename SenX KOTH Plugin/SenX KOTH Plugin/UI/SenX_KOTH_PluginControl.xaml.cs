using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SenX_KOTH_Plugin.Utils;
using System.Drawing;
using System.Linq;
using static SenX_KOTH_Plugin.SenX_KOTH_PluginMain;
using System.Collections.Generic;

namespace SenX_KOTH_Plugin
{
    public partial class SenX_KOTH_PluginControl : UserControl
    {
        public SenX_KOTH_PluginControl()
        {
            DataContext = Instance.Config;
            InitializeComponent();
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Instance.Save();
        }

        private void SendSampleAttackWebHook_Click(object sender, RoutedEventArgs e)
        {
            DiscordService.SendDiscordWebHook("This is a test... test test test... you've just been tested... did it work?");
        }

        private void SendSampleRankWebHook_Click(object sender, RoutedEventArgs e)
        {
            var WeekResults = new StringBuilder();
            var weekList = new List<KeyValuePair<string, int>>();

            // Create a formatted ranking list
            if (MasterScore.WeekScores != null)
            {
                weekList = MasterScore.WeekScores.ToList();
            } 

            // Sort the list.
            weekList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
             
            // Push list to WeekResults
            for (var a = 0; a < weekList.Count; a++) 
            {
                if (a < 3)
                {
                    DiscordService.SendDiscordWebHook($"**Rank {a}** {weekList[a].Key} with {weekList[a].Value} points.", Color.Gold, 1);
                    continue;
                }

                WeekResults.AppendLine($"**Rank {a}** {weekList[a].Key} with {weekList[a].Value} points.");
            }

            DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Gold, 1);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            switch (SenX_KOTH_PluginMain.Instance.Config.DefaultEmbedPic)
            {
                case true:
                    Instance.Config.DefaultEmbedPic = false;
                    Instance.Save();
                    break;

                case false: 
                    Instance.Config.DefaultEmbedPic = true;
                    Instance.Save();
                    break;
            }
        }
    }
}

namespace KoTH.Converters
{
    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is string parameterString))
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            var parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is string parameterString))
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }
}
