using System.Windows;
using System.Windows.Controls;
using Sandbox.Engine.Utils;
using SenX_KOTH_Plugin.Utils;

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

        private void SendTestWebHook_Click(object sender, RoutedEventArgs e)
        {
            DiscordService.SendDiscordWebHook("This is a test... test test test... you've just been tested... did it work?");
        }
    }
}
