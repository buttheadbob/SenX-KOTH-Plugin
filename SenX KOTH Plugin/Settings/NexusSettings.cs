namespace SenX_KOTH_Plugin
{
    public partial class SenX_KOTH_PluginConfig
    {
        private bool _isLobby;
        public bool isLobby { get => _isLobby; set => SetValue(ref _isLobby, value); }
    }
}