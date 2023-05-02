using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SenX_KOTH_Plugin.Utils;
using System.Drawing;
using static SenX_KOTH_Plugin.SenX_KOTH_PluginMain;
using System.Threading;

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
            DiscordService.SendDiscordWebHook("First Place Vengeful Idiots with 2565 Points!", Color.Gold, 1);
            Thread.Sleep(5000);
            DiscordService.SendDiscordWebHook("Second Place Space Nuggets with 1954 Points!", Color.Silver, 1);
            Thread.Sleep(5000);
            DiscordService.SendDiscordWebHook("Third Place Legionly Legions with 584 Points!", Color.SandyBrown, 1);
            Thread.Sleep(5000);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The Other People....");
            sb.AppendLine("Hamsters of Europa with 486 Points!");
            sb.AppendLine("TRex's with 386 Points!");
            sb.AppendLine("Muppet Empire with 212 Points!");
            DiscordService.SendDiscordWebHook(sb.ToString(), Color.Brown, 1);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            switch (Instance.Config.DefaultEmbedPic)
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
