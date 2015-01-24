﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using log4net.Config;
using Nini.Config;
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.RegionLoader;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Main.Common
{
    public class ConfigurationLoader
    {
        class ResourceAssetPlugin : SceneInterface.ResourceAssetService, IPlugin
        {
            public ResourceAssetPlugin()
            {

            }

            public void Startup(ConfigurationLoader loader)
            {

            }
        }

        public class ConfigurationError : Exception
        {
            public ConfigurationError()
            {

            }

            public ConfigurationError(string msg)
                : base(msg)
            {

            }
        }

        private ILog m_Log;
        private IConfigSource m_Config = new IniConfigSource();
        private Queue<CFG_ISource> m_Sources = new Queue<CFG_ISource>();
        private RwLockedDictionary<string, IPlugin> PluginInstances = new RwLockedDictionary<string, IPlugin>();
        private ManualResetEvent m_ShutdownEvent;

        static ConfigurationLoader()
        {
            /* prevent circular dependencies by assigning relevant parts here */
            ObjectGroup.CompilerRegistry = CompilerRegistry.ScriptCompilers;
        }

        public IConfigSource Config
        {
            get
            {
                return m_Config;
            }
        }

        public void AddPlugin(string name, IPlugin plugin)
        {
            PluginInstances.Add("$" + name, plugin);
        }

        public T GetService<T>(string serviceName)
        {
            IPlugin module = PluginInstances[serviceName];
            if(!typeof(T).IsAssignableFrom(module.GetType()))
            {
                throw new InvalidOperationException("Unexpected module configured for service " + serviceName);
            }
            return (T)module;
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

        #region Configuration Loader Helpers
        private interface CFG_ISource
        {
            IConfigSource ConfigSource { get; }
            string Name { get; }
            string Message { get; }
            string DirName { get; }
        }

        private class CFG_IniFileSource : CFG_ISource
        {
            string m_FileName;
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

        private class CFG_NiniXmlFileSource : CFG_ISource
        {
            string m_Filename;
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

        private class CFG_NiniXmlUriSource : CFG_ISource
        {
            string m_Uri;
            public CFG_NiniXmlUriSource(string uri)
            {
                m_Uri = uri;
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
                    XmlReader r = XmlReader.Create(m_Uri);
                    return new XmlConfigSource(r);
                }
            }
        }

        private class CFG_IniResourceSource : CFG_ISource
        {
            string m_Name;
            string m_Info;
            string m_Assembly = string.Empty;

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

            public IConfigSource ConfigSource
            {
                get
                {
                    Assembly assembly;
                    if(m_Assembly != string.Empty)
                    {
                        try
                        {
                            assembly = Assembly.LoadFrom("plugins/" + m_Assembly + ".dll");
                        }
                        catch
                        {
                            throw new FileNotFoundException();
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
                        System.Console.Write(assemblyName + ".Resources." + m_Name);
                        System.Console.WriteLine();
                        throw new FileNotFoundException();
                    }
                    return new IniConfigSource(resource);
                }
            }
        }

        #endregion

        #region Config Source Management
        private void AddSource(CFG_ISource cfgsource, string file)
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
            else
            {
                m_Sources.Enqueue(new CFG_IniFileSource(file));
            }
        }

        private void AddIncludes(CFG_ISource cfgsource)
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
                m_Sources.Enqueue(new CFG_IniResourceSource(nameparts[0], info));
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

        SilverSim.Types.Assembly.InterfaceVersion GetInterfaceVersion(Assembly assembly)
        {
            foreach(object o in assembly.GetCustomAttributes(false))
            {
                if(o is SilverSim.Types.Assembly.InterfaceVersion)
                {
                    return (SilverSim.Types.Assembly.InterfaceVersion)o;
                }
            }
            m_Log.FatalFormat("Assembly {0} misses InterfaceVersion information", assembly.FullName);
            throw new ConfigurationError();
        }
 
        private Type FindPluginInAssembly(Assembly assembly, string pluginName)
        {
            foreach(Type t in assembly.GetTypes())
            {
                foreach(object o in t.GetCustomAttributes(typeof(PluginName), false))
                {
                    if(((PluginName)o).Name == pluginName)
                    {
                        return t;
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        private void LoadModules()
        {
            SilverSim.Types.Assembly.InterfaceVersion ownVersion = GetInterfaceVersion(Assembly.GetExecutingAssembly());
            foreach (IConfig config in m_Config.Configs)
            {
                if (config.Contains("IsTemplate"))
                {
                    continue;
                }
                foreach (string key in config.GetKeys())
                {
                    if (key.Equals("Module"))
                    {
                        string modulename = config.GetString(key);
                        string[] modulenameparts = modulename.Split(new char[] {':'}, 2, StringSplitOptions.None);
                        if(modulenameparts.Length < 2)
                        {
                            m_Log.FatalFormat("Invalid Module in section {0}: {1}", config.Name, modulename);
                            throw new ConfigurationError();
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
                            throw new ConfigurationError();
                        }

                        SilverSim.Types.Assembly.InterfaceVersion loadedVersion = GetInterfaceVersion(assembly);
                        if(loadedVersion.Version != ownVersion.Version)
                        {
                            m_Log.FatalFormat("Failed to load module {0}: interface version mismatch: {2} != {1}", assemblyname, ownVersion, loadedVersion);
                            throw new ConfigurationError();
                        }

                        /* try to load class from assembly */
                        Type t;
                        try
                        {
                            t = FindPluginInAssembly(assembly, modulenameparts[1]);
                        }
                        catch(Exception e)
                        {
                            m_Log.FatalFormat("Failed to load factory for {1} in module {0}: {2}", assemblyname, modulenameparts[1], e.Message);
                            throw new ConfigurationError();
                        }

                        if(t == null)
                        {
                            m_Log.FatalFormat("Failed to load factory for {1} in module {0}: factory not found", assemblyname, modulenameparts[1]);
                            throw new ConfigurationError();
                        }

                        /* check type inheritance first */
                        if (!t.GetInterfaces().Contains(typeof(IPluginFactory)))
                        {
                            m_Log.FatalFormat("Failed to load factory for {1} in module {0}: not a factory", assemblyname, modulenameparts[1]);
                            throw new ConfigurationError();
                        }

                        IPluginFactory module = (IPluginFactory)assembly.CreateInstance(t.FullName);
                        PluginInstances.Add(config.Name, module.Initialize(this, config));
                    }
                }
            }
        }
        #endregion

        #region Process [ParameterMap] section
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
        private void ProcessResourceMap()
        {
            IConfig resourceMap = m_Config.Configs["ResourceMap"];

            foreach(string key in resourceMap.GetKeys())
            {
                string[] parts = key.Split(new char[] { '.' }, 2, StringSplitOptions.None);
                if(parts.Length < 2)
                {
                    continue;
                }
                IConfig config = m_Config.Configs[parts[0]];
                if(config == null)
                {
                    continue;
                }
                if(config.Contains(parts[1]))
                {
                    if (config.Get(parts[1]) != string.Empty)
                    {
                        string configname = resourceMap.GetString(key) + "." + config.Get(parts[1]) + ".ini";
                        AddResourceConfig(configname,
                            String.Format("Parameter {1} = {2} in section {0} is invalid", parts[0], parts[1], config.Get(parts[1])));
                        config.Remove(parts[1]);
                    }
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
                        throw new ConfigurationError();
                    }
                    IConfig configSection = m_Config.Configs[section];
                    if(!configSection.Contains("IsTemplate"))
                    {
                        System.Console.Write("Use does not reference a valid template");
                        System.Console.WriteLine();
                        throw new ConfigurationError();
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

        #region Constructor and Main
        public ConfigurationLoader(string[] args, string defaultConfigName, string defaultsIniName, ManualResetEvent shutdownEvent)
        {
            m_ShutdownEvent = shutdownEvent;
            ArgvConfigSource configSource = new ArgvConfigSource(args);
            configSource.AddSwitch("Startup", "config");
            IConfig startup = configSource.Configs["Startup"];
            string mainConfig = startup.GetString("config", defaultConfigName);

            m_Sources.Enqueue(new CFG_IniResourceSource(defaultsIniName));
            /* make the resource assets available for all users not just scene */
            PluginInstances.Add("ResourceAssetService", new ResourceAssetPlugin());
            PluginInstances.Add("LocalNeighborConnector", new Neighbor.LocalNeighborConnector());
            AddSource(mainConfig);

            CmdIO.CommandRegistry.Commands.Add("shutdown", ShutdownCommand);

            while(m_Sources.Count != 0)
            {
                CFG_ISource source = m_Sources.Dequeue();
                try
                {
                    m_Config.Merge(source.ConfigSource);
                }
                catch
                {
                    System.Console.Write(String.Format(source.Message, source.Name));
                    System.Console.WriteLine();
                    throw new ConfigurationError();
                }
                AddIncludes(source);
                ProcessImportResources();
                ProcessParameterMap();
                ProcessResourceMap();
            }
            ProcessParameterMap();
            ProcessUseTemplates();

            string logConfigFile = string.Empty;
            IConfig startupConfig = m_Config.Configs["Startup"];
            if(startupConfig != null)
            {
                logConfigFile = startupConfig.GetString("LogConfig", string.Empty);
            }

            /* Initialize Log system */
            if (logConfigFile != String.Empty)
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

            IConfig consoleConfig = m_Config.Configs["Console"];
            if (null == consoleConfig || consoleConfig.GetBoolean("EnableLocalConsole", true))
            {
                PluginInstances.Add("LocalConsole", new Console.LocalConsole());
            }

            m_Log.InfoFormat("Simulator: {0}", VersionInfo.ProductName);
            m_Log.InfoFormat("Version: {0}", VersionInfo.Version);
            m_Log.InfoFormat("Runtime: {0} {1}", VersionInfo.RuntimeInformation, VersionInfo.MachineWidth);

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
                catch(Exception e)
                {
                    m_Log.FatalFormat("Database connection verification for {0} failed", p.Key);
                    throw e;
                }
            }

            m_Log.Info("Process Migrations of all database modules");
            foreach (KeyValuePair<string, IDBServiceInterface> p in dbInterfaces)
            {
                m_Log.InfoFormat("-> {0}", p.Key);
                p.Value.ProcessMigrations();
            }

            m_Log.Info("Initializing HTTP Server");
            IConfig httpConfig = m_Config.Configs["Network"];
            if(null == httpConfig)
            {
                m_Log.Fatal("Missing configuration section [Network]");
                throw new ConfigurationError();
            }

            PluginInstances.Add("HttpServer", new BaseHttpServer(httpConfig));
            PluginInstances.Add("XmlRpcServer", new HttpXmlRpcHandler());
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

            m_Log.Info("Loading regions");
            foreach(IRegionLoaderInterface regionLoader in GetServices<IRegionLoaderInterface>().Values)
            {
                regionLoader.LoadRegions();
            }

        }
        #endregion

        #region Shutdown Control
        public void TriggerShutdown()
        {
            m_ShutdownEvent.Set();
        }

        public void ShutdownCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("shutdown not allowed from restricted console");
            }
            else
            {
                m_ShutdownEvent.Set();
            }
        }

        public void Shutdown()
        {
            CommandManager.ClearCommands();
            List<IPluginShutdown> shutdownLogoutAgentsList = new List<IPluginShutdown>();
            List<IPluginShutdown> shutdownLogoutRegionsList = new List<IPluginShutdown>();
            List<IPluginShutdown> shutdownAnyList = new List<IPluginShutdown>();

            foreach(IPluginShutdown s in GetServices<IPluginShutdown>().Values)
            {
                switch(s.ShutdownOrder)
                {
                    case ShutdownOrder.Any: shutdownAnyList.Add(s); break;
                    case ShutdownOrder.LogoutAgents: shutdownLogoutAgentsList.Add(s); break;
                    case ShutdownOrder.LogoutRegion: shutdownLogoutRegionsList.Add(s); break;
                }
            }

            foreach (IPluginShutdown s in shutdownLogoutAgentsList)
            {
                s.Shutdown();
            }

            SceneManager.Scenes.RemoveAll();

            foreach (IPluginShutdown s in shutdownLogoutRegionsList)
            {
                s.Shutdown();
            }

            foreach (IPluginShutdown s in shutdownAnyList)
            {
                s.Shutdown();
            }
        }
        #endregion
    }
}
