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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.MuteList;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.MuteList;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.MuteList;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace SilverSim.Viewer.MuteList
{
    [Description("Viewer MuteList Handler")]
    [PluginName("ViewerMuteListServer")]
    public sealed class ViewerMuteListServer : IPlugin, IPacketHandlerExtender, IPluginShutdown
    {
        [PacketHandler(MessageType.MuteListRequest)]
        [PacketHandler(MessageType.RemoveMuteListEntry)]
        [PacketHandler(MessageType.UpdateMuteListEntry)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private bool m_ShutdownMuteList;

        public void Startup(ConfigurationLoader loader)
        {
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "MuteList Handler Thread";

            while (!m_ShutdownMuteList)
            {
                KeyValuePair<AgentCircuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;

                if (req.Key == null)
                {
                    continue;
                }

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MuteListRequest:
                            HandleMuteListRequest(req.Key, m);
                            break;

                        case MessageType.UpdateMuteListEntry:
                            HandleUpdateMuteListEntry(req.Key, m);
                            break;

                        case MessageType.RemoveMuteListEntry:
                            HandleRemoveMuteListEntry(req.Key, m);
                            break;

                        default:
                            break;
                    }
                }
                catch
                {
                    /* intentionally ignored */
                }
            }
        }

        private void HandleMuteListRequest(AgentCircuit circuit, Message m)
        {
            ViewerAgent agent = circuit.Agent;
            MuteListServiceInterface muteListService = agent.MuteListService;

            if(muteListService == null)
            {
                circuit.SendMessage(new UseCachedMuteList { AgentID = agent.ID });
                return;
            }

            string filename = $"mutes{agent.ID}";
            using (var ms = new MemoryStream())
            {
                using (StreamWriter writer = ms.UTF8StreamWriter())
                {
                    foreach (MuteListEntry entry in muteListService.GetList(agent.ID))
                    {
                        writer.WriteLine("{0} {1} {2}|{3}", (int)entry.Type, entry.MuteID.ToString(), entry.MuteName, (uint)entry.Flags);
                    }
                }
                agent.AddNewFile(filename, ms.ToArray());
            }

            circuit.SendMessage(new MuteListUpdate { AgentID = agent.ID, Filename = filename });
        }

        private void HandleUpdateMuteListEntry(AgentCircuit circuit, Message m)
        {
            ViewerAgent agent = circuit.Agent;
            MuteListServiceInterface muteListService = agent.MuteListService;
            var req = (UpdateMuteListEntry)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if (muteListService == null)
            {
                return;
            }

            muteListService.Store(agent.ID, new MuteListEntry
            {
                Flags = req.MuteFlags,
                MuteID = req.MuteID,
                MuteName = req.MuteName,
                Type = req.MuteType
            });
        }

        private void HandleRemoveMuteListEntry(AgentCircuit circuit, Message m)
        {
            ViewerAgent agent = circuit.Agent;
            MuteListServiceInterface muteListService = agent.MuteListService;
            var req = (RemoveMuteListEntry)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if (muteListService == null)
            {
                return;
            }

            muteListService.Remove(agent.ID, req.MuteID, req.MuteName);
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownMuteList = true;
        }
    }
}
