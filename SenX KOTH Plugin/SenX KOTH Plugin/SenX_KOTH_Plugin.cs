using NLog;
using Sandbox;
using System;
using System.IO;
using System.Windows.Controls;
using System.Xml.Serialization;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin
{
    public class SenX_KOTH_PluginMain : TorchPluginBase, IWpfPlugin
    {
        public static readonly Logger Log = LogManager.GetLogger("KOTH => Main");

        private static readonly string CONFIG_FILE_NAME = "SenX_KOTH_PluginConfig.cfg";

        private SenX_KOTH_PluginControl _control;
        public UserControl GetControl() => _control ?? (_control = new SenX_KOTH_PluginControl());
        public static string KothScorePath = "";
        private Persistent<SenX_KOTH_PluginConfig> _config;
        public SenX_KOTH_PluginConfig Config => _config?.Data;

        public static SenX_KOTH_PluginMain Instance { get; private set; }
        
        
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            SetupConfig();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Instance = this;
            Save();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            switch (state)
            {

                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    Network.NetworkService.NetworkInit();
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    break;
            }
        }

        private void SetupConfig()
        {

            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {

                _config = Persistent<SenX_KOTH_PluginConfig>.Load(configFile);

            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<SenX_KOTH_PluginConfig>(configFile, new SenX_KOTH_PluginConfig());
                _config.Save();
            }
        }

        public void Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        public static void SetPath()
        {
            var kothScoreName = MySandboxGame.ConfigDedicated.LoadWorld;
            KothScorePath = Path.Combine(kothScoreName, @"Storage\2388326362.sbm_koth\Scores.data");
        }

        public static session ScoresFromStorage()
        {
            var serializer = new XmlSerializer(typeof(session));
            using (var reader = new StreamReader(KothScorePath))
            {
                return (session)serializer.Deserialize(reader);
            }
        }
    }
}
