// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Http.Client;
using SilverSim.Types.Assembly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SilverSim.Main.Common
{
    partial class ConfigurationLoader
    {
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
                    using (XmlReader r = new XmlTextReader(HttpClient.DoStreamGetRequest(m_Uri, null, 20000)))
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
                    string assembly = (m_Assembly.Length != 0) ?
                        m_Assembly :
                        GetType().Assembly.GetName().Name;
                    return "x-resource://" + assembly + "/" + m_Name;
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
                    string assembly = (m_Assembly.Length != 0) ?
                        m_Assembly :
                        GetType().Assembly.GetName().Name;
                    return "x-resource://" + assembly + "/" + m_Name;
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
            if (Uri.TryCreate(file, UriKind.Absolute,
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
            if (Uri.TryCreate(file, UriKind.Absolute,
                    out configUri))
            {
                m_Sources.Enqueue(new CFG_NiniXmlUriSource(file));
            }
            else if (file.EndsWith(".xml"))
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
            foreach (IConfig config in m_Config.Configs)
            {
                foreach (string key in config.GetKeys())
                {
                    if (key.StartsWith("Include"))
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
            else if (nameparts[0].EndsWith(".xml"))
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
            PluginInstances.ForEach(delegate (KeyValuePair<string, IPlugin> p)
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
            if (null != attr)
            {
                return attr;
            }
            throw new ConfigurationErrorException(string.Format("Assembly {0} misses InterfaceVersion information", assembly.FullName));
        }

        private Type FindPluginInAssembly(Assembly assembly, string pluginName)
        {
            foreach (Type t in assembly.GetTypes())
            {
                foreach (object o in t.GetCustomAttributes(typeof(PluginNameAttribute), false))
                {
                    if (((PluginNameAttribute)o).Name == pluginName)
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
            if (PreloadPlatformAssemblies.TryGetValue(aName.Name, out assembly))
            {
                return assembly;
            }

            switch (Environment.OSVersion.Platform)
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

            if (!File.Exists(assemblyFileName))
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
                    if (key.Equals(archModule) || key.Equals("Module"))
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
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
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
            if (null == parameterMap)
            {
                return;
            }

            IConfig processedParameterMaps = m_Config.Configs["ProcessedParameterMaps"];
            if (null == processedParameterMaps)
            {
                processedParameterMaps = m_Config.AddConfig("ProcessedParameterMaps");
            }

            foreach (string key in parameterMap.GetKeys())
            {
                string val = parameterMap.GetString(key);
                string[] toparts = key.Split(new char[] { '.' }, 2, StringSplitOptions.None);
                string[] fromparts = val.Split(new char[] { '.' }, 2, StringSplitOptions.None);
                if (fromparts.Length < 2 || toparts.Length < 2)
                {
                    continue;
                }

                IConfig fromconfig = m_Config.Configs[fromparts[0]];
                if (fromconfig == null)
                {
                    continue;
                }

                if (!fromconfig.Contains(fromparts[1]))
                {
                    continue;
                }

                IConfig toconfig = m_Config.Configs[toparts[0]];
                if (toconfig == null)
                {
                    toconfig = m_Config.AddConfig(toparts[0]);
                }

                if (toconfig.Contains(toparts[1]))
                {
                    /* do not overwrite existing keys */
                    continue;
                }


                processedParameterMaps.Set(key, val);
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

        #region Process UseSourceParameter lines
        private void ProcessUseSourceParameter()
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
                    config.Remove("UseSourceParameter");
                    continue;
                }
                IConfig sourceConfig = m_Config.Configs[useparam[0]];
                if (null == sourceConfig || !sourceConfig.Contains(useparam[1]))
                {
                    continue;
                }

                string sourceParam = sourceConfig.GetString(useparam[1]);

                if (string.IsNullOrEmpty(sourceParam) || sourceParam.StartsWith("SourceParameter") ||
                    !config.Contains("SourceParameter-" + sourceParam))
                {
                    continue;
                }
                config.Remove("UseSourceParameter");

                string inputsource = config.GetString("SourceParameter-" + sourceParam);
                if (inputsource.Contains(":"))
                {
                    config.Set("ImportResource-Generated", inputsource.StartsWith(":") ? inputsource.Substring(1) : inputsource);
                }
                else
                {
                    AddSource(inputsource);
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
                foreach (string section in sections)
                {
                    if (section == config.Name)
                    {
                        System.Console.Write("Self referencing Use");
                        System.Console.WriteLine();
                        throw new ConfigurationErrorException();
                    }
                    IConfig configSection = m_Config.Configs[section];
                    if (!configSection.Contains("IsTemplate"))
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

        void ProcessConfigurations(bool processParameterMap = true)
        {
            IConfig importedInfo;
            importedInfo = m_Config.Configs["ImportedConfigs"];
            if (null == importedInfo)
            {
                importedInfo = m_Config.AddConfig("ImportedConfigs");
            }
            while (m_Sources.Count != 0)
            {
                ICFG_Source source = m_Sources.Dequeue();
                try
                {
#if DEBUG
                    System.Console.WriteLine("Processing config {0}", source.Name);
#endif
                    importedInfo.Set("Imported-" + source.Name, true);
                    m_Config.Merge(source.ConfigSource);
                }
                catch
                {
                    System.Console.Write(String.Format(source.Message, source.Name));
                    System.Console.WriteLine();
                    throw new ConfigurationErrorException();
                }
                LoadGridsXml();
                ProcessUseSourceParameter();
                AddIncludes(source);
                ProcessImportResources();
                if (processParameterMap)
                {
                    ProcessParameterMap();
                }
                ProcessResourceMap();
            }
            LoadGridsXml();
            if (processParameterMap)
            {
                ProcessParameterMap();
            }
            ProcessUseTemplates();
        }

        void ShowModeHelp()
        {
            string searchstring = GetType().Assembly.GetName().Name + ".Resources.ModeConfig.";
            int searchstringlen = searchstring.Length;
            foreach (string res in GetType().Assembly.GetManifestResourceNames())
            {
                if (res.StartsWith(searchstring))
                {
                    string modepara = res.Substring(searchstringlen);
                    modepara = modepara.Substring(0, modepara.Length - 4);
                    try
                    {
                        IConfigSource modeParamsSource = new CFG_IniResourceSource("ModeConfig." + modepara.ToLower() + ".ini").ConfigSource;
                        IConfig modeConfig = modeParamsSource.Configs["ModeConfig"];
                        string description = modeConfig.GetString("Description", string.Empty);
                        if (!string.IsNullOrEmpty(description))
                        {
                            System.Console.WriteLine(string.Format("-m={0}\n  {1}", modepara, description));
                            string defaultsIniName = modeConfig.GetString("DefaultConfigName", string.Empty);
                            if (!string.IsNullOrEmpty(defaultsIniName))
                            {
                                System.Console.WriteLine(string.Format("  defaults to use {0}", defaultsIniName));
                            }
                            System.Console.WriteLine();
                        }
                    }
                    catch
                    {
                        /* intentionally left empty */
                    }
                }
            }
        }
    }
}
