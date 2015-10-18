// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Viewer.Messages.Simulator;
using SilverSim.Viewer.Core.Capabilities;
using SilverSim.Scene.Types.Physics;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
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

        int m_LastPacketsReceived;
        int m_LastPacketsSent;
        int m_LastAgentUpdatesReceived;

        protected override void SendSimStats(int dt)
        {
            int packetsReceived = m_PacketsReceived - m_LastPacketsReceived;
            int packetsSent = m_PacketsSent - m_LastPacketsSent;
            int agentUpdatesReceived = m_AgentUpdatesReceived - m_LastAgentUpdatesReceived;
            int activeUploads = 0;
            ViewerAgent agent = Agent;
            if (agent != null)
            {
                activeUploads += agent.m_TerrainTransactions.Count + agent.m_AssetTransactions.Count;
            }
            foreach(UploadAssetAbstractCapability cap in m_UploadCapabilities)
            {
                activeUploads += cap.ActiveUploads;
            }
            m_LastPacketsSent = m_PacketsSent;
            m_LastPacketsReceived = m_PacketsReceived;
            m_LastAgentUpdatesReceived = m_AgentUpdatesReceived;

            m_SimStatsData[(int)SimStatIndex.InPacketsPerSecond].StatValue = (double)packetsReceived * 1000f / dt;
            m_SimStatsData[(int)SimStatIndex.OutPacketsPerSecond].StatValue = (double)packetsSent * 1000f / dt;
            if (agent != null)
            {
                m_SimStatsData[(int)SimStatIndex.PendingDownloads].StatValue = m_TextureDownloadQueue.Count + m_InventoryRequestQueue.Count + agent.m_DownloadTransfers.Count;
            }
            else
            {
                m_SimStatsData[(int)SimStatIndex.PendingDownloads].StatValue = m_TextureDownloadQueue.Count + m_InventoryRequestQueue.Count;
            }
            m_SimStatsData[(int)SimStatIndex.PendingUploads].StatValue = activeUploads;
            m_SimStatsData[(int)SimStatIndex.AgentUpdates].StatValue = (double)agentUpdatesReceived * 1000f / dt;
            m_SimStatsData[(int)SimStatIndex.UnAckedBytes].StatValue = m_UnackedBytes;
            m_SimStatsData[(int)SimStatIndex.TotalPrim].StatValue = Scene.Primitives.Count;
            IPhysicsScene physics = Scene.PhysicsScene;
            if(physics != null)
            {
                m_SimStatsData[(int)SimStatIndex.PhysicsFPS].StatValue = physics.PhysicsFPS;
                m_SimStatsData[(int)SimStatIndex.PhysicsTimeMs].StatValue = physics.PhysicsExecutionTime * 1000f;
            }

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
