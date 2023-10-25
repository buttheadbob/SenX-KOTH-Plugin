using NLog;
using Sandbox;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using SenX_KOTH_Plugin.Utils;
using Sandbox.ModAPI;
using Newtonsoft.Json;
using Nexus.API;
using Torch.Managers;
using Torch.Managers.PatchManager;

namespace SenX_KOTH_Plugin
{
    public sealed class SenX_KOTH_PluginMain : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => Main");
        public static ScoreFile MasterScore = new();
        private static readonly string CONFIG_FILE_NAME = "SenX_KOTH_PluginConfig.cfg";
        private static readonly object _fileLock = new ();
        private static readonly TimeSpan _lockTimeOut = TimeSpan.FromMilliseconds(500);
        public static LiveAgent? resetAgent;
        public static NexusAPI? nexusAPI { get; private set; }
        
        private static readonly Guid NexusGUID = new ("28a12184-0422-43ba-a6e6-2e228611cca5");
        public static bool NexusInstalled { get; private set; } = false;
        public static bool NexusInited;

        private SenX_KOTH_PluginControl? _control;
        public UserControl GetControl() => _control ?? (_control = new SenX_KOTH_PluginControl());
        private Persistent<SenX_KOTH_PluginConfig>? _config;
        public SenX_KOTH_PluginConfig? Config => _config?.Data;
        public static SenX_KOTH_PluginMain? Instance;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            SetupConfig();

            TorchSessionManager sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Instance = this;
            Save();
            resetAgent = new LiveAgent();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    Network.NetworkService.NetworkInit();
                    MasterScore = Load_MasterData();
                    resetAgent?.Run();
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    Save_MasterData(MasterScore);
                    resetAgent?.Dispose();
                    break;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!NexusInited)
            {
                PluginManager? _pluginManager = Torch.Managers.GetManager<PluginManager>();
                PatchManager? _patchManager = Torch.Managers.GetManager<PatchManager>();
                
                if (_pluginManager.Plugins.TryGetValue(NexusGUID, out ITorchPlugin torchPlugin))
                {
                    Type? type = torchPlugin.GetType();
                    Type? type2 = type != null! ? type.Assembly.GetType("Nexus.API.PluginAPISync") : null;
                    if (type2 != null)
                    {
                        type2.GetMethod("ApplyPatching", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[]
                        {
                            typeof(NexusAPI),
                            "Distress"
                        });
                        nexusAPI = new NexusAPI(8542);
                        MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8542, new Action<ushort, byte[], ulong, bool>(NexusManager.HandleNexusMessage));
                        NexusInstalled = true;
                    }
                }
                NexusInited = true;
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
                if (_config is null) return;
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        public static Session? ScoresFromStorage()
        {
            bool lockTaken = false;

            if (!File.Exists(Path.Combine(MySandboxGame.ConfigDedicated.LoadWorld, @"Storage\2388326362.sbm_koth\Scores.data")))
            {
                File.Create(Path.Combine(MySandboxGame.ConfigDedicated.LoadWorld, @"Storage\2388326362.sbm_koth\Scores.data"));
            }

            try
            {
                Monitor.TryEnter(_fileLock, _lockTimeOut, ref lockTaken);
                if (lockTaken)
                {
                    using TextReader reader = File.OpenText(Path.Combine(MySandboxGame.ConfigDedicated.LoadWorld, @"Storage\2388326362.sbm_koth\Scores.data"));
                    string text = reader.ReadToEnd();
                    reader.Dispose();

                    Session? UpdateScore = MyAPIGateway.Utilities.SerializeFromXML<Session>(text);
                    
                    return UpdateScore;
                }

            }
            catch (Exception e)
            {
                Log.Warn(e);
                return null;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(_fileLock);
            }

            return null;
        }

        private static ScoreFile Load_MasterData()
        {
            string data = "";
            bool lockTaken = false;
            ScoreFile? ScoreFile;

            try
            {
                Monitor.TryEnter(_fileLock, _lockTimeOut, ref lockTaken);
                if (lockTaken)
                {
                    if (!File.Exists(Path.Combine(Instance!.StoragePath, "KoTH_ScoreCard.dat")))
                    {
                        using StreamWriter sw = File.CreateText(Path.Combine(Instance.StoragePath, "KoTH_ScoreCard.dat"));
                        ScoreFile Empty_MasterScore = new ();
                        data = JsonConvert.SerializeObject(Empty_MasterScore);
                        sw.Write(data);
                        return new ScoreFile();
                    }

                    using StreamReader sr = new (Path.Combine(Instance.StoragePath, "KoTH_ScoreCard.dat"));
                    data = sr.ReadToEnd();
                }
            }catch(Exception e)
            {
                Log.Warn(e);
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(_fileLock);
            }

            if (string.IsNullOrEmpty(data))
            {
                return new ScoreFile();
            }

            ScoreFile = JsonConvert.DeserializeObject<ScoreFile>(data);

            ScoreFile? file = ScoreFile;
            
            // If a new score file is created, make sure the lists are not null as json will store them as null in some versions
            if (file != null)
            {
                if (file.WeekScores is null)
                    file.WeekScores = new ();
                
                if (file.MonthScores is null)
                    file.MonthScores = new ();
                
                if (file.YearScores is null)
                    file.YearScores = new ();
                
                return file;
            }

            return new ScoreFile();
        }

        public static bool Save_MasterData(ScoreFile MasterData)
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_fileLock, _lockTimeOut, ref lockTaken);
                if (lockTaken)
                {
                    File.WriteAllText(Path.Combine(Instance!.StoragePath, "KoTH_ScoreCard.dat"),JsonConvert.SerializeObject(MasterData, Formatting.Indented));
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Warn(e);
                return false;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(_fileLock);
            }
            Log.Warn("Unable to save KoTH_ScoreCard.dat, lock timed out while file in use.");
            return false;
        }
    }

}
