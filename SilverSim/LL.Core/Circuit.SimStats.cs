/*

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

using SilverSim.LL.Messages;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.LL.Messages.Simulator;
using SilverSim.LL.Core.Capabilities;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        enum SimStatIndex : int
        {
            TimeDilation,
            SimFPS,
            PhysicsFPS,
            AgentUpdates,
            Agents,
            ChildAgents,
            TotalPrim,
            ActivePrim,
            FrameTimeMs,
            NetTimeMs,
            PhysicsTimeMs,
            ImageTimeMs,
            OtherTimeMs,
            InPacketsPerSecond,
            OutPacketsPerSecond,
            UnAckedBytes,
            AgentTimeMs,
            PendingDownloads,
            PendingUploads,
            ActiveScripts,
            ScriptLinesPerSecond,
            SimSpareTimeMs,

            NumStatIndex
        }

        readonly SimStats.Data[] m_SimStatsData = new SimStats.Data[(int)SimStatIndex.NumStatIndex];

        void InitSimStats()
        {
            m_SimStatsData[(int)SimStatIndex.TimeDilation] = new SimStats.Data(SimStats.Data.StatType.TimeDilation, 1);
            m_SimStatsData[(int)SimStatIndex.SimFPS] = new SimStats.Data(SimStats.Data.StatType.SimFPS, 0);
            m_SimStatsData[(int)SimStatIndex.PhysicsFPS] = new SimStats.Data(SimStats.Data.StatType.PhysicsFPS, 0);
            m_SimStatsData[(int)SimStatIndex.AgentUpdates] = new SimStats.Data(SimStats.Data.StatType.AgentUpdates, 1);
            m_SimStatsData[(int)SimStatIndex.Agents] = new SimStats.Data(SimStats.Data.StatType.Agents, 1);
            m_SimStatsData[(int)SimStatIndex.ChildAgents] = new SimStats.Data(SimStats.Data.StatType.ChildAgents, 0);
            m_SimStatsData[(int)SimStatIndex.TotalPrim] = new SimStats.Data(SimStats.Data.StatType.TotalPrim, 0);
            m_SimStatsData[(int)SimStatIndex.ActivePrim] = new SimStats.Data(SimStats.Data.StatType.ActivePrim, 0);
            m_SimStatsData[(int)SimStatIndex.FrameTimeMs] = new SimStats.Data(SimStats.Data.StatType.FrameTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.NetTimeMs] = new SimStats.Data(SimStats.Data.StatType.NetTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.PhysicsTimeMs] = new SimStats.Data(SimStats.Data.StatType.PhysicsTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.ImageTimeMs] = new SimStats.Data(SimStats.Data.StatType.ImageTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.OtherTimeMs] = new SimStats.Data(SimStats.Data.StatType.OtherTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.InPacketsPerSecond] = new SimStats.Data(SimStats.Data.StatType.InPacketsPerSecond, 1);
            m_SimStatsData[(int)SimStatIndex.OutPacketsPerSecond] = new SimStats.Data(SimStats.Data.StatType.OutPacketsPerSecond, 1);
            m_SimStatsData[(int)SimStatIndex.UnAckedBytes] = new SimStats.Data(SimStats.Data.StatType.UnAckedBytes, 0);
            m_SimStatsData[(int)SimStatIndex.AgentTimeMs] = new SimStats.Data(SimStats.Data.StatType.AgentTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.PendingDownloads] = new SimStats.Data(SimStats.Data.StatType.PendingDownloads, 0);
            m_SimStatsData[(int)SimStatIndex.PendingUploads] = new SimStats.Data(SimStats.Data.StatType.PendingUploads, 0);
            m_SimStatsData[(int)SimStatIndex.ActiveScripts] = new SimStats.Data(SimStats.Data.StatType.ActiveScripts, 0);
            m_SimStatsData[(int)SimStatIndex.ScriptLinesPerSecond] = new SimStats.Data(SimStats.Data.StatType.ScriptLinesPerSecond, 0);
            m_SimStatsData[(int)SimStatIndex.SimSpareTimeMs] = new SimStats.Data(SimStats.Data.StatType.SimSpareTimeMs, 0);
        }

        int m_LastPacketsReceived = 0;
        int m_LastPacketsSent = 0;
        int m_LastAgentUpdatesReceived = 0;

        void SendSimStats(int deltatime)
        {
            int packetsReceived = m_PacketsReceived - m_LastPacketsReceived;
            int packetsSent = m_PacketsSent - m_LastPacketsSent;
            int agentUpdatesReceived = m_AgentUpdatesReceived - m_LastAgentUpdatesReceived;
            int activeUploads = Agent.m_TerrainTransactions.Count + Agent.m_AssetTransactions.Count;
            foreach(UploadAssetAbstractCapability cap in m_UploadCapabilities)
            {
                activeUploads += cap.ActiveUploads;
            }
            m_LastPacketsSent = m_PacketsSent;
            m_LastPacketsReceived = m_PacketsReceived;
            m_LastAgentUpdatesReceived = m_AgentUpdatesReceived;

            m_SimStatsData[(int)SimStatIndex.InPacketsPerSecond].StatValue = packetsReceived * 1000f / deltatime;
            m_SimStatsData[(int)SimStatIndex.OutPacketsPerSecond].StatValue = packetsSent * 1000f / deltatime;
            LLAgent agent = Agent;
            if (agent != null)
            {
                m_SimStatsData[(int)SimStatIndex.PendingDownloads].StatValue = m_TextureDownloadQueue.Count + m_InventoryRequestQueue.Count + Agent.m_DownloadTransfers.Count;
            }
            else
            {
                m_SimStatsData[(int)SimStatIndex.PendingDownloads].StatValue = m_TextureDownloadQueue.Count + m_InventoryRequestQueue.Count;
            }
            m_SimStatsData[(int)SimStatIndex.PendingUploads].StatValue = activeUploads;
            m_SimStatsData[(int)SimStatIndex.AgentUpdates].StatValue = agentUpdatesReceived * 1000f / deltatime;
            m_SimStatsData[(int)SimStatIndex.UnAckedBytes].StatValue = m_UnackedBytes;
            m_SimStatsData[(int)SimStatIndex.TotalPrim].StatValue = Scene.Primitives.Count;

            SimStats stats = new SimStats();
            stats.RegionX = Scene.RegionData.Location.X;
            stats.RegionY = Scene.RegionData.Location.Y;
            stats.RegionFlags = 0;
            stats.ObjectCapacity = 15000;
            stats.PID = 0;
            stats.Stat = m_SimStatsData;
            SendMessage(stats);
        }
    }
}
