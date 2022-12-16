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
            DiscordService.SendDiscordWebHook("Fweeox Is Dominating The KOTH Zone!!!!  Unknown Wins (he makes them dissapear...), 0 Losses... Pure Brutality!!");
        }
    }
}
