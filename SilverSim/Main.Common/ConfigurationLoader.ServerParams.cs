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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilverSim.Main.Common
{
    partial class ConfigurationLoader
    {

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

        public IDictionary<string, ServerParamAttribute> ServerParams
        {
            get
            {
                Dictionary<string, ServerParamAttribute> resList = new Dictionary<string, ServerParamAttribute>();
                foreach (SceneInterface scene in Scenes.Values)
                {
                    Type instanceType = scene.GetType();
                    ServerParamAttribute[] attrs = (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute));
                    foreach (ServerParamAttribute attr in attrs)
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

        #region Distribute server params
        public void TriggerParameterUpdated(UUID regionID, string parametername, string value)
        {
            ServerParamServiceInterface serverParams = GetServerParamStorage();
            if (regionID == UUID.Zero)
            {
                foreach (SceneInterface scene in Scenes.Values)
                {
                    Type instanceType = scene.GetType();
                    ServerParamAttribute[] attrs = (ServerParamAttribute[])Attribute.GetCustomAttributes(instanceType, typeof(ServerParamAttribute));
                    foreach (ServerParamAttribute attr in attrs)
                    {
                        if (attr.ParameterName == parametername &&
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