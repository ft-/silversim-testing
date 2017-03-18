﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.XmlRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace SilverSim.Main.Common
{
    partial class ConfigurationLoader
    {
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

        void ShowHttpHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            BaseHttpServer http = HttpServer;
            StringBuilder sb = new StringBuilder("HTTP Handlers: (" + http.ServerURI + ")\n----------------------------------------------\n");
            ListHttpHandlers(sb, http);
            BaseHttpServer https;
            try
            {
                https = HttpsServer;
            }
            catch
            {
                https = null;
            }
            if (null != https)
            {
                sb.AppendFormat("\nHTTPS Handlers: ({0})\n----------------------------------------------\n", https.ServerURI);
                ListHttpHandlers(sb, https);
            }
            io.Write(sb.ToString());
        }

        void ListHttpHandlers(StringBuilder sb, BaseHttpServer server)
        {
            foreach (KeyValuePair<string, Action<HttpRequest>> kvp in server.UriHandlers)
            {
                sb.AppendFormat("URL: {0}\n", kvp.Key);
            }
            foreach (KeyValuePair<string, Action<HttpRequest>> kvp in server.StartsWithUriHandlers)
            {
                sb.AppendFormat("URL: {0}*\n", kvp.Key);
            }
            foreach (KeyValuePair<string, Action<HttpRequest>> kvp in server.RootUriContentTypeHandlers)
            {
                sb.AppendFormat("Content-Type: {0}\n", kvp.Key);
            }
        }

        void ShowThreadsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (UUID.Zero != limitedToScene)
            {
                io.Write("Not allowed on limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("Show existing threads");
            }
            else
            {
                StringBuilder sb = new StringBuilder("Threads:\n----------------------------------------------\n");
                foreach (Thread t in ThreadManager.Threads)
                {
                    sb.AppendFormat("Thread({0}): {1}\n", t.ManagedThreadId, t.Name);
                    sb.AppendFormat("- State: {0}\n", t.ThreadState.ToString());
                }
                io.Write(sb.ToString());
            }
        }

        void ShowXmlRpcHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            StringBuilder sb = new StringBuilder("XMLRPC Handlers:\n----------------------------------------------\n");
            HttpXmlRpcHandler server = XmlRpcServer;
            foreach (KeyValuePair<string, Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse>> kvp in server.XmlRpcMethods)
            {
                sb.AppendFormat("Method: {0}\n", kvp.Key);
            }
            io.Write(sb.ToString());
        }

        void ShowJson20RpcHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            StringBuilder sb = new StringBuilder("JSON2.0RPC Handlers:\n----------------------------------------------\n");
            HttpJson20RpcHandler server = Json20RpcServer;
            foreach (KeyValuePair<string, Func<string, IValue, IValue>> kvp in server.Json20RpcMethods)
            {
                sb.AppendFormat("Method: {0}\n", kvp.Key);
            }
            io.Write(sb.ToString());
        }

        void ShowCapsHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            StringBuilder sb = new StringBuilder("Caps Handlers:\n----------------------------------------------\n");
            CapsHttpRedirector redirector = CapsRedirector;
            foreach (KeyValuePair<string, RwLockedDictionary<UUID, Action<HttpRequest>>> kvp in redirector.Caps)
            {
                sb.AppendFormat("Capability: {0}\n", kvp.Key);
                foreach (KeyValuePair<UUID, Action<HttpRequest>> kvpInner in kvp.Value)
                {
                    sb.AppendFormat("- ID: {0}\n", kvpInner.Key);
                }
                sb.AppendLine();
            }
            io.Write(sb.ToString());
        }

        #region Show Port allocations
        readonly GridServiceInterface m_RegionStorage;
        void ShowPortAllocationsCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            StringBuilder sb = new StringBuilder("TCP Ports:\n----------------------------------------------\n");
            foreach (KeyValuePair<int, string> kvp in KnownTcpPorts)
            {
                sb.AppendFormat("{0}:\n- Port: {1}\n", kvp.Value, kvp.Key);
            }
            if (null != m_RegionStorage)
            {
                sb.Append("\nUDP Ports:\n----------------------------------------------\n");
                IEnumerable<RegionInfo> regions = m_RegionStorage.GetAllRegions(UUID.Zero);
                foreach (RegionInfo region in regions)
                {
                    string status;
                    status = Scenes.ContainsKey(region.ID) ? "online" : "offline";
                    sb.AppendFormat("Region \"{0}\" ({1})\n- Port: {2}\n- Status: ({3})\n", region.Name, region.ID, region.ServerPort, status);
                }
            }
            io.Write(sb.ToString());
        }
        #endregion

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

        void ShowModulesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("show modules [<searchstring>] - Show currently loaded modules");
            }
            else
            {
                string searchstring = string.Empty;
                if (args.Count > 2)
                {
                    searchstring = args[2].ToLower();
                }

                StringBuilder output = new StringBuilder("Module List:\n----------------------------------------------");
                if (!string.IsNullOrEmpty(searchstring))
                {
                    output.AppendFormat("\n<limited to modules containing \"{0}\">\n", searchstring);
                }
                foreach (KeyValuePair<string, IPlugin> moduledesc in PluginInstances)
                {
                    DescriptionAttribute desc = (DescriptionAttribute)Attribute.GetCustomAttribute(moduledesc.Value.GetType(), typeof(DescriptionAttribute));
                    if (!string.IsNullOrEmpty(searchstring) &&
                        !moduledesc.Key.ToLower().Contains(searchstring))
                    {
                        continue;
                    }

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
            else if (UUID.Zero != limitedToScene)
            {
                io.Write("Not allowed on limited console");
            }
            else
            {
                StringBuilder sb = new StringBuilder("Queue List:\n----------------------------------------------");
                foreach (KeyValuePair<string, IQueueStatsAccess> kvp in GetServices<IQueueStatsAccess>())
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
    }
}