// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using log4net.Config;
using Nini.Config;
using SilverSim.Http.Client;
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
using SilverSim.ServiceInterfaces;
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
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Assembly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;

namespace SilverSim.Main.Common
{
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    public sealed class ConfigurationLoader : IServerParamAnyListener
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
            m_ShutdownEvent.Dispose();
        }

        readonly ILog m_Log;
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
        static Action SimulatorShutdownAbortDelegate; /* used for Scene.Management registration */

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
            try
            {
                m_MonoSecurity = Assembly.Load("Mono.Security");
            }
            catch
            {
                m_MonoSecurity = Assembly.LoadFile(Path.GetFullPath("platform-libs/Mono.Security.dll"));
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
            FeaturesTable[typeof(IAssetServicePlugin)] = "Asset Service HELO Instantiator";
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
        bool m_ServerParamInitialLoadProcessed;

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
        #endregion

        #region Configuration Loader Helpers
        private interface ICFG_Source
        {
            IConfigSource ConfigSource { get; }
            string Name { get; }
            string Message { get; }
            string DirName { get; }
        }

        sealed class CFG_IniFileSource : ICFG_Source
        {
            readonly string m_FileName;
            public CFG_IniFileSource(string fileName)
            {
                m_FileName = fileName;
            }

            public string Message
            {
                get
                {
                    return "Could not load ini file {0}";
                }
            }

            public string DirName
            {
                get
                {
                    return Path.GetDirectoryName(Name);
                }
            }

            public string Name
            {
                get
                {
                    return Path.GetFullPath(m_FileName);
                }
            }

            public IConfigSource ConfigSource
            {
                get
                {
                    return new IniConfigSource(m_FileName);
                }
            }
        }

        sealed class CFG_NiniXmlFileSource : ICFG_Source
        {
            readonly string m_Filename;
            public CFG_NiniXmlFileSource(string fileName)
            {
                m_Filename = fileName;
            }

            public string Message
            {
                get
                {
                    return "Could not load ini file {0}";
                }
            }

            public string DirName
            {
                get
                {
                    return Path.GetDirectoryName(Name);
                }
            }

            public string Name
            {
                get
                {
                    return Path.GetFullPath(m_Filename);
                }
            }

            public IConfigSource ConfigSource
            {
                get
                {
                    return new XmlConfigSource(m_Filename);
                }
            }
        }

        sealed class CFG_NiniXmlUriSource : ICFG_Source
        {
            readonly string m_Uri;
            public CFG_NiniXmlUriSource(string uri)
            {
                m_Uri = uri;
            }

            public string Message
            {
                get
                {
                    return "Could not load xml file {0}";
                }
            }

            public string DirName
            {
                get
                {
                    return ".";
                }
            }

            public string Name
            {
                get
                {
                    return m_Uri;
                }
            }

            public IConfigSource ConfigSource
            {
                get
                {
                    using (XmlReader r = new XmlTextReader(HttpRequestHandler.DoStreamGetRequest(m_Uri, null, 20000)))
                    {
                        return new XmlConfigSource(r);
                    }
                }
            }
        }

        sealed class CFG_IniResourceSource : ICFG_Source
        {
            readonly string m_Name;
            readonly string m_Info;
            readonly string m_Assembly = string.Empty;

            public CFG_IniResourceSource(string name)
            {
                m_Name = name;
                m_Info = "Resource {0} not found";
            }
            
            public CFG_IniResourceSource(string name, string info)
            {
                m_Name = name;
                m_Info = info;
            }

            public CFG_IniResourceSource(string name, string info, string assembly)
            {
                m_Name = name;
                m_Info = info;
                m_Assembly = assembly;
            }

            public string DirName
            {
                get
                {
                    return ".";
                }
            }

            public string Message
            {
                get
                {
                    return m_Info;
                }
            }

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidCallingProblematicMethodsRule")]
            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public IConfigSource ConfigSource
            {
                get
                {
                    Assembly assembly;
                    if(m_Assembly.Length != 0)
                    {
                        try
                        {
                            assembly = Assembly.LoadFrom("plugins/" + m_Assembly + ".dll");
                        }
                        catch
                        {
                            throw new FileNotFoundException("plugins/" + m_Assembly + ".dll");
                        }
                    }
                    else
                    {
                        assembly = GetType().Assembly;
                    }

                    string assemblyName = assembly.GetName().Name;
                    Stream resource = assembly.GetManifestResourceStream(assemblyName + ".Resources." + m_Name);
                    if(null == resource)
                    {
                        throw new FileNotFoundException(assemblyName + ".Resources." + m_Name);
                    }
                    return new IniConfigSource(resource);
                }
            }
        }

        sealed class CFG_NiniXmlResourceSource : ICFG_Source
        {
            readonly string m_Name;
            readonly string m_Info;
            readonly string m_Assembly = string.Empty;

            public CFG_NiniXmlResourceSource(string name)
            {
                m_Name = name;
                m_Info = "Resource {0} not found";
            }

            public CFG_NiniXmlResourceSource(string name, string info)
            {
                m_Name = name;
                m_Info = info;
            }

            public CFG_NiniXmlResourceSource(string name, string info, string assembly)
            {
                m_Name = name;
                m_Info = info;
                m_Assembly = assembly;
            }

            public string DirName
            {
                get
                {
                    return ".";
                }
            }

            public string Message
            {
                get
                {
                    return m_Info;
                }
            }

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidCallingProblematicMethodsRule")]
            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public IConfigSource ConfigSource
            {
                get
                {
                    Assembly assembly;
                    if (m_Assembly.Length != 0)
                    {
                        try
                        {
                            assembly = Assembly.LoadFrom("plugins/" + m_Assembly + ".dll");
                        }
                        catch
                        {
                            throw new FileNotFoundException("plugins/" + m_Assembly + ".dll");
                        }
                    }
                    else
                    {
                        assembly = GetType().Assembly;
                    }

                    string assemblyName = assembly.GetName().Name;
                    Stream resource = assembly.GetManifestResourceStream(assemblyName + ".Resources." + m_Name);
                    if (null == resource)
                    {
                        throw new FileNotFoundException(assemblyName + ".Resources." + m_Name);
                    }
                    using (XmlReader r = new XmlTextReader(resource))
                    {
                        return new XmlConfigSource(r);
                    }
                }
            }
        }
        #endregion

        #region Config Source Management
        private void AddSource(ICFG_Source cfgsource, string file)
        {
            Uri configUri;
            if(Uri.TryCreate(file, UriKind.Absolute,
                    out configUri))
            {
                m_Sources.Enqueue(new CFG_NiniXmlUriSource(file));
            }
            else
            {
                m_Sources.Enqueue(new CFG_IniFileSource(Path.Combine(cfgsource.DirName, file)));
            }
        }

        private void AddSource(string file)
        {
            Uri configUri;
            if(Uri.TryCreate(file, UriKind.Absolute,
                    out configUri))
            {
                m_Sources.Enqueue(new CFG_NiniXmlUriSource(file));
            }
            else if(file.EndsWith(".xml"))
            {
                m_Sources.Enqueue(new CFG_NiniXmlFileSource(file));
            }
            else
            {
                m_Sources.Enqueue(new CFG_IniFileSource(file));
            }
        }

        private void AddIncludes(ICFG_Source cfgsource)
        {
            foreach(IConfig config in m_Config.Configs)
            {
                foreach(string key in config.GetKeys())
                {
                    if(key.StartsWith("Include"))
                    {
                        AddSource(cfgsource, config.GetString(key));
                        config.Remove(key);
                    }
                }
            }
        }

        private void AddResourceConfig(string resourcereference, string info)
        {
            string[] nameparts = resourcereference.Split(new char[] { ':' }, 2, StringSplitOptions.None);
            if (nameparts.Length == 1)
            {
                if (nameparts[0].EndsWith(".xml"))
                {
                    m_Sources.Enqueue(new CFG_NiniXmlResourceSource(nameparts[0], info));
                }
                else
                {
                    m_Sources.Enqueue(new CFG_IniResourceSource(nameparts[0], info));
                }
            }
            else if(nameparts[0].EndsWith(".xml"))
            {
                m_Sources.Enqueue(new CFG_NiniXmlResourceSource(nameparts[1], info, nameparts[0]));
            }
            else
            {
                m_Sources.Enqueue(new CFG_IniResourceSource(nameparts[1], info, nameparts[0]));
            }
        }
        #endregion

        #region Module Loading
        private Dictionary<string, T> GetServices<T>()
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            PluginInstances.ForEach(delegate(KeyValuePair<string, IPlugin> p)
            {
                if (typeof(T).IsAssignableFrom(p.Value.GetType()))
                {
                    result.Add(p.Key, (T)p.Value);
                }
            });
            return result;
        }

        static InterfaceVersionAttribute GetInterfaceVersion(Assembly assembly)
        {
            InterfaceVersionAttribute attr = Attribute.GetCustomAttribute(assembly, typeof(InterfaceVersionAttribute)) as InterfaceVersionAttribute;
            if(null != attr)
            {
                return attr;
            }
            throw new ConfigurationErrorException(string.Format("Assembly {0} misses InterfaceVersion information", assembly.FullName));
        }
 
        private Type FindPluginInAssembly(Assembly assembly, string pluginName)
        {
            foreach(Type t in assembly.GetTypes())
            {
                foreach(object o in t.GetCustomAttributes(typeof(PluginNameAttribute), false))
                {
                    if(((PluginNameAttribute)o).Name == pluginName)
                    {
                        return t;
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        static Assembly ArchSpecificResolveEventHandler(object sender, ResolveEventArgs args)
        {
            AssemblyName aName = new AssemblyName(args.Name);
            string assemblyFileName;
            Assembly assembly;
            if(PreloadPlatformAssemblies.TryGetValue(aName.Name, out assembly))
            {
                return assembly;
            }

            switch(Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    assemblyFileName = Environment.Is64BitProcess ? 
                        "platform-libs/windows/64/" + aName.Name + ".dll" :
                        "platform-libs/windows/32/" + aName.Name + ".dll";
                    break;

                case PlatformID.MacOSX:
                    assemblyFileName = Environment.Is64BitProcess ?
                        "platform-libs/macosx/64/" + aName.Name + ".dll" :
                        "platform-libs/macosx/32/" + aName.Name + ".dll";
                    break;

                case PlatformID.Unix:
                    assemblyFileName = Environment.Is64BitProcess ? 
                        "platform-libs/linux/64/" + aName.Name + ".dll" :
                        "platform-libs/linux/32/" + aName.Name + ".dll";
                    break;

                default:
                    return null;
            }

            if(!File.Exists(assemblyFileName))
            {
                return null;
            }
            assembly = Assembly.LoadFrom(assemblyFileName);
            PreloadPlatformAssemblies[aName.Name] = assembly;
            return assembly;
        }

        readonly InterfaceVersionAttribute m_OwnInterfaceVersion = GetInterfaceVersion(Assembly.GetExecutingAssembly());

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidCallingProblematicMethodsRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void LoadModule(IConfig config, string modulename)
        {
            string[] modulenameparts = modulename.Split(new char[] { ':' }, 2, StringSplitOptions.None);
            if (modulenameparts.Length < 2)
            {
                m_Log.FatalFormat("Invalid Module in section {0}: {1}", config.Name, modulename);
                throw new ConfigurationErrorException();
            }
            string assemblyname = "plugins/" + modulenameparts[0] + ".dll";
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyname);
            }
            catch
            {
                m_Log.FatalFormat("Failed to load module {0}", assemblyname);
                throw new ConfigurationErrorException();
            }

            InterfaceVersionAttribute loadedVersion = GetInterfaceVersion(assembly);
            if (loadedVersion.Version != m_OwnInterfaceVersion.Version)
            {
                m_Log.FatalFormat("Failed to load module {0}: interface version mismatch: {2} != {1}", assemblyname, m_OwnInterfaceVersion, loadedVersion);
                throw new ConfigurationErrorException();
            }

            /* try to load class from assembly */
            Type t;
            try
            {
                t = FindPluginInAssembly(assembly, modulenameparts[1]);
            }
            catch (Exception e)
            {
                m_Log.FatalFormat("Failed to load factory for {1} in module {0}: {2}", assemblyname, modulenameparts[1], e.Message);
                throw new ConfigurationErrorException();
            }

            if (t == null)
            {
                m_Log.FatalFormat("Failed to load factory for {1} in module {0}: factory not found", assemblyname, modulenameparts[1]);
                throw new ConfigurationErrorException();
            }

            /* check type inheritance first */
            if (!t.GetInterfaces().Contains(typeof(IPluginFactory)))
            {
                m_Log.FatalFormat("Failed to load factory for {1} in module {0}: not a factory", assemblyname, modulenameparts[1]);
                throw new ConfigurationErrorException();
            }

            IPluginFactory module = (IPluginFactory)assembly.CreateInstance(t.FullName);
            PluginInstances.Add(config.Name, module.Initialize(this, config));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidCallingProblematicMethodsRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void LoadModules()
        {
            string archModule = "Module-" + VersionInfo.ArchSpecificId;
            foreach (IConfig config in m_Config.Configs)
            {
                if (config.Contains("IsTemplate"))
                {
                    continue;
                }
                foreach (string key in config.GetKeys())
                {
                    if(key.Equals(archModule) || key.Equals("Module"))
                    {
                        LoadModule(config, config.GetString(key));
                    }
                }
            }
        }
        #endregion

        #region Preload Arch Specific Libraries
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string dllToLoad);

        void LoadArchDlls()
        {
            string archModule = "UnmanagedModule-" + VersionInfo.ArchSpecificId;
            foreach (IConfig config in m_Config.Configs)
            {
                if (config.Contains("IsTemplate"))
                {
                    continue;
                }
                foreach (string key in config.GetKeys())
                {
                    if (key.StartsWith(archModule))
                    {
                        string fName = config.GetString(key);
                        if(Environment.OSVersion.Platform == PlatformID.Win32NT &&
                            LoadLibrary(Path.GetFullPath(fName)) == IntPtr.Zero)
                        {
                            throw new ConfigurationLoader.ConfigurationErrorException("unmanaged module " + fName + " not found");
                        }
                    }
                }
            }
        }
        #endregion

        #region Process [ParameterMap] section
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        private void ProcessParameterMap()
        {
            IConfig parameterMap = m_Config.Configs["ParameterMap"];
            if(null == parameterMap)
            {
                return;
            }

            foreach (string key in parameterMap.GetKeys())
            {
                string[] toparts = key.Split(new char[] { '.' }, 2, StringSplitOptions.None);
                string[] fromparts = parameterMap.GetString(key).Split(new char[] { '.' }, 2, StringSplitOptions.None);
                if (fromparts.Length < 2 || toparts.Length < 2)
                {
                    continue;
                }

                IConfig fromconfig = m_Config.Configs[fromparts[0]];
                if (fromconfig == null)
                {
                    continue;
                }

                if(!fromconfig.Contains(fromparts[1]))
                {
                    continue;
                }
                
                IConfig toconfig = m_Config.Configs[toparts[0]];
                if(toconfig == null)
                {
                    toconfig = m_Config.AddConfig(toparts[0]);
                }

                if(toconfig.Contains(toparts[1]))
                {
                    /* do not overwrite existing keys */
                    continue;
                }

                toconfig.Set(toparts[1], fromconfig.Get(fromparts[1]));
                parameterMap.Remove(key);
            }
        }
        #endregion

        #region Process ImportResource* entries
        private void ProcessImportResources()
        {
            foreach (IConfig config in m_Config.Configs)
            {
                foreach (string key in config.GetKeys())
                {
                    if (key.StartsWith("ImportResource"))
                    {
                        AddResourceConfig(config.GetString(key),
                            String.Format("Import of resource {0} failed", config.GetString(key)));
                        config.Remove(key);
                    }
                }
            }
        }
        #endregion

        #region Process [ResourceMap] section
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        private void ProcessResourceMap()
        {
            IConfig resourceMap = m_Config.Configs["ResourceMap"];
            if (resourceMap == null)
            {
                return;
            }

            foreach (string key in resourceMap.GetKeys())
            {
                string[] parts = key.Split(new char[] { '.' }, 2, StringSplitOptions.None);
                if (parts.Length < 2)
                {
                    continue;
                }
                IConfig config = m_Config.Configs[parts[0]];
                if (config == null)
                {
                    continue;
                }
                if (config.Contains(parts[1]) &&
                    config.Get(parts[1]).Length != 0)
                {
                    string configname = resourceMap.GetString(key) + "." + config.Get(parts[1]) + ".ini";
                    AddResourceConfig(configname,
                        String.Format("Parameter {1} = {2} in section {0} is invalid", parts[0], parts[1], config.Get(parts[1])));
                    config.Remove(parts[1]);
                }
            }
        }
        #endregion

        #region Process UseTemplates lines
        private void ProcessUseTemplates()
        {
            foreach (IConfig config in m_Config.Configs)
            {
                string[] sections = config.GetString("Use", string.Empty).Split(new char[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(string section in sections)
                {
                    if(section == config.Name)
                    {
                        System.Console.Write("Self referencing Use");
                        System.Console.WriteLine();
                        throw new ConfigurationErrorException();
                    }
                    IConfig configSection = m_Config.Configs[section];
                    if(!configSection.Contains("IsTemplate"))
                    {
                        System.Console.Write("Use does not reference a valid template");
                        System.Console.WriteLine();
                        throw new ConfigurationErrorException();
                    }
                    foreach (string fromkey in configSection.GetKeys())
                    {
                        if (!config.Contains(fromkey) && fromkey != "IsTemplate")
                        {
                            config.Set(fromkey, configSection.Get(fromkey));
                        }
                    }
                }
            }
        }
        #endregion

        #region Load from grids xml
        IConfigSource m_GridsXmlConfig;
        public void LoadGridsXml()
        {
            if (null == m_GridsXmlConfig)
            {
                IConfig gridMap = m_Config.Configs["Grid"];
                if (gridMap == null)
                {
                    return;
                }

                if (gridMap.Contains("Id"))
                {
                    m_GridsXmlConfig = new IniConfigSource();
                    string gridid = gridMap.GetString("Id");
                    if (!SimGridInfo.LoadFromGridsXml(m_GridsXmlConfig, gridid))
                    {
                        throw new ConfigurationErrorException(string.Format("Unknown grid id {0}", gridid));
                    }
                    m_Config.Merge(m_GridsXmlConfig);
                }
            }
        }
        #endregion

        #region Constructor and Main
        public enum LocalConsole
        {
            Disallowed,
            Allowed
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public ConfigurationLoader(string[] args, ManualResetEvent shutdownEvent, LocalConsole localConsoleControl = LocalConsole.Allowed)
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
            configSource.AddSwitch("Startup", "mode", "m");
            configSource.AddSwitch("Startup", "config", "c");
            configSource.AddSwitch("Startup", "skip-regions");
            IConfig startup = configSource.Configs["Startup"];
            mode = startup.GetString("mode", "simulator");
            switch(mode)
            {
                case "simulator":
                    defaultConfigName = "../data/SilverSim.ini";
                    defaultsIniName = "Simulator.defaults.ini";
                    break;

                case "grid":
                    defaultConfigName = "../data/SilverSim.Grid.ini";
                    defaultsIniName = "Grid.defaults.ini";
                    break;

                case "testing":
                    defaultConfigName = string.Empty;
                    defaultsIniName = "Testing.defaults.ini";
                    break;

                default:
                    throw new ArgumentException("Invalid mode parameter");
            }
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
            /* make the resource assets available for all users not just scene */
            PluginInstances.Add("ResourceAssetService", new ResourceAssetPlugin());
            AddSource(mainConfig);

            CommandRegistry.Commands.Add("shutdown", ShutdownCommand);
            CommandRegistry.Commands.Add("execute", ExecuteCommand);
            CommandRegistry.ShowCommands.Add("memory", ShowMemoryCommand);
            CommandRegistry.ShowCommands.Add("threadcount", ShowThreadCountCommand);
            CommandRegistry.ShowCommands.Add("queues", ShowQueuesCommand);
            CommandRegistry.ShowCommands.Add("modules", ShowModulesCommand);
            CommandRegistry.GetCommands.Add("serverparam", GetServerParamCommand);
            CommandRegistry.SetCommands.Add("serverparam", SetServerParamCommand);
            CommandRegistry.ShowCommands.Add("serverparams", ShowServerParamsCommand);
            CommandRegistry.ShowCommands.Add("issues", ShowIssuesCommand);
            CommandRegistry.ShowCommands.Add("cacheddns", ShowCachedDnsCommand);
            CommandRegistry.DeleteCommands.Add("cacheddns", RemoveCachedDnsCommand);

            while(m_Sources.Count != 0)
            {
                ICFG_Source source = m_Sources.Dequeue();
                try
                {
                    m_Config.Merge(source.ConfigSource);
                }
                catch
                {
                    System.Console.Write(String.Format(source.Message, source.Name));
                    System.Console.WriteLine();
                    throw new ConfigurationErrorException();
                }
                LoadGridsXml();
                AddIncludes(source);
                ProcessImportResources();
                ProcessParameterMap();
                ProcessResourceMap();
            }
            LoadGridsXml();
            ProcessParameterMap();
            ProcessUseTemplates();

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

            string logConfigFile = string.Empty;
            IConfig startupConfig = m_Config.Configs["Startup"];
            if(startupConfig != null)
            {
                logConfigFile = startupConfig.GetString("LogConfig", string.Empty);
            }

            /* Initialize Log system */
            if (logConfigFile.Length != 0)
            {
                XmlConfigurator.Configure(new System.IO.FileInfo(logConfigFile));
                m_Log = LogManager.GetLogger("MAIN");
                m_Log.InfoFormat("configured log4net using \"{0}\" as configuration file",
                                 logConfigFile);
            }
            else
            {
                XmlConfigurator.Configure();
                m_Log = LogManager.GetLogger("MAIN");
                m_Log.Info("configured log4net using defaults");
            }

            IConfig heloConfig = m_Config.Configs["Helo.Headers"];
            if(null != heloConfig)
            {
                foreach (string key in heloConfig.GetKeys())
                {
                    SetHeloResponseHeader(key, heloConfig.GetString(key));
                }
            }

            IConfig consoleConfig = m_Config.Configs["Console"];
            string consoleTitle = string.Empty;
            if(null != consoleConfig)
            {
                consoleTitle = consoleConfig.GetString("ConsoleTitle", consoleTitle);
            }

            consoleTitle += ": " + VersionInfo.ProductName + " (" + VersionInfo.Version + ")";
            if (null == consoleConfig || consoleConfig.GetBoolean("EnableLocalConsole", true) && localConsoleControl == LocalConsole.Allowed)
            {
                PluginInstances.Add("LocalConsole", new Console.LocalConsole(consoleTitle, Scenes, CommandRegistry));
            }
            else if (null == consoleConfig || consoleConfig.GetBoolean("EnableLogConsole", false) && localConsoleControl == LocalConsole.Allowed)
            {
                PluginInstances.Add("LogConsole", new Console.LogConsole(consoleTitle));
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

            httpServer = new BaseHttpServer(httpConfig);
            PluginInstances.Add("HttpServer", httpServer);
            httpServer.UriHandlers.Add("/helo", HeloResponseHandler);

            IConfig httpsConfig = m_Config.Configs["HTTPS"];
            if(null != httpsConfig)
            {
                BaseHttpServer httpsServer = new BaseHttpServer(httpsConfig, true);
                httpsServer.UriHandlers.Add("/helo", HeloResponseHandler);
                PluginInstances.Add("HttpsServer", httpsServer);
            }
            else
            {
                KnownConfigurationIssues.Add("Configure HTTPS support in [HTTPS] section");
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
                instance.Startup(this);
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

                m_Log.Info("Loading regions");
                if (startup.Contains("skip-regions"))
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

        #region Load Server Params
        void LoadServerParamsForPlugin(string name, IPlugin instance, Dictionary<string, List<KeyValuePair<UUID, string>>> cachedResults)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            Type instanceType = instance.GetType();
            ServerParamStartsWithAttribute[] startswithattrs = Attribute.GetCustomAttributes(instanceType, typeof(ServerParamStartsWithAttribute)) as ServerParamStartsWithAttribute[];
            if(instanceType.GetInterfaces().Contains(typeof(IServerParamAnyListener)) && startswithattrs.Length != 0)
            {
                IServerParamAnyListener listener = (IServerParamAnyListener)instance;
#if DEBUG
                m_Log.DebugFormat("Processing {0} for start with server params", name);
#endif
                foreach(ServerParamStartsWithAttribute attr in startswithattrs)
                {
                    foreach(KeyValuePair<UUID, string> kvp in serverParams.KnownParameters)
                    {
                        if(kvp.Value.StartsWith(attr.ParameterNameStartsWith))
                        {
#if DEBUG
                            m_Log.DebugFormat("sending config value to {0} with parameter {1}/{2}", name, kvp.Key.ToString(), kvp.Value);
#endif
                            try
                            {
                                listener.TriggerParameterUpdated(kvp.Key, kvp.Value, serverParams.GetString(kvp.Key, kvp.Value));
                            }
                            catch (Exception e)
                            {
                                m_Log.WarnFormat("Failed to configure {0} with parameter {1}/{2}: {3}: {4}\n{5}", name, kvp.Key.ToString(), kvp.Value, e.GetType().FullName, e.Message, e.StackTrace);
                            }
                        }
                    }

#if DEBUG
                    m_Log.DebugFormat("adding update listener for {0} with start with parameter {1}", name, attr.ParameterNameStartsWith);
#endif
                    serverParams.StartsWithServerParamListeners[attr.ParameterNameStartsWith].Add((IServerParamAnyListener)instance);
                }

            }

            if (instanceType.GetInterfaces().Contains(typeof(IServerParamListener)))
            {
#if DEBUG
                m_Log.DebugFormat("Processing {0} for specific server param", name);
#endif
                ServerParamAttribute[] attrs = Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute)) as ServerParamAttribute[];
                foreach (ServerParamAttribute attr in attrs)
                {
                    string parameterName = attr.ParameterName;
                    List<KeyValuePair<UUID, string>> result;
                    if (!cachedResults.TryGetValue(parameterName, out result))
                    {
                        result = serverParams[parameterName];
                        cachedResults.Add(parameterName, result);
                    }

                    bool foundSpecific = false;
                    MethodInfo[] mis = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (MethodInfo mi in mis)
                    {
                        ServerParamAttribute[] mi_attrs = Attribute.GetCustomAttributes(mi, typeof(ServerParamAttribute)) as ServerParamAttribute[];
                        foreach (ServerParamAttribute mi_attr in mi_attrs)
                        {
                            if (mi_attr.ParameterName == parameterName)
                            {
                                Action<UUID, string> del;
                                try
                                {
                                    del = Delegate.CreateDelegate(typeof(Action<UUID, string>), instance, mi) as Action<UUID, string>;
                                }
                                catch(Exception e)
                                {
                                    m_Log.WarnFormat("Failed to initialize {0} for parameter {1}: {2}: {3}\n{4}", name, parameterName, e.GetType().FullName, e.Message, e.StackTrace);
                                    /* could not create so skip it */
                                    continue;
                                }
                                foundSpecific = true;
                                foreach (KeyValuePair<UUID, string> kvp in result)
                                {
#if DEBUG
                                    m_Log.DebugFormat("sending config value to {0} with parameter {1}/{2}", name, kvp.Key.ToString(), parameterName);
#endif
                                    try
                                    {
                                        del(kvp.Key, kvp.Value);
                                    }
                                    catch (Exception e)
                                    {
                                        m_Log.WarnFormat("Failed to configure {0} with parameter {1}/{2}: {3}: {4}\n{5}", name, kvp.Key.ToString(), parameterName, e.GetType().FullName, e.Message, e.StackTrace);
                                    }
                                }
#if DEBUG
                                m_Log.DebugFormat("adding update listener for {0} with parameter {1}", name, parameterName);
#endif
                                serverParams.SpecificParamListeners[parameterName].Add(del);
                            }
                        }
                    }
                    
                    if(foundSpecific)
                    {
                        continue;
                    }

                    if (instanceType.GetInterfaces().Contains(typeof(IServerParamAnyListener)))
                    {
                        IServerParamAnyListener listener = (IServerParamAnyListener)instance;
                        foreach (KeyValuePair<UUID, string> kvp in result)
                        {
#if DEBUG
                            m_Log.DebugFormat("sending config value to {0} with parameter {1}/{2}", name, kvp.Key.ToString(), parameterName);
#endif
                            try
                            {
                                listener.TriggerParameterUpdated(kvp.Key, parameterName, kvp.Value);
                            }
                            catch (Exception e)
                            {
                                m_Log.WarnFormat("Failed to configure {0} with parameter {1}/{2}: {3}: {4}\n{5}", name, kvp.Key.ToString(), parameterName, e.GetType().FullName, e.Message, e.StackTrace);
                            }
                        }
#if DEBUG
                        m_Log.DebugFormat("adding update listener for {0} with parameter {1}", name, parameterName);
#endif
                        serverParams.GenericServerParamListeners[parameterName].Add(listener);
                    }
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

        #region Load region scene params
        void LoadParamsOnAddedScene(SceneInterface scene)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            Dictionary<string, List<KeyValuePair<UUID, string>>> cachedResults = new Dictionary<string, List<KeyValuePair<UUID, string>>>();
            Type instanceType = scene.GetType();

            ServerParamAttribute[] attrs = Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute)) as ServerParamAttribute[];
            foreach (ServerParamAttribute attr in attrs)
            {
                string parameterName = attr.ParameterName;
                List<KeyValuePair<UUID, string>> result;
                if (!cachedResults.TryGetValue(parameterName, out result))
                {
                    result = serverParams[parameterName];
                    cachedResults.Add(parameterName, result);
                }

                bool foundSpecific = false;
                MethodInfo[] mis = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (MethodInfo mi in mis)
                {
                    ServerParamAttribute[] mi_attrs = Attribute.GetCustomAttributes(mi, typeof(ServerParamAttribute)) as ServerParamAttribute[];
                    foreach (ServerParamAttribute mi_attr in mi_attrs)
                    {
                        if (mi_attr.ParameterName == parameterName)
                        {
                            Action<UUID, string> del;
                            try
                            {
                                del = Delegate.CreateDelegate(typeof(Action<UUID, string>), scene, mi) as Action<UUID, string>;
                            }
                            catch(Exception e)
                            {
                                /* could not create so skip it */
                                m_Log.WarnFormat("Failed to initialize scene {0} for parameter {1}: {2}: {3}\n{4}", scene.Name, parameterName, e.GetType().FullName, e.Message, e.StackTrace);
                                continue;
                            }
                            foundSpecific = true;
                            foreach (KeyValuePair<UUID, string> kvp in result)
                            {
#if DEBUG
                                m_Log.DebugFormat("sending update to scene {0} with parameter {1}/{2}", scene.Name, kvp.Key.ToString(), parameterName);
#endif
                                try
                                {
                                    del(kvp.Key, kvp.Value);
                                }
                                catch (Exception e)
                                {
                                    m_Log.WarnFormat("Failed to configure scene {0} with parameter {1}/{2}: {3}: {4}\n{5}", scene.Name, kvp.Key.ToString(), parameterName, e.GetType().FullName, e.Message, e.StackTrace);
                                }
                            }
                        }
                    }
                }

                if (foundSpecific)
                {
                    continue;
                }

                if (instanceType.GetInterfaces().Contains(typeof(IServerParamAnyListener)))
                {
                    IServerParamAnyListener listener = (IServerParamAnyListener)scene;
                    foreach (KeyValuePair<UUID, string> kvp in result)
                    {
#if DEBUG
                        m_Log.DebugFormat("sending update to {0} with parameter {1}/{2}", scene.Name, kvp.Key.ToString(), parameterName);
#endif
                        try
                        {
                            listener.TriggerParameterUpdated(kvp.Key, parameterName, kvp.Value);
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Failed to configure scene {0} with parameter {1}/{2}: {3}: {4}\n{5}", scene.Name, kvp.Key.ToString(), parameterName, e.GetType().FullName, e.Message, e.StackTrace);
                        }
                    }
                }
            }
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

        public void ShowIssuesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (UUID.Zero != limitedToScene)
            {
                io.Write("show issues not allowed on limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("show issues");
            }
            else
            {
                io.Write("Known Configuration Issues:\n" + string.Join("\n", KnownConfigurationIssues));
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        static void ShowMemoryCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Shows current memory usage by simulator");
            }
            else
            {
                io.WriteFormatted("Heap allocated to simulator : {0} MB\n" +
                                    "Process Memory              : {0} MB", Math.Round(GC.GetTotalMemory(false) / 1048576.0), Math.Round(Process.GetCurrentProcess().WorkingSet64 / 1048576.0));
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void GetServerParamCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help" || args.Count < 3 || args.Count > 4)
            {
                io.Write("get serverparam <regionid> <param>\nget serverparam <param>");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("get serverparam is not possible with limited console");
            }
            else if (args.Count == 3)
            {
                try
                {
                    io.Write("=" + GetServerParamStorage()[UUID.Zero, args[2]]);
                }
                catch (Exception e)
                {
                    io.Write("Server parameter not available");
                }
            }
            else if (args.Count == 4)
            {
                UUID regionId;
                if (!UUID.TryParse(args[2], out regionId))
                {
                    SceneInterface scene;
                    if (!Scenes.TryGetValue(args[2], out scene))
                    {
                        io.Write("regionid is not a UUID nor a region name");
                        return;
                    }
                    regionId = scene.ID;
                }
                try
                {
                    io.Write("=" + GetServerParamStorage()[regionId, args[3]]);
                }
                catch (Exception e)
                {
                    io.Write("Server parameter not available");
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void SetServerParamCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help" || args.Count < 4 || args.Count > 5)
            {
                io.Write("set serverparam <regionid> <param> <value>\nset serverparam <regionname> <param> <value>\nset serverparam <param> <value>");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("set serverparam is not possible with limited console");
            }
            else if (args.Count == 4)
            {
                try
                {
                    GetServerParamStorage()[UUID.Zero, args[2]] = args[3];
                }
                catch (Exception e)
                {
                    io.Write(e.Message);
                }
            }
            else if (args.Count == 5)
            {
                UUID regionId;
                if (!UUID.TryParse(args[2], out regionId))
                {
                    SceneInterface scene;
                    if (!Scenes.TryGetValue(args[2], out scene))
                    {
                        io.Write("regionid is not a UUID nor a region name");
                        return;
                    }
                    regionId = scene.ID;
                }
                try
                {
                    GetServerParamStorage()[regionId, args[3]] = args[4];
                }
                catch (Exception e)
                {
                    io.Write(e.Message);
                }
            }
        }

        void ShowCachedDnsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if(args[0] == "help")
            {
                io.Write("Shows currently cached DNS entries");
            }
            else
            {
                StringBuilder output = new StringBuilder("Cached DNS entries:\n----------------------------------------------");
                foreach(string dns in DnsNameCache.GetCachedDnsEntries())
                {
                    output.Append("\n");
                    output.Append(dns);
                }
                io.Write(output.ToString());
            }
        }

        void RemoveCachedDnsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if(args[0] == "help" || args.Count < 3)
            {
                io.Write("delete cacheddns <host>\nRemoves a DNS cache entry");
            }
            else
            {
                if(DnsNameCache.RemoveCachedDnsEntry(args[2]))
                {
                    io.WriteFormatted("DNS Entry {0} removed", args[2]);
                }
                else
                {
                    io.WriteFormatted("DNS Entry {0} not found", args[2]);
                }
            }
        }

        void ShowModulesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Show currently loaded modules");
            }
            else
            {
                StringBuilder output = new StringBuilder("Module List:\n----------------------------------------------");
                foreach (KeyValuePair<string, IPlugin> moduledesc in PluginInstances)
                {
                    DescriptionAttribute desc = (DescriptionAttribute)Attribute.GetCustomAttribute(moduledesc.Value.GetType(), typeof(DescriptionAttribute));

                    output.AppendFormat("\nModule {0}:", moduledesc.Key);
                    if (null != desc)
                    {
                        output.Append("\n   Description: ");
                        output.Append(desc.Description);
                    }
                    foreach (KeyValuePair<Type, string> kvp in FeaturesTable)
                    {
                        if (kvp.Key.IsInterface)
                        {
                            if (moduledesc.Value.GetType().GetInterfaces().Contains(kvp.Key))
                            {
                                output.Append("\n  - ");
                                output.Append(kvp.Value);
                            }
                        }
                        else if (kvp.Key.IsAssignableFrom(moduledesc.Value.GetType()))
                        {
                            output.Append("\n  - ");
                            output.Append(kvp.Value);
                        }
                    }
                    output.Append("\n");
                }
                io.Write(output.ToString());
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        static void ShowThreadCountCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Show current thread count");
            }
            else
            {
                io.WriteFormatted("Threads: {0}", Process.GetCurrentProcess().Threads.Count);
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        void ShowQueuesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Show queue stats on instance");
            }
            else if(UUID.Zero != limitedToScene)
            {
                io.Write("Not allowed on limited console");
            }
            else
            {
                StringBuilder sb = new StringBuilder("Queue List:\n----------------------------------------------");
                foreach(KeyValuePair<string, IQueueStatsAccess> kvp in GetServices<IQueueStatsAccess>())
                {
                    foreach (QueueStatAccessor accessors in kvp.Value.QueueStats)
                    {
                        QueueStat stat = accessors.GetData();
                        sb.AppendFormat("\n{0}: {1}:\n- Status: {2}\n- Count: {3}\n- Processed: {4}\n", kvp.Key, accessors.Name, stat.Status, stat.Count, stat.Processed);
                    }
                }
                io.Write(sb.ToString());
            }
        }
        #endregion

        #region List TCP Ports
        public Dictionary<int, string> KnownTcpPorts
        {
            get
            {
                Dictionary<int, string> tcpPorts = new Dictionary<int, string>();
                tcpPorts.Add((int)HttpServer.Port, "HTTP Server");
                try
                {
                    tcpPorts.Add((int)HttpsServer.Port, "HTTPS Server");
                }
                catch
                {
                    /* no HTTPS Server */
                }
                return tcpPorts;
            }
        }
        #endregion

        #region Distribute server params
        void ShowServerParamsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, ServerParamAttribute> resList = new Dictionary<string, ServerParamAttribute>();

            foreach (KeyValuePair<string, ServerParamAttribute> kvp in ServerParams)
            {
                ServerParamAttribute paraType;
                if (!resList.TryGetValue(kvp.Key, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                {
                    resList[kvp.Key] = kvp.Value;
                }
            }

            sb.Append("Server Params:\n-------------------------------------------------\n");
            foreach(KeyValuePair<string, ServerParamAttribute> kvp in resList)
            {
                if(kvp.Value.Type == ServerParamType.GlobalOnly)
                {
                    sb.AppendFormat("{0} - global only\n", kvp.Key);
                }
                else
                {
                    sb.AppendFormat("{0} - global and region\n", kvp.Key);
                }
                if(!string.IsNullOrEmpty(kvp.Value.Description))
                {
                    sb.AppendFormat("- {0}\n", kvp.Value.Description);
                }
            }
            io.Write(sb.ToString());
        }

        public IDictionary<string, ServerParamAttribute> ServerParams
        {
            get
            {
                Dictionary<string, ServerParamAttribute> resList = new Dictionary<string, ServerParamAttribute>();
                foreach(SceneInterface scene in Scenes.Values)
                {
                    Type instanceType = scene.GetType();
                    ServerParamAttribute[] attrs = (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute));
                    foreach(ServerParamAttribute attr in attrs)
                    {
                        if (!resList.ContainsKey(attr.ParameterName))
                        {
                            resList.Add(attr.ParameterName, attr);
                        }
                    }
                }

                foreach (IServerParamListener listener in GetServicesByValue<IServerParamListener>())
                {
                    Type instanceType = listener.GetType();
                    ServerParamAttribute[] attrs = (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute));
                    foreach (ServerParamAttribute attr in attrs)
                    {
                        ServerParamAttribute paraType;
                        if (!resList.TryGetValue(attr.ParameterName, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                        {
                            resList[attr.ParameterName] = attr;
                        }
                    }

                    if (instanceType.GetInterfaces().Contains(typeof(IServerParamAnyListener)))
                    {
                        IServerParamAnyListener anyListener = (IServerParamAnyListener)listener;
                        foreach (KeyValuePair<string, ServerParamAttribute> kvp in anyListener.ServerParams)
                        {
                            ServerParamAttribute paraType;
                            if (!resList.TryGetValue(kvp.Key, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                            {
                                resList[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                return resList;
            }
        }

        public void TriggerParameterUpdated(UUID regionID, string parametername, string value)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            if(regionID == UUID.Zero)
            {
                foreach(SceneInterface scene in Scenes.Values)
                {
                    Type instanceType = scene.GetType();
                    ServerParamAttribute[] attrs = (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute));
                    foreach(ServerParamAttribute attr in attrs)
                    {
                        if(attr.ParameterName == parametername &&
                            !serverParams.Contains(regionID, parametername))
                        {
                            bool foundSpecific = false;
                            MethodInfo[] mis = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            foreach (MethodInfo mi in mis)
                            {
                                ServerParamAttribute[] mi_attrs = Attribute.GetCustomAttributes(mi, typeof(ServerParamAttribute)) as ServerParamAttribute[];
                                foreach (ServerParamAttribute mi_attr in mi_attrs)
                                {
                                    if (mi_attr.ParameterName == parametername)
                                    {
                                        Action<UUID, string> del;
                                        try
                                        {
                                            del = Delegate.CreateDelegate(typeof(Action<UUID, string>), mi) as Action<UUID, string>;
                                        }
                                        catch
                                        {
                                            /* could not create so skip it */
                                            continue;
                                        }
                                        foundSpecific = true;
#if DEBUG
                                        m_Log.DebugFormat("Updating scene {0} with parameter {1}/{2}", scene.Name, regionID.ToString(), parametername);
#endif
                                        try
                                        {
                                            del(regionID, value);
                                        }
                                        catch (Exception e)
                                        {
                                            m_Log.WarnFormat("Failed to update scene {0} with parameter {1}/{2}: {3}: {4}\n{5}", scene.Name, regionID.ToString(), parametername, e.GetType().FullName, e.Message, e.StackTrace);
                                        }
                                    }
                                }
                            }

                            if (foundSpecific)
                            {
                                continue;
                            }

                            try
                            {
                                scene.TriggerParameterUpdated(regionID, parametername, value);
                            }
                            catch(Exception e)
                            {
                                m_Log.WarnFormat("Failed to update scene {0} with parameter {1}/{2}: {3}: {4}\n{5}", scene.Name, regionID.ToString(), value, e.GetType().FullName, e.Message, e.StackTrace);
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                SceneInterface scene;
                if (Scenes.TryGetValue(regionID, out scene))
                {
                    Type instanceType = scene.GetType();
                    ServerParamAttribute[] attrs = (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute));
                    foreach (ServerParamAttribute attr in attrs)
                    {
                        if (attr.ParameterName == parametername)
                        {
                            bool foundSpecific = false;
                            MethodInfo[] mis = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            foreach (MethodInfo mi in mis)
                            {
                                ServerParamAttribute[] mi_attrs = Attribute.GetCustomAttributes(mi, typeof(ServerParamAttribute)) as ServerParamAttribute[];
                                foreach (ServerParamAttribute mi_attr in mi_attrs)
                                {
                                    if (mi_attr.ParameterName == parametername)
                                    {
                                        Action<UUID, string> del;
                                        try
                                        {
                                            del = Delegate.CreateDelegate(typeof(Action<UUID, string>), mi) as Action<UUID, string>;
                                        }
                                        catch
                                        {
                                            /* could not create so skip it */
                                            continue;
                                        }
                                        foundSpecific = true;
                                        try
                                        {
                                            del(regionID, value);
                                        }
                                        catch (Exception e)
                                        {
                                            m_Log.WarnFormat("Failed to update scene {0} with parameter {1}/{2}: {3}: {4}\n{5}", scene.Name, regionID.ToString(), value, e.GetType().FullName, e.Message, e.StackTrace);
                                        }
                                    }
                                }
                            }

                            if (foundSpecific)
                            {
                                continue;
                            }

                            try
                            {
                                scene.TriggerParameterUpdated(regionID, parametername, value);
                            }
                            catch(Exception e)
                            {
                                m_Log.WarnFormat("Failed to update scene {0} with parameter {1}/{2}: {3}: {4}\n{5}", scene.Name, regionID.ToString(), value, e.GetType().FullName, e.Message, e.StackTrace);
                            }
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        sealed class SystemIPv4Service : ExternalHostNameServiceInterface
        {
            static string m_IPv4Cached = string.Empty;
            static int m_IPv4LastCached = 0;

            public SystemIPv4Service()
            {

            }

            public override string ExternalHostName
            {
                get
                {
                    if (Environment.TickCount - m_IPv4LastCached < 60000 && m_IPv4Cached.Length != 0)
                    {
                        return m_IPv4Cached;
                    }

                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        {
                            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    m_IPv4Cached = ip.Address.ToString();
                                    m_IPv4LastCached = Environment.TickCount;
                                    return m_IPv4Cached;
                                }
                            }
                        }
                    }
                    throw new InvalidDataException("No IPv4 address found");
                }
            }
        }

        sealed class DomainNameResolveService : ExternalHostNameServiceInterface
        {
            readonly string m_HostName;
            public DomainNameResolveService(string hostname)
            {
                m_HostName = hostname;
            }

            public DomainNameResolveService()
            {

            }

            public override string ExternalHostName
            {
                get
                {
                    return m_HostName;
                }
            }
        }

        static readonly SystemIPv4Service m_SystemIPv4 = new SystemIPv4Service();
        static DomainNameResolveService m_DomainResolver;

        public ExternalHostNameServiceInterface ExternalHostNameService
        {
            get
            {
                string result = "SYSTEMIP";
                IConfig config = Config.Configs["Network"];
                if (config != null)
                {
                    result = config.GetString("ExternalHostName", "SYSTEMIP");
                }

                if(result == "SYSTEMIP")
                {
                    return m_SystemIPv4;
                }
                else if(result.StartsWith("resolver:"))
                {
                    return GetService<ExternalHostNameServiceInterface>(result.Substring(9));
                }
                else
                {
                    if(null == m_DomainResolver)
                    {
                        m_DomainResolver = new DomainNameResolveService(result);
                    }
                    return m_DomainResolver;
                }
            }
        }
    }
}
