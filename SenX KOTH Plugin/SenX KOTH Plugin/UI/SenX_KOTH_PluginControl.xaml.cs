﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Sandbox.Engine.Utils;
using SenX_KOTH_Plugin.Utils;
using Torch.Mod.Messages;
using Torch.Mod;
using System.Drawing;
using System.Linq;

namespace SenX_KOTH_Plugin
{
    public partial class SenX_KOTH_PluginControl : UserControl
    {
        public SenX_KOTH_PluginControl()
        {
            DataContext = SenX_KOTH_PluginMain.Instance.Config;
            InitializeComponent();
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SenX_KOTH_PluginMain.Instance.Save();
        }

        private void SendSampleAttackWebHook_Click(object sender, RoutedEventArgs e)
        {
            DiscordService.SendDiscordWebHook("This is a test... test test test... you've just been tested... did it work?");
        }

        private void SendSampleRankkWebHook_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder WeekResults = new StringBuilder();

            // Create a formatted ranking list
            var weeklist = SenX_KOTH_PluginMain.MasterScore.WeekScores.ToList();

            // Sort the list.
            weeklist.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Push list to weekresults
            for (int a = 0; a < weeklist.Count; a++) 
            {
                if (a < 3)
                {
                    DiscordService.SendDiscordWebHook($"**Rank {a}** {weeklist[a].Key} with {weeklist[a].Value} points.", Color.Gold, 1);
                    continue;
                }

                WeekResults.AppendLine($"**Rank {a}** {weeklist[a].Key} with {weeklist[a].Value} points.");
            }

            DiscordService.SendDiscordWebHook(WeekResults.ToString(), Color.Gold, 1);
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
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }
}
