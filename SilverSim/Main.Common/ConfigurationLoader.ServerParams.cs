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

using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilverSim.Main.Common
{
    public partial class ConfigurationLoader
    {
        #region Load Server Params
        private void LoadServerParamsForPlugin(string name, IPlugin instance, Dictionary<string, List<KeyValuePair<UUID, string>>> cachedResults)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            Type instanceType = instance.GetType();
            var startswithattrs = Attribute.GetCustomAttributes(instanceType, typeof(ServerParamStartsWithAttribute)) as ServerParamStartsWithAttribute[];
            if (instanceType.GetInterfaces().Contains(typeof(IServerParamAnyListener)) && startswithattrs.Length != 0)
            {
                var listener = (IServerParamAnyListener)instance;
#if DEBUG
                m_Log.DebugFormat("Processing {0} for start with server params", name);
#endif
                foreach (ServerParamStartsWithAttribute attr in startswithattrs)
                {
                    foreach (KeyValuePair<UUID, string> kvp in serverParams.KnownParameters)
                    {
                        if (kvp.Value.StartsWith(attr.ParameterNameStartsWith))
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
                                catch (Exception e)
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

                    if (foundSpecific)
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

        private void GetServerParamCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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

        private void ResetServerParamCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help" || args.Count < 3 || args.Count > 4)
            {
                io.Write("reset serverparam <regionid> <param>\nreset serverparam <regionname> <param>\nreset serverparam <param>");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("reset serverparam is not possible with limited console");
            }
            else if (args.Count == 3)
            {
                try
                {
                    GetServerParamStorage()[UUID.Zero, args[2]] = string.Empty;
                }
                catch (Exception e)
                {
                    io.Write(e.Message);
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
                    GetServerParamStorage()[regionId, args[3]] = string.Empty;
                }
                catch (Exception e)
                {
                    io.Write(e.Message);
                }
            }
        }

        private void SetServerParamCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
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

        private void ShowServerParamsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            var sb = new StringBuilder();
            var resList = new Dictionary<string, ServerParamAttribute>();

            foreach (KeyValuePair<string, ServerParamAttribute> kvp in ServerParams)
            {
                ServerParamAttribute paraType;
                if (!resList.TryGetValue(kvp.Key, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                {
                    resList[kvp.Key] = kvp.Value;
                }
            }

            sb.Append("Server Params:\n-------------------------------------------------\n");
            foreach (KeyValuePair<string, ServerParamAttribute> kvp in resList)
            {
                sb.AppendFormat(kvp.Value.Type == ServerParamType.GlobalOnly ? "{0} - global only\n" : "{0} - global and region\n", kvp.Key);
                if (!string.IsNullOrEmpty(kvp.Value.Description))
                {
                    sb.AppendFormat("- {0}\n", kvp.Value.Description);
                }
            }
            io.Write(sb.ToString());
        }

        private Dictionary<string, ServerParamAttribute> m_KnownServerParams;
        private readonly object m_KnownServerParamsLock = new object();

        public IReadOnlyDictionary<string, ServerParamAttribute> ServerParams
        {
            get
            {
                if (m_KnownServerParams == null)
                {
                    lock (m_KnownServerParamsLock)
                    {
                        if (m_KnownServerParams == null)
                        {
                            m_KnownServerParams = new Dictionary<string, ServerParamAttribute>();
                            foreach (SceneInterface scene in Scenes.Values)
                            {
                                Type instanceType = scene.GetType();
                                foreach (ServerParamAttribute attr in (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute)))
                                {
                                    if (!m_KnownServerParams.ContainsKey(attr.ParameterName))
                                    {
                                        m_KnownServerParams.Add(attr.ParameterName, attr);
                                    }
                                }
                            }

                            foreach (IServerParamListener listener in GetServicesByValue<IServerParamListener>())
                            {
                                Type instanceType = listener.GetType();
                                foreach (ServerParamAttribute attr in (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute)))
                                {
                                    ServerParamAttribute paraType;
                                    if (!m_KnownServerParams.TryGetValue(attr.ParameterName, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                                    {
                                        m_KnownServerParams[attr.ParameterName] = attr;
                                    }
                                }

                                if (instanceType.GetInterfaces().Contains(typeof(IServerParamAnyListener)))
                                {
                                    var anyListener = (IServerParamAnyListener)listener;
                                    foreach (KeyValuePair<string, ServerParamAttribute> kvp in anyListener.ServerParams)
                                    {
                                        ServerParamAttribute paraType;
                                        if (!m_KnownServerParams.TryGetValue(kvp.Key, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                                        {
                                            m_KnownServerParams[kvp.Key] = kvp.Value;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return m_KnownServerParams;
            }
        }

        #region Distribute server params
        public void TriggerParameterUpdated(UUID regionID, string parametername, string value)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            if (regionID == UUID.Zero)
            {
                foreach (SceneInterface scene in Scenes.Values)
                {
                    Type instanceType = scene.GetType();
                    foreach (ServerParamAttribute attr in (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute)))
                    {
                        if (attr.ParameterName == parametername &&
                            !serverParams.Contains(regionID, parametername))
                        {
                            bool foundSpecific = false;
                            foreach (MethodInfo mi in instanceType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                            {
                                foreach (ServerParamAttribute mi_attr in Attribute.GetCustomAttributes(mi, typeof(ServerParamAttribute)) as ServerParamAttribute[])
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
                            catch (Exception e)
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
                            catch (Exception e)
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

        #region Load region scene params
        private void LoadParamsOnAddedScene(SceneInterface scene)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            var cachedResults = new Dictionary<string, List<KeyValuePair<UUID, string>>>();
            Type instanceType = scene.GetType();

            foreach (ServerParamAttribute attr in Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute)) as ServerParamAttribute[])
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
                            catch (Exception e)
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
    }
}