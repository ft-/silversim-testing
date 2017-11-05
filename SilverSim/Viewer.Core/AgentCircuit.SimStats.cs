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

using SilverSim.Threading;
using SilverSim.Types.Estate;
using SilverSim.Viewer.Messages.Simulator;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        public enum SimStatIndex
        {
            TimeDilation,
            SimFPS,
            PhysicsFPS,
            AgentUpdates,
            Agents,
            ChildAgents,
            TotalPrim,
            ActivePrim,
            PhysicsTimeMs,
            InPacketsPerSecond,
            OutPacketsPerSecond,
            UnAckedBytes,
            PendingDownloads,
            PendingUploads,
            ActiveScripts,
            ScriptEventsPerSeconds,
            PercentExecutingScripts,

            NumStatIndex
        }

        private readonly SimStats.Data[] m_SimStatsData = new SimStats.Data[(int)SimStatIndex.NumStatIndex];

        private void InitSimStats()
        {
            m_SimStatsData[(int)SimStatIndex.TimeDilation] = new SimStats.Data(SimStats.Data.StatType.TimeDilation, 1);
            m_SimStatsData[(int)SimStatIndex.SimFPS] = new SimStats.Data(SimStats.Data.StatType.SimFPS, 0);
            m_SimStatsData[(int)SimStatIndex.PhysicsFPS] = new SimStats.Data(SimStats.Data.StatType.PhysicsFPS, 0);
            m_SimStatsData[(int)SimStatIndex.AgentUpdates] = new SimStats.Data(SimStats.Data.StatType.AgentUpdates, 1);
            m_SimStatsData[(int)SimStatIndex.Agents] = new SimStats.Data(SimStats.Data.StatType.Agents, 1);
            m_SimStatsData[(int)SimStatIndex.ChildAgents] = new SimStats.Data(SimStats.Data.StatType.ChildAgents, 0);
            m_SimStatsData[(int)SimStatIndex.TotalPrim] = new SimStats.Data(SimStats.Data.StatType.TotalPrim, 0);
            m_SimStatsData[(int)SimStatIndex.ActivePrim] = new SimStats.Data(SimStats.Data.StatType.ActivePrim, 0);
            m_SimStatsData[(int)SimStatIndex.PhysicsTimeMs] = new SimStats.Data(SimStats.Data.StatType.PhysicsTimeMs, 0);
            m_SimStatsData[(int)SimStatIndex.InPacketsPerSecond] = new SimStats.Data(SimStats.Data.StatType.InPacketsPerSecond, 1);
            m_SimStatsData[(int)SimStatIndex.OutPacketsPerSecond] = new SimStats.Data(SimStats.Data.StatType.OutPacketsPerSecond, 1);
            m_SimStatsData[(int)SimStatIndex.UnAckedBytes] = new SimStats.Data(SimStats.Data.StatType.UnAckedBytes, 0);
            m_SimStatsData[(int)SimStatIndex.PendingDownloads] = new SimStats.Data(SimStats.Data.StatType.PendingDownloads, 0);
            m_SimStatsData[(int)SimStatIndex.PendingUploads] = new SimStats.Data(SimStats.Data.StatType.PendingUploads, 0);
            m_SimStatsData[(int)SimStatIndex.ActiveScripts] = new SimStats.Data(SimStats.Data.StatType.ActiveScripts, 0);
            m_SimStatsData[(int)SimStatIndex.ScriptEventsPerSeconds] = new SimStats.Data(SimStats.Data.StatType.ScriptEps, 0);
            m_SimStatsData[(int)SimStatIndex.PercentExecutingScripts] = new SimStats.Data(SimStats.Data.StatType.PercentScriptsRun, 0);
        }

        private int m_LastPacketsReceived;
        private int m_LastPacketsSent;
        private int m_LastAgentUpdatesReceived;

        protected override void SendSimStats(long dt)
        {
            int packetsReceived = m_PacketsReceived - m_LastPacketsReceived;
            int packetsSent = m_PacketsSent - m_LastPacketsSent;
            int agentUpdatesReceived = m_AgentUpdatesReceived - m_LastAgentUpdatesReceived;
            int activeUploads = 0;
            var agent = Agent;
            var scene = Scene;
            if (agent != null)
            {
                activeUploads += agent.m_TerrainTransactions.Count + agent.m_AssetTransactions.Count;
            }
            foreach (var cap in m_UploadCapabilities)
            {
                activeUploads += cap.ActiveUploads;
            }
            m_LastPacketsSent = m_PacketsSent;
            m_LastPacketsReceived = m_PacketsReceived;
            m_LastAgentUpdatesReceived = m_AgentUpdatesReceived;

            m_SimStatsData[(int)SimStatIndex.InPacketsPerSecond].StatValue = (double)packetsReceived * StopWatchTime.Frequency / dt;
            m_SimStatsData[(int)SimStatIndex.OutPacketsPerSecond].StatValue = (double)packetsSent * StopWatchTime.Frequency / dt;

            m_SimStatsData[(int)SimStatIndex.PendingDownloads].StatValue = agent != null ?
                m_TextureDownloadQueue.Count + m_InventoryRequestQueue.Count + agent.m_DownloadTransfersByName.Count :
                m_TextureDownloadQueue.Count + m_InventoryRequestQueue.Count;

            m_SimStatsData[(int)SimStatIndex.PendingUploads].StatValue = activeUploads;
            m_SimStatsData[(int)SimStatIndex.AgentUpdates].StatValue = (double)agentUpdatesReceived * StopWatchTime.Frequency / dt;
            m_SimStatsData[(int)SimStatIndex.UnAckedBytes].StatValue = m_UnackedBytes;
            if (scene != null)
            {
                int rootAgents = scene.RootAgents.Count;
                int childAgents = scene.Agents.Count - rootAgents;
                if(childAgents < 0)
                {
                    childAgents = 0;
                }
                m_SimStatsData[(int)SimStatIndex.Agents].StatValue = rootAgents;
                m_SimStatsData[(int)SimStatIndex.ChildAgents].StatValue = childAgents;
                m_SimStatsData[(int)SimStatIndex.TotalPrim].StatValue = scene.Primitives.Count;
                m_SimStatsData[(int)SimStatIndex.ActivePrim].StatValue = scene.ActiveObjects;
                int activeScripts = scene.ActiveScripts;
                m_SimStatsData[(int)SimStatIndex.ActiveScripts].StatValue = scene.ScriptedObjects;
                m_SimStatsData[(int)SimStatIndex.SimFPS].StatValue = scene.Environment.EnvironmentFps;
                m_SimStatsData[(int)SimStatIndex.ScriptEventsPerSeconds].StatValue = scene.ScriptThreadPool.ScriptEventsPerSec;
                m_SimStatsData[(int)SimStatIndex.PercentExecutingScripts].StatValue = 100.0 * scene.ScriptThreadPool.ExecutingScripts / activeScripts;

                var physics = scene.PhysicsScene;
                if (physics != null)
                {
                    m_SimStatsData[(int)SimStatIndex.PhysicsFPS].StatValue = physics.PhysicsFPS;
                    m_SimStatsData[(int)SimStatIndex.PhysicsTimeMs].StatValue = physics.PhysicsExecutionTime * 1000f;
                }
            }

            var stats = new SimStats();
            if (scene != null)
            {
                stats.RegionX = scene.GridPosition.X;
                stats.RegionY = scene.GridPosition.Y;
                RegionOptionFlags regionFlags = scene.RegionSettings.AsFlags;
                stats.RegionFlags = (uint)regionFlags;
                stats.RegionFlagsExtended.Add((ulong)regionFlags);
            }
            stats.ObjectCapacity = 15000;
            stats.PID = 0;
            stats.Stat = m_SimStatsData;
            SendMessage(stats);
            CheckExperienceTimeouts();
        }
    }
}
