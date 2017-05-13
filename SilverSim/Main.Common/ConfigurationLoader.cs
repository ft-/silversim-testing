// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using log4net.Config;
using Nini.Config;
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.RegionLoader;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.ServiceInterfaces.PortControl;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Updater;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Timers;

namespace SilverSim.Main.Common
{
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    public sealed partial class ConfigurationLoader : IServerParamAnyListener
    {
        #region Resource Assets support
        [Description("Resource Asset Backend")]
        sealed class ResourceAssetPlugin : SceneInterface.ResourceAssetService, IPlugin
        {
            public ResourceAssetPlugin()
            {

            }

            public void Startup(ConfigurationLoader loader)
            {
                /* intentionally left empty */
            }
        }
        #endregion

        #region Exceptions
        [Serializable]
        public class TestingErrorException : Exception
        {
            public TestingErrorException()
            {

            }

            public TestingErrorException(string message)
                : base(message)
            {

            }

            protected TestingErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public TestingErrorException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        [Serializable]
        public class ConfigurationErrorException : Exception
        {
            public ConfigurationErrorException()
            {

            }

            public ConfigurationErrorException(string msg)
                : base(msg)
            {

            }

            protected ConfigurationErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public ConfigurationErrorException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        [Serializable]
        public class ServiceNotFoundException : Exception
        {
            public ServiceNotFoundException()
            {

            }

            public ServiceNotFoundException(string msg)
                : base(msg)
            {

            }

            protected ServiceNotFoundException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public ServiceNotFoundException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }
        #endregion

        ~ConfigurationLoader()
        {
            if (!string.IsNullOrEmpty(m_PIDFile))
            {
                try
                {
                    File.Delete(m_PIDFile);
                }
                catch (Exception e)
                {
                    m_Log.Error(string.Format("Error removing PID file \"{0}\"", m_PIDFile), e);
                }
            }
            m_ShutdownEvent.Dispose();
        }

        readonly ILog m_Log;
        readonly ILog m_UpdaterLog;
        readonly IConfigSource m_Config = new IniConfigSource();
        readonly Queue<ICFG_Source> m_Sources = new Queue<ICFG_Source>();
        readonly RwLockedDictionary<string, IPlugin> PluginInstances = new RwLockedDictionary<string, IPlugin>();
        readonly ManualResetEvent m_ShutdownEvent;
        static public readonly Dictionary<Type, string> FeaturesTable = new Dictionary<Type, string>();
        readonly RwLockedDictionary<string, string> m_HeloResponseHeaders = new RwLockedDictionary<string, string>();
        public readonly RwLockedList<string> KnownConfigurationIssues = new RwLockedList<string>();
        static readonly RwLockedDictionary<string, Assembly> PreloadPlatformAssemblies = new RwLockedDictionary<string, Assembly>();
        public readonly SceneList Scenes = new SceneList();
        public readonly IMRouter IMRouter = new IMRouter();
        public readonly CmdIO.CommandRegistry CommandRegistry = new CmdIO.CommandRegistry();

        static string m_InstallationBinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string InstallationBinPath
        {
            get
            {
                return m_InstallationBinPath;
            }
        }

        #region Simulator Shutdown Handler
        readonly System.Timers.Timer m_ShutdownTimer = new System.Timers.Timer(1000);
        int m_ShutdownInSeconds = -1;
        bool m_FirstShutdownNotice;
        readonly object m_ShutdownTimerLock = new object();

        int ShutdownInSeconds
        {
            get
            {
                return m_ShutdownInSeconds;
            }
            set
            {
                lock(m_ShutdownTimerLock)
                {
                    if (value > 0)
                    {
                        m_ShutdownInSeconds = value;
                    }
                }
            }
        }

        static Action<int> SimulatorShutdownDelegate; /* used for Scene.Management registration */
        static public Action SimulatorShutdownAbortDelegate; /* used for Scene.Management registration */

        void ShutdownTimerEventHandler(object o, ElapsedEventArgs evargs)
        {
            int timeLeft;
            lock(m_ShutdownTimerLock)
            {
                timeLeft = m_ShutdownInSeconds--;
            }

            if(timeLeft < 0)
            {
                /* probably a shutdown abort */
                return;
            }

            if(timeLeft % 15 == 0 || m_FirstShutdownNotice)
            {
                m_FirstShutdownNotice = false;
                if (null != SimulatorShutdownDelegate)
                {
                    SimulatorShutdownDelegate.Invoke(timeLeft);
                }

                m_Log.InfoFormat("Simulator shutdown in {0} seconds", timeLeft);
            }
            if (timeLeft == 0)
            {
                m_ShutdownTimer.Stop();
                TriggerShutdown();
            }
        }

        public void RequestSimulatorShutdown(int timeUntilShutdown)
        {
            AbortSimulatorShutdown(true);
            if(timeUntilShutdown < 1)
            {
                return;
            }
            ShutdownInSeconds = timeUntilShutdown;
            m_FirstShutdownNotice = true;
            m_ShutdownTimer.Start();
        }

        public void AbortSimulatorShutdown(bool quietAbort = false)
        {
            bool sendAbortNotice = false;
            lock(m_ShutdownTimerLock)
            {
                sendAbortNotice = (m_ShutdownInSeconds > 0);
                m_ShutdownInSeconds = -1;
                m_ShutdownTimer.Stop();
            }

            if(null != SimulatorShutdownAbortDelegate && sendAbortNotice && !quietAbort)
            {
                m_Log.Info("Simulator shutdown is aborted.");
                SimulatorShutdownAbortDelegate();
            }
        }
        #endregion

        #region Helo Responder
        public void SetHeloResponseHeader(string key, string val)
        {
            m_HeloResponseHeaders[key] = val;
        }

        RwLockedDictionary<string, int> m_XProtocolsProvided = new RwLockedDictionary<string, int>();
        public void AddHeloProtocolsProvided(string protocol, int priority)
        {
            m_XProtocolsProvided[protocol] = -priority;
            List<string> list = new List<string>();
            foreach(KeyValuePair<string, int> item in m_XProtocolsProvided.OrderBy(key => key.Value))
            {
                list.Add(item.Key);
            }
            m_HeloResponseHeaders["X-Protocols-Provided"] = string.Join(",", list);
        }

        public void HeloResponseHandler(HttpRequest req)
        {
            if(req.Method != "GET" && req.Method != "HEAD")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            using (HttpResponse res = req.BeginResponse())
            {
                res.ContentType = "text/plain";
                foreach (KeyValuePair<string, string> kvp in m_HeloResponseHeaders)
                {
                    res.Headers.Add(kvp.Key, kvp.Value);
                }
                if (Scenes.Count != 0)
                {
                    res.Headers["X-UDP-InterSim"] = "supported";
                }
            }
        }
        #endregion

        static Assembly m_MonoSecurity;

        static Assembly ResolveMonoSecurityEventHandler(object sender, ResolveEventArgs args)
        {
            AssemblyName aName = new AssemblyName(args.Name);
            if (aName.Name == "Mono.Security")
            {
                return m_MonoSecurity;
            }
            return null;
        }

        static ConfigurationLoader()
        {
            /* Mono.Security is providing some subtle issues when trying to load 4.0.0.0 on Mono 4.4 to 4.6.
             * So, we make our dependency being loaded by an assembly that allows preloading the assembly on Win. 
             */
            if(!VersionInfo.IsPlatformMono)
            {
                m_MonoSecurity = Assembly.LoadFile(Path.Combine(InstallationBinPath, "platform-libs/Mono.Security.dll"));
                AppDomain.CurrentDomain.AssemblyResolve += ResolveMonoSecurityEventHandler;
            }

            /* prevent circular dependencies by assigning relevant parts here */
            ObjectGroup.CompilerRegistry = CompilerRegistry.ScriptCompilers;
            FeaturesTable[typeof(IPluginFactory)] = "Plugin Factory";
            FeaturesTable[typeof(IPluginSubFactory)] = "Plugin Sub Factory";

            FeaturesTable[typeof(AssetServiceInterface)] = "Asset Service";
            FeaturesTable[typeof(InventoryServiceInterface)] = "Inventory Service";
            FeaturesTable[typeof(AvatarNameServiceInterface)] = "Avatar Name Lookup Service";
            FeaturesTable[typeof(PresenceServiceInterface)] = "Presence Service";
            FeaturesTable[typeof(GridServiceInterface)] = "Grid Service";
            FeaturesTable[typeof(GridUserServiceInterface)] = "GridUser Service";
            FeaturesTable[typeof(AvatarServiceInterface)] = "Avatar Service";
            FeaturesTable[typeof(UserAccountServiceInterface)] = "UserAccount Service";
            FeaturesTable[typeof(IInventoryServicePlugin)] = "Inventory Service HELO Instantiator";
            FeaturesTable[typeof(IFriendsServicePlugin)] = "FriendsService HELO Instantiator";
            FeaturesTable[typeof(IAssetServicePlugin)] = "Asset Service HELO Instantiator";
            FeaturesTable[typeof(IUserAgentServicePlugin)] = "UserAgent Service HELO Instantiator";
            FeaturesTable[typeof(SimulationDataStorageInterface)] = "Simulation Data Storage";
            FeaturesTable[typeof(EstateServiceInterface)] = "Estate Service";
            FeaturesTable[typeof(GroupsServiceInterface)] = "Groups Service";
            FeaturesTable[typeof(ProfileServiceInterface)] = "Profile Service";
            FeaturesTable[typeof(NeighborServiceInterface)] = "Neighbor Signaling Service";
            FeaturesTable[typeof(FriendsServiceInterface)] = "Friends Service";

            FeaturesTable[typeof(IPluginShutdown)] = "Shutdown Handler";
            FeaturesTable[typeof(IDBServiceInterface)] = "DataBase Service";
            FeaturesTable[typeof(BaseHttpServer)] = "HTTP Server";
            FeaturesTable[typeof(IScriptCompiler)] = "Script Compiler";
            FeaturesTable[typeof(IScriptApi)] = "Script Api";
            FeaturesTable[typeof(ITerrainFileStorage)] = "Terrain File Format";
            FeaturesTable[typeof(IRegionLoaderInterface)] = "Region Loader";
            FeaturesTable[typeof(CmdIO.TTY)] = "Console";
            FeaturesTable[typeof(SceneInterface)] = "Scene Implementation";
            FeaturesTable[typeof(HttpXmlRpcHandler)] = "XML RPC Server";
            FeaturesTable[typeof(CapsHttpRedirector)] = "Capability Redirector";
            FeaturesTable[typeof(HttpJson20RpcHandler)] = "JSON2.0RPC Server";
            FeaturesTable[typeof(IServerParamListener)] = "Server Params";
            FeaturesTable[typeof(IPortControlServiceInterface)] = "Port Control";

            AppDomain.CurrentDomain.AssemblyResolve += ArchSpecificResolveEventHandler;
        }

        public IConfigSource Config
        {
            get
            {
                return m_Config;
            }
        }

        #region Plugin Registry
        readonly bool m_ServerParamInitialLoadProcessed;

        public void AddPlugin(string name, IPlugin plugin)
        {
            PluginInstances.Add("$" + name, plugin);
            if(m_ServerParamInitialLoadProcessed)
            {
                LoadServerParamsForPlugin(name, plugin, new Dictionary<string, List<KeyValuePair<UUID, string>>>());
            }
        }

        public ServerParamServiceInterface GetServerParamStorage()
        {
            return GetService<ServerParamServiceInterface>("ServerParamStorage");
        }

        public Dictionary<string, IPlugin> AllServices
        {
            get
            {
                return new Dictionary<string, IPlugin>(PluginInstances);
            }
        }

        public T GetService<T>(string serviceName)
        {
            IPlugin module;
            if(!PluginInstances.TryGetValue(serviceName, out module))
            {
                throw new ServiceNotFoundException(string.Format("Service {0} not found", serviceName));
            }
            if(!typeof(T).IsAssignableFrom(module.GetType()))
            {
                throw new InvalidOperationException("Unexpected module configured for service " + serviceName);
            }
            return (T)module;
        }

        public T GetPluginService<T>(string serviceName)
        {
            return GetService<T>("$" + serviceName);
        }

        public List<T> GetServicesByValue<T>()
        {
            List<T> list = new List<T>();
            foreach (IPlugin module in PluginInstances.Values)
            {
                if (typeof(T).IsAssignableFrom(module.GetType()))
                {
                    list.Add((T)module);
                }
            }
            return list;
        }

        public BaseHttpServer HttpServer
        {
            get
            {
                return GetService<BaseHttpServer>("HttpServer");
            }
        }

        public BaseHttpServer HttpsServer
        {
            get
            {
                return GetService<BaseHttpServer>("HttpsServer");
            }
        }

        public HttpXmlRpcHandler XmlRpcServer
        {
            get
            {
                return GetService<HttpXmlRpcHandler>("XmlRpcServer");
            }
        }

        public HttpJson20RpcHandler Json20RpcServer
        {
            get
            {
                return GetService<HttpJson20RpcHandler>("JSON2.0RpcServer");
            }
        }

        public CapsHttpRedirector CapsRedirector
        {
            get
            {
                return GetService<CapsHttpRedirector>("CapsRedirector");
            }
        }

        #endregion

        string m_GatekeeperURI = string.Empty;

        /** <summary>specifies the inter-grid region management server (not updated after any Startup has been called)</summary> */
        public string GatekeeperURI
        {
            get
            {
                return m_GatekeeperURI;
            }
            set
            {
                if(!value.EndsWith("/"))
                {
                    value += "/";
                }
                m_GatekeeperURI = value;
            }
        }

        string m_HomeURI = string.Empty;

        /** <summary>specifies the user server (not updated after any Startup has been called)</summary> */
        public string HomeURI
        {
            get
            {
                return m_HomeURI;
            }
            set
            {
                if(!value.EndsWith("/"))
                {
                    value += "/";
                }
                m_HomeURI = value;
            }
        }

        public string GridNick
        {
            get; set;
        }

        public string GridName
        {
            get; set;
        }

        #region Constructor and Main
        public enum LocalConsole
        {
            Disallowed,
            Allowed
        }


        void HandleRobotsTxt(HttpRequest req)
        {
            using (HttpResponse res = req.BeginResponse("text/plain"))
            {
                using (StreamWriter writer = res.GetOutputStream().UTF8StreamWriter())
                {
                    writer.WriteLine("User-agent: *");
                    writer.WriteLine("Disallow: /");
                }
            }
        }

        readonly string m_PIDFile = string.Empty;

        void CtrlCHandler(object o, ConsoleCancelEventArgs e)
        {
            m_ShutdownEvent.Set();
            e.Cancel = true;
        }

        void UpdaterLogEvent(CoreUpdater.LogType type, string msg)
        {
            switch(type)
            {
                case CoreUpdater.LogType.Info:
                    m_UpdaterLog.Info(msg);
                    break;

                case CoreUpdater.LogType.Warn:
                    m_UpdaterLog.Warn(msg);
                    break;

                case CoreUpdater.LogType.Error:
                    m_UpdaterLog.Error(msg);
                    break;

                default:
                    m_UpdaterLog.Debug(msg);
                    break;
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public ConfigurationLoader(string[] args, ManualResetEvent shutdownEvent, LocalConsole localConsoleControl = LocalConsole.Allowed, bool disableShutdownCommand = false)
        {
            string defaultConfigName;
            string defaultsIniName;
            string mode;

            m_ShutdownTimer.Elapsed += ShutdownTimerEventHandler;
            m_ShutdownEvent = shutdownEvent;
            List<string> defineargs = new List<string>();
            List<string> otherargs = new List<string>();
            foreach(string arg in args)
            {
                if(arg.StartsWith("-D:"))
                {
                    defineargs.Add(arg);
                }
                else
                {
                    otherargs.Add(arg);
                }
            }

            ArgvConfigSource configSource = new ArgvConfigSource(otherargs.ToArray());
            configSource.AddSwitch("Startup", "help", "h");
            configSource.AddSwitch("Startup", "mode", "m");
            configSource.AddSwitch("Startup", "config", "c");
            configSource.AddSwitch("Startup", "dumpconfig");
            configSource.AddSwitch("Startup", "skipregions");
            IConfig startup = configSource.Configs["Startup"];
            mode = startup.GetString("mode", "simulator");
            string dumpResultingIniName = startup.GetString("dumpconfig", string.Empty);
            string newmode = mode;
            IConfig modeConfig;

            if(startup.Contains("help"))
            {
                System.Console.WriteLine("Usage: SilverSim.Main.exe switches...\n");
                System.Console.WriteLine("-config=filename or -c filename\n  Use specific config file\n");
                System.Console.WriteLine("-skipregions\n  Skip start of regions\n");
                System.Console.WriteLine("-D:<section>:<variable>=<value>\n  Set a value from command line\n");
                ShowModeHelp();
                shutdownEvent.Set();
                return;
            }

            if (mode == "?" || mode == "help")
            {
                ShowModeHelp();
                shutdownEvent.Set();
                return;
            }
            {
                List<string> loopCheck = new List<string>();
                do
                {
                    mode = newmode;
                    if(loopCheck.Contains(mode))
                    {
                        throw new ArgumentException("Internal error with mode parameter");
                    }
                    loopCheck.Add(mode);
                    try
                    {
                        IConfigSource modeParamsSource = new CFG_IniResourceSource("ModeConfig." + mode.ToLower() + ".ini").ConfigSource;
                        modeConfig = modeParamsSource.Configs["ModeConfig"];
                    }
                    catch
                    {
                        ShowModeHelp();
                        throw new ArgumentException("Invalid mode parameter");
                    }
                    newmode = modeConfig.GetString("Mode", mode);
                } while (mode != newmode);
            }

            defaultConfigName = modeConfig.GetString("DefaultConfigName", string.Empty);
            defaultsIniName = modeConfig.GetString("DefaultsIniName", string.Empty);
            string defaultLogConfigName = modeConfig.GetString("DefaultLogConfig", "default.log.config");

            string mainConfig = startup.GetString("config", defaultConfigName);

            if (defaultsIniName.Length != 0)
            {
                if (defaultsIniName.EndsWith(".xml"))
                {
                    m_Sources.Enqueue(new CFG_NiniXmlResourceSource(defaultsIniName));
                }
                else
                {
                    m_Sources.Enqueue(new CFG_IniResourceSource(defaultsIniName));
                }
            }

            CoreUpdater.Instance.LoadInstalledPackageDescriptions();

            string DefaultDataPath = Path.Combine(InstallationBinPath, "../data");
            foreach(string defaultCfg in CoreUpdater.Instance.GetDefaultConfigurationFiles(mode))
            {
                if(defaultCfg.Contains(':'))
                {
                    string[] parts = defaultCfg.Split(new char[] { ':' }, 2);
                    if (parts[1].EndsWith(".xml"))
                    {
                        m_Sources.Enqueue(new CFG_NiniXmlResourceSource(parts[1], "Package Defaults " + defaultCfg, parts[0]));
                    }
                    else
                    {
                        m_Sources.Enqueue(new CFG_IniResourceSource(parts[1], "Package Defaults " + defaultCfg, parts[0]));
                    }
                }
                else if(defaultCfg.EndsWith(".xml"))
                {
                    m_Sources.Enqueue(new CFG_NiniXmlFileSource(Path.Combine(DefaultDataPath, defaultCfg)));
                }
                else
                {
                    m_Sources.Enqueue(new CFG_IniFileSource(Path.Combine(DefaultDataPath, defaultCfg)));
                }
            }

            foreach(string preloadAssembly in CoreUpdater.Instance.GetPreloadAssemblies(mode))
            {
                Assembly.LoadFile(Path.Combine(CoreUpdater.Instance.BinariesPath, preloadAssembly));
            }
            
            /* pre-process defaults ini before adding the final configuration */
            ProcessConfigurations(false);

            /* make the resource assets available for all users not just scene */
            PluginInstances.Add("ResourceAssetService", new ResourceAssetPlugin());
            if (!string.IsNullOrEmpty(mainConfig))
            {
                AddSource(mainConfig);
            }
            else
            {
                IConfig cfg = m_Config.AddConfig("HTTP");
                cfg.Set("ListenerPort", "9000");
            }

            if (!disableShutdownCommand)
            {
                CommandRegistry.Commands.Add("shutdown", ShutdownCommand);
            }
            UpdaterControlCommands.RegisterCommands(this);
            CommandRegistry.Commands.Add("execute", ExecuteCommand);
            CommandRegistry.AddShowCommand("memory", ShowMemoryCommand);
            CommandRegistry.AddShowCommand("threadcount", ShowThreadCountCommand);
            CommandRegistry.AddShowCommand("threads", ShowThreadsCommand);
            CommandRegistry.AddShowCommand("queues", ShowQueuesCommand);
            CommandRegistry.AddShowCommand("modules", ShowModulesCommand);
            CommandRegistry.AddGetCommand("serverparam", GetServerParamCommand);
            CommandRegistry.AddSetCommand("serverparam", SetServerParamCommand);
            CommandRegistry.AddShowCommand("serverparams", ShowServerParamsCommand);
            CommandRegistry.AddShowCommand("issues", ShowIssuesCommand);
            CommandRegistry.AddShowCommand("cacheddns", ShowCachedDnsCommand);
            CommandRegistry.AddDeleteCommand("cacheddns", RemoveCachedDnsCommand);
            CommandRegistry.AddShowCommand("ports", ShowPortAllocationsCommand);
#if DEBUG
            CommandRegistry.AddShowCommand("http-handlers", ShowHttpHandlersCommand);
            CommandRegistry.AddShowCommand("xmlrpc-handlers", ShowXmlRpcHandlersCommand);
            CommandRegistry.AddShowCommand("json20rpc-handlers", ShowJson20RpcHandlersCommand);
            CommandRegistry.AddShowCommand("caps-handlers", ShowCapsHandlersCommand);
#endif

            /* inject config values from arguments */
            foreach (string arg in defineargs)
            {
                string vardef = arg.Substring(3);
                int varpos = vardef.IndexOf('=');
                if (varpos < 0)
                {
                    continue;
                }
                string varname = vardef.Substring(0, varpos);
                string varvalue = vardef.Substring(varpos + 1);
                string[] parts = varname.Split(new char[] { ':' }, 2);
                IConfig cfg;
                switch (parts.Length)
                {
                    case 1:
                        cfg = m_Config.Configs["Startup"];
                        if (null == cfg)
                        {
                            cfg = m_Config.AddConfig("Startup");
                        }
                        cfg.Set(parts[0], varvalue);
                        break;

                    case 2:
                        cfg = m_Config.Configs[parts[0]];
                        if (null == cfg)
                        {
                            cfg = m_Config.AddConfig(parts[0]);
                        }
                        cfg.Set(parts[1], varvalue);
                        break;

                    default:
                        break;
                }
            }
            ProcessConfigurations();

            foreach(IConfig cfg in m_Config.Configs)
            {
                foreach (IConfig config in m_Config.Configs)
                {
                    if (!config.Contains("UseSourceParameter"))
                    {
                        continue;
                    }

                    string[] useparam = config.Get("UseSourceParameter").Split(new char[] { '.' }, 2);
                    if (useparam.Length < 2)
                    {
                        continue;
                    }

                    IConfig sourceConfig = m_Config.Configs[useparam[0]];
                    if (null == sourceConfig || !sourceConfig.Contains(useparam[1]))
                    {
                        continue;
                    }

                    string sourceParam = sourceConfig.GetString(useparam[1]);

                    if (string.IsNullOrEmpty(sourceParam))
                    {
                        continue;
                    }
                    throw new ConfigurationErrorException(string.Format("Parameter value {0} for {1} in section {2}",
                        sourceParam, useparam[1], useparam[0]));
                }
            }

            if (dumpResultingIniName.Length != 0)
            {
                using (TextWriter writer = new StreamWriter(dumpResultingIniName))
                {
                    foreach (IConfig cfg in m_Config.Configs)
                    {
                        writer.WriteLine("[{0}]", cfg.Name);
                        foreach (string key in cfg.GetKeys())
                        {
                            writer.WriteLine("{0}={1}", key, cfg.GetString(key));
                        }
                        writer.WriteLine();
                    }
                }
            }

            string logConfigFile = string.Empty;
            IConfig startupConfig = m_Config.Configs["Startup"];
            if(startupConfig != null)
            {
                logConfigFile = startupConfig.GetString("LogConfig", string.Empty);
            }

            try
            {
                if (startupConfig == null || startupConfig.GetBoolean("TreatControlCAsInput", true))
                {
                    System.Console.TreatControlCAsInput = true;
                }
                else
                {
                    System.Console.CancelKeyPress += CtrlCHandler;
                }
            }
            catch
            {
                /* intentionally ignored */
            }


            /* Initialize Log system */
            if (logConfigFile.Length != 0)
            {
                XmlConfigurator.Configure(new System.IO.FileInfo(logConfigFile));
                m_Log = LogManager.GetLogger("MAIN");
            }
            else if(defaultLogConfigName.Length != 0)
            {
                using (Stream s = GetType().Assembly.GetManifestResourceStream("SilverSim.Main.Common.Resources.log4net." + defaultLogConfigName))
                {
                    if(s == null)
                    {
                        throw new ConfigurationErrorException("Could not load log4net defaults named " + defaultLogConfigName);
                    }
                    XmlConfigurator.Configure(s);
                    m_Log = LogManager.GetLogger("MAIN");
                }
            }
            else
            {
                XmlConfigurator.Configure();
                m_Log = LogManager.GetLogger("MAIN");
            }

            m_UpdaterLog = LogManager.GetLogger("UPDATER");
            CoreUpdater.Instance.OnUpdateLog += UpdaterLogEvent;

            IConfig heloConfig = m_Config.Configs["Helo.Headers"];
            if(null != heloConfig)
            {
                foreach (string key in heloConfig.GetKeys())
                {
                    SetHeloResponseHeader(key, heloConfig.GetString(key));
                }
            }

            heloConfig = m_Config.Configs["Helo.X-Protocols-Provided"];
            if(null != heloConfig)
            {
                foreach(string key in heloConfig.GetKeys())
                {
                    AddHeloProtocolsProvided(key, heloConfig.GetInt(key));
                }
            }

            IConfig consoleConfig = m_Config.Configs["Console"];
            string consoleTitle = string.Empty;
            if(null != consoleConfig)
            {
                consoleTitle = consoleConfig.GetString("ConsoleTitle", consoleTitle);
            }

            consoleTitle += ": " + VersionInfo.ProductName + " (" + VersionInfo.Version + ")";
            if ((null == consoleConfig || consoleConfig.GetBoolean("EnableLocalConsole", true)) && localConsoleControl == LocalConsole.Allowed)
            {
                PluginInstances.Add("LocalConsole", new Console.LocalConsole(consoleTitle, Scenes, CommandRegistry));
            }
            else if ((null == consoleConfig || consoleConfig.GetBoolean("EnableLogConsole", false)) && localConsoleControl == LocalConsole.Allowed)
            {
                PluginInstances.Add("LogConsole", new Console.LogConsole(consoleTitle));
            }

            if (startupConfig != null)
            {
                string pidFile = startupConfig.GetString("PIDFile", string.Empty);

                if (pidFile.Length != 0)
                {
                    pidFile = Path.GetFullPath(pidFile);

                    if (File.Exists(pidFile))
                    {
                        m_Log.ErrorFormat(
                            "Old pid file {0} still exists on startup.  May be a previous unclean shutdown.",
                            pidFile);
                    }

                    try
                    {
                        string pidstring = Process.GetCurrentProcess().Id.ToString();

                        using (FileStream fs = File.Create(pidFile))
                        {
                            byte[] buf = pidstring.ToUTF8Bytes();
                            fs.Write(buf, 0, buf.Length);
                        }

                        m_PIDFile = pidFile;

                        m_Log.InfoFormat("Created pid file {0}", pidFile);
                    }
                    catch (Exception e)
                    {
                        m_Log.Warn(string.Format("Could not create PID file \"{0}\"", pidFile), e);
                    }
                }
            }

            m_Log.InfoFormat("Product: {0}", VersionInfo.ProductName);
            m_Log.InfoFormat("Version: {0}", VersionInfo.Version);
            m_Log.InfoFormat("Runtime: {0} {1}", VersionInfo.RuntimeInformation, VersionInfo.MachineWidth);
            m_Log.InfoFormat("OS Version: {0}", Environment.OSVersion.ToString());
            m_Log.InfoFormat("CLR Runtime Version: {0}", Environment.Version);
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    m_Log.InfoFormat("Mono Version String: " + displayName.Invoke(null, null));
                }
            }

#if DEBUG
            string pleaseUseReleaseMsg = "Please use a release build for productive usage";
            KnownConfigurationIssues.Add(pleaseUseReleaseMsg);
            m_Log.Error(pleaseUseReleaseMsg);
#endif

            if(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                KnownConfigurationIssues.Add("Please run as 64-bit process on a 64-bit operating system");
            }

            m_Log.Info("Loading platform modules");
            LoadArchDlls();

            m_Log.Info("Loading specified modules");
            LoadModules();

            m_Log.Info("Verifying Database connectivity");
            Dictionary<string, IDBServiceInterface> dbInterfaces = GetServices<IDBServiceInterface>();
            foreach (KeyValuePair<string, IDBServiceInterface> p in dbInterfaces)
            {
                m_Log.InfoFormat("-> {0}", p.Key);
                try
                {
                    p.Value.VerifyConnection();
                }
                catch
                {
                    m_Log.FatalFormat("Database connection verification for {0} failed", p.Key);
                    throw;
                }
            }

            m_Log.Info("Process Migrations of all database modules");
            foreach (KeyValuePair<string, IDBServiceInterface> p in dbInterfaces)
            {
                m_Log.InfoFormat("-> {0}", p.Key);
                p.Value.ProcessMigrations();
            }

            IConfig httpConfig = m_Config.Configs["HTTP"];
            if(null == httpConfig)
            {
                m_Log.Fatal("Missing configuration section [HTTP]");
                throw new ConfigurationErrorException();
            }

            if(httpConfig.Contains("ServerCertificate"))
            {
                m_Log.Fatal("Configuration section [HTTP] should not be configured for HTTPS");
                throw new ConfigurationErrorException();
            }

            BaseHttpServer httpServer;
            BaseHttpServer httpsServer = null;

            httpServer = new BaseHttpServer(httpConfig, this);
            PluginInstances.Add("HttpServer", httpServer);
            httpServer.StartsWithUriHandlers.Add("/helo", HeloResponseHandler);
            httpServer.UriHandlers.Add("/robots.txt", HandleRobotsTxt);

            IConfig httpsConfig = m_Config.Configs["HTTPS"];
            if(null != httpsConfig)
            {
                httpsServer = new BaseHttpServer(httpsConfig, this, true);
                PluginInstances.Add("HttpsServer", httpsServer);
                httpsServer.UriHandlers.Add("/helo", HeloResponseHandler);
                httpsServer.UriHandlers.Add("/robots.txt", HandleRobotsTxt);
            }
            else
            {
                KnownConfigurationIssues.Add("Configure HTTPS support in [HTTPS] section");
            }

            httpServer.Startup(this);

            if(startupConfig != null)
            {
                HomeURI = startupConfig.GetString("HomeURI", httpServer.ServerURI);
                GatekeeperURI = startupConfig.GetString("GatekeeperURI", HomeURI);
                GridName = startupConfig.GetString("GridName", string.Empty);
                GridNick = startupConfig.GetString("GridNick", string.Empty);
            }

            PluginInstances.Add("XmlRpcServer", new HttpXmlRpcHandler());
            PluginInstances.Add("JSON2.0RpcServer", new HttpJson20RpcHandler());
            PluginInstances.Add("CapsRedirector", new CapsHttpRedirector());

            m_Log.Info("Initing extra modules");
            foreach (IPlugin instance in PluginInstances.Values)
            {
                if(instance.GetType().GetInterfaces().Contains(typeof(IPluginSubFactory)))
                {
                    IPluginSubFactory subfact = (IPluginSubFactory)instance;
                    subfact.AddPlugins(this);
                }
            }

            m_Log.Info("Starting modules");
            foreach(IPlugin instance in PluginInstances.Values)
            {
                if (instance != httpServer)
                {
                    instance.Startup(this);
                }
            }

            IConfig configLoader = Config.Configs["ConfigurationLoader"];
            if(null != configLoader)
            {
                m_RegionStorage = configLoader.Contains("RegionStorage") ? 
                    GetService<GridServiceInterface>(configLoader.GetString("RegionStorage")) : 
                    null;
            }

            if(PluginInstances.ContainsKey("ServerParamStorage"))
            {
                ServerParamServiceInterface serverParams = GetServerParamStorage();
                Dictionary<string, List<KeyValuePair<UUID, string>>> cachedResults = new Dictionary<string, List<KeyValuePair<UUID, string>>>();

                m_Log.Info("Distribute Server Params");
                Dictionary<string, IPlugin> plugins = new Dictionary<string, IPlugin>(PluginInstances);

                m_ServerParamInitialLoadProcessed = true;

                foreach (KeyValuePair<string, IPlugin> kvp in plugins)
                {
                    LoadServerParamsForPlugin(kvp.Key, kvp.Value, cachedResults);
                }

                serverParams.AnyServerParamListeners.Add(this);
                Scenes.OnRegionAdd += LoadParamsOnAddedScene;
            }

            ICollection<IRegionLoaderInterface> regionLoaders = GetServices<IRegionLoaderInterface>().Values;
            if (regionLoaders.Count != 0)
            {
                /* we have to bypass the circular issue we would get when trying to do it via using */
                Assembly assembly = Assembly.Load("SilverSim.Viewer.Core");
                Type t = assembly.GetType("SilverSim.Viewer.Core.SimCircuitEstablishService");
                MethodInfo m = t.GetMethod("HandleSimCircuitRequest");
                m_SimCircuitRequest = (Action<HttpRequest, ConfigurationLoader>)Delegate.CreateDelegate(typeof(Action<HttpRequest, ConfigurationLoader>), m);
                httpServer.StartsWithUriHandlers.Add("/circuit", SimCircuitRequest);
                if(null != httpsServer)
                {
                    httpsServer.StartsWithUriHandlers.Add("/circuit", SimCircuitRequest);
                }

                m_Log.Info("Loading regions");
                if (startup.Contains("skipregions"))
                {
                    m_Log.Warn("Skipping loading of regions");
                }
                else
                {
                    foreach (IRegionLoaderInterface regionLoader in regionLoaders)
                    {
                        regionLoader.LoadRegions();
                    }
                }
                foreach (IRegionLoaderInterface regionLoader in regionLoaders)
                {
                    regionLoader.AllRegionsLoaded();
                }
            }

            ICollection<IPostLoadStep> postLoadSteps = GetServices<IPostLoadStep>().Values;
            if (postLoadSteps.Count != 0)
            {
                m_Log.Info("Running post loading steps");
                foreach (IPostLoadStep postLoadStep in postLoadSteps)
                {
                    postLoadStep.PostLoad();
                }
            }
        }
        #endregion

        #region Sim Establish
        readonly Action<HttpRequest, ConfigurationLoader> m_SimCircuitRequest;
        void SimCircuitRequest(HttpRequest req)
        {
            m_SimCircuitRequest(req, this);
        }
        #endregion

        #region Shutdown Control
        public void TriggerShutdown()
        {
            m_ShutdownEvent.Set();
        }

        void ShutdownCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("shutdown not allowed from restricted console");
            }
            else if(args[0] == "help" || args.Count < 2 || (args[1] == "in" && args.Count < 3))
            {
                io.Write("shutdown now\nshutdown in <seconds>\nshutdown abort");
            }
            else if(args[1] == "now")
            {
                m_ShutdownEvent.Set();
            }
            else if (args[1] == "abort")
            {
                AbortSimulatorShutdown();
            }
            else if(args[1] == "in")
            {
                int secondsToShutdown;
                if(int.TryParse(args[2], out secondsToShutdown))
                {
                    RequestSimulatorShutdown(secondsToShutdown);
                }
                else
                {
                    io.WriteFormatted("{0} is not a valid number.", secondsToShutdown);
                }
            }
            else
            {
                io.WriteFormatted("Unknown token {0} for shutdown", args[1]);
            }
        }

        public void Shutdown()
        {
            Scenes.OnRegionAdd -= LoadParamsOnAddedScene;

            List<IPluginShutdown> shutdownLogoutBeforeAgentsList = new List<IPluginShutdown>();
            List<IPluginShutdown> shutdownLogoutAgentsList = new List<IPluginShutdown>();
            List<IPluginShutdown> shutdownLogoutRegionsList = new List<IPluginShutdown>();
            List<IPluginShutdown> shutdownLogoutDatabaseList = new List<IPluginShutdown>();
            List<IPluginShutdown> shutdownAnyList = new List<IPluginShutdown>();

            foreach(IPluginShutdown s in GetServices<IPluginShutdown>().Values)
            {
                switch(s.ShutdownOrder)
                {
                    case ShutdownOrder.Any:
                        shutdownAnyList.Add(s);
                        break;
                    case ShutdownOrder.BeforeLogoutAgents:
                        shutdownLogoutBeforeAgentsList.Add(s);
                        break;
                    case ShutdownOrder.LogoutAgents:
                        shutdownLogoutAgentsList.Add(s);
                        break;
                    case ShutdownOrder.LogoutRegion:
                        shutdownLogoutRegionsList.Add(s);
                        break;
                    case ShutdownOrder.LogoutDatabase:
                        shutdownLogoutDatabaseList.Add(s);
                        break;
                    default:
                        break;
                }
            }

            foreach (IPluginShutdown s in shutdownLogoutBeforeAgentsList)
            {
                s.Shutdown();
            }

            foreach (IPluginShutdown s in shutdownLogoutAgentsList)
            {
                s.Shutdown();
            }

            Scenes.RemoveAll();

            foreach (IPluginShutdown s in shutdownLogoutRegionsList)
            {
                s.Shutdown();
            }

            foreach (IPluginShutdown s in shutdownAnyList)
            {
                s.Shutdown();
            }

            foreach(IPluginShutdown s in shutdownLogoutDatabaseList)
            {
                s.Shutdown();
            }
        }
        #endregion

        #region Common Commands
        public void ExecuteCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if(args[0] == "help" || args.Count < 2)
            {
                io.Write("execute <commandlist file>");
            }
            else
            {
                using (StreamReader reader = new StreamReader(args[1]))
                {
                    string line;
                    while (null != (line = reader.ReadLine()))
                    {
                        CommandRegistry.ExecuteCommand(io.GetCmdLine(line), io, limitedToScene);
                    }
                }
            }
        }

        #endregion
    }
}
