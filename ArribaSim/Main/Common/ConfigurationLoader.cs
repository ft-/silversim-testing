using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Config;
using Nini.Config;
using Mono.Addins;
using ThreadedClasses;

namespace ArribaSim.Main.Common
{
    public class ConfigurationLoader
    {
        public const string REGISTRY_LOCATION_PATH = "../data";

        public class ConfigurationError : Exception
        {
            public ConfigurationError()
            {

            }
        }

        private ILog m_Log;
        private IConfigSource m_Config = new IniConfigSource();
        private Queue<CFG_ISource> m_Sources = new Queue<CFG_ISource>();
        public RwLockedList<object> PluginInstances = new RwLockedList<object>();
        
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
                    return "Could not load ini file {0}";
                }
            }

            public string Name
            {
                get
                {
                    return m_Info;
                }
            }

            public IConfigSource ConfigSource
            {
                get
                {
                    string[] names = GetType().Assembly.GetManifestResourceNames();
                    string assemblyName = GetType().Assembly.GetName().Name;
                    Stream resource = GetType().Assembly.GetManifestResourceStream(assemblyName + ".Resources." + m_Name);
                    if(null == resource)
                    {
                        throw new FileNotFoundException();
                    }
                    return new IniConfigSource(resource);
                }
            }
        }

        #endregion

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

        private void LoadModules()
        {
            foreach (IConfig config in m_Config.Configs)
            {
                foreach (string key in config.GetKeys())
                {
                    if (key.StartsWith("Module"))
                    {
                        PluginFactory module = AddinManager.GetExtensionNode<PluginFactory>(config.GetString(key));
                        module.Initialize(this, config);
                    }
                }
            }
        }

        private void ProcessParameterMap()
        {
            IConfig parameterMap = m_Config.Configs["ParameterMap"];

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
            }
        }

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
                    string resourcename = resourceMap.Get(key) + "." + config.Get(parts[1]) + ".ini";
                    m_Sources.Enqueue(new CFG_IniResourceSource(resourcename, 
                        String.Format("Parameter {1} in section {0} is invalid", parts[1], parts[0])));
                }
            }
        }

        public ConfigurationLoader(string[] args, string defaultConfigName, string defaultsIniName)
        {
            ArgvConfigSource configSource = new ArgvConfigSource(args);
            configSource.AddSwitch("Startup", "config");
            IConfig startup = configSource.Configs["Startup"];
            string mainConfig = startup.GetString("config", defaultConfigName);

            m_Sources.Enqueue(new CFG_IniResourceSource(defaultsIniName));
            AddSource(mainConfig);

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
                ProcessParameterMap();
                ProcessResourceMap();
            }

            string registryLocation = REGISTRY_LOCATION_PATH;
            string logConfigFile = string.Empty;
            IConfig startupConfig = m_Config.Configs["Startup"];
            if(startupConfig != null)
            {
                registryLocation = startupConfig.GetString("RegistryLocation", REGISTRY_LOCATION_PATH);
                logConfigFile = startupConfig.GetString("LogConfig", string.Empty);
            }

            AddinManager.Initialize(".", "plugins", registryLocation);

            /* Initialize Log system */
            if (logConfigFile != String.Empty)
            {
                XmlConfigurator.Configure(new System.IO.FileInfo(logConfigFile));
                m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                m_Log.InfoFormat("[MAIN]: configured log4net using \"{0}\" as configuration file",
                                 logConfigFile);
            }
            else
            {
                XmlConfigurator.Configure();
                m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                m_Log.Info("[MAIN]: configured log4net using defaults");
            }

            m_Log.Info("Updating addin registry");
            AddinManager.Registry.Update();

            m_Log.Info("Loading specified modules");
            LoadModules();

        }
    }
}
