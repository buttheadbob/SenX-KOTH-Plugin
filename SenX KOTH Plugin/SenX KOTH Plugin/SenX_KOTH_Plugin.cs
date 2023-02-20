using NLog;
using Sandbox;
using System;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using SenX_KOTH_Plugin.Utils;
using Sandbox.ModAPI;
using VRage;
using Newtonsoft.Json;

namespace SenX_KOTH_Plugin
{
    public sealed class SenX_KOTH_PluginMain : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => Main");
        public static ScoreFile MasterScore;
        private static readonly string CONFIG_FILE_NAME = "SenX_KOTH_PluginConfig.cfg";
        private static readonly FastResourceLock resourceLock = new FastResourceLock();
        LiveAgent resetAgent = new LiveAgent();

        private SenX_KOTH_PluginControl _control;
        public UserControl GetControl() => _control ?? (_control = new SenX_KOTH_PluginControl());
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
                    MasterScore = Load_MasterData();
                    resetAgent.Run();                    
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    Save_MasterData(MasterScore);
                    resetAgent.Dispose();
                    break;
            }
        }

        private void SetupConfig()
        {
            string configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

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

        public static Session ScoresFromStorage()
        {
            Session UpdateScore;

            if (!File.Exists(Path.Combine(MySandboxGame.ConfigDedicated.LoadWorld, @"Storage\2388326362.sbm_koth\Scores.data")))
            {
                File.Create(Path.Combine(MySandboxGame.ConfigDedicated.LoadWorld, @"Storage\2388326362.sbm_koth\Scores.data"));
            }
            
            using (TextReader reader = File.OpenText(Path.Combine(MySandboxGame.ConfigDedicated.LoadWorld, @"Storage\2388326362.sbm_koth\Scores.data")))
            {
                string text = reader.ReadToEnd();
                reader.Dispose();

                UpdateScore = MyAPIGateway.Utilities.SerializeFromXML<Session>(text);
            }
            
            return UpdateScore;            
        }

        private static ScoreFile Load_MasterData()
        {
            ScoreFile ScoreFile;

            if (!File.Exists(Path.Combine(Instance.StoragePath, "KoTH_ScoreCard.dat")))
            {
                using (StreamWriter sw = File.CreateText(Path.Combine(Instance.StoragePath, "KoTH_ScoreCard.dat")))
                {
                    var Empty_MasterScore = new ScoreFile();
                    string text = JsonConvert.SerializeObject(Empty_MasterScore);
                    sw.Write(text);
                }
            }

            using (resourceLock.AcquireExclusiveUsing())
            {
                TextReader reader = File.OpenText(Path.Combine(Instance.StoragePath, "KoTH_ScoreCard.dat"));
                string text = reader.ReadToEnd();
                reader.Dispose();

                if (string.IsNullOrEmpty(text))
                {
                    return new ScoreFile();
                }

                if (text.Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?>")) // Original version used XML, now it uses JSON.
                {
                    Log.Warn("KoTH plugin data file was not compatible for an upgrade.  This was caused because the plugin " +
                             "no longer uses XML but JSON instead and I this change was made early in the plugins " +
                             "lifetime cycle.");
                    return new ScoreFile();
                }

                ScoreFile = JsonConvert.DeserializeObject<ScoreFile>(text);
            }

            return ScoreFile;
        }

        public static bool Save_MasterData(ScoreFile MasterData)
        {            
            using (resourceLock.AcquireExclusiveUsing())
            {
                File.WriteAllText(Path.Combine(Instance.StoragePath, "KoTH_ScoreCard.dat"),JsonConvert.SerializeObject(MasterData, Formatting.Indented));
            }
            return true;
        }
    }
}
