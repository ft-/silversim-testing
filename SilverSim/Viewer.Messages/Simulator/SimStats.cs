// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Simulator
{
    [UDPMessage(MessageType.SimStats)]
    [Trusted]
    public class SimStats : Message
    {
        public struct Data
        {
            public enum StatType : uint
            {
                TimeDilation = 0,
                SimFPS = 1,
                PhysicsFPS = 2,
                AgentUpdates = 3,
                FrameTimeMs = 4,
                NetTimeMs = 5,
                OtherTimeMs = 6,
                PhysicsTimeMs = 7,
                AgentTimeMs = 8,
                ImageTimeMs = 9,
                ScriptMS = 10,
                TotalPrim = 11,
                ActivePrim = 12,
                Agents = 13,
                ChildAgents = 14,
                ActiveScripts = 15,
                ScriptLinesPerSecond = 16,
                InPacketsPerSecond = 17,
                OutPacketsPerSecond = 18,
                PendingDownloads = 19,
                PendingUploads = 20,
                VirtualSizeKb = 21,
                ResidentSizeKb = 22,
                PendingLocalUploads = 23,
                UnAckedBytes = 24,
                PhysicsPinnedTasks = 25,
                PhysicsLodTasks = 26,
                SimPhysicsStepMs = 27,
                SimPhysicsShapeMs = 28,
                SimPhysicsOtherMs = 29,
                SimPhysicsMemory = 30,
                ScriptEps = 31,
                SimSpareTimeMs = 32,
                SimSleepTimeMs = 33,
                SimIoPumpTimeMs = 34,
                PercentScriptsRun = 35,
            }

            public StatType StatID;
            public double StatValue;

            public Data(StatType type)
            {
                StatID = type;
                StatValue = 0;
            }
            public Data(StatType type, double val)
            {
                StatID = type;
                StatValue = val;
            }
        }

        public UInt32 RegionX;
        public UInt32 RegionY;
        public UInt32 RegionFlags;
        public UInt32 ObjectCapacity;

        public Data[] Stat = new Data[0];
        public Int32 PID;
        public UInt64[] RegionFlagsExtended = new UInt64[0];

        public SimStats()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt32(RegionX);
            p.WriteUInt32(RegionY);
            p.WriteUInt32(RegionFlags);
            p.WriteUInt32(ObjectCapacity);

            p.WriteUInt8((byte)Stat.Length);
            for (int i = 0; i < Stat.Length; ++i)
            {
                p.WriteUInt32((uint)Stat[i].StatID);
                p.WriteFloat((float)Stat[i].StatValue);
            }
            p.WriteInt32(PID);
            p.WriteUInt8((byte)RegionFlagsExtended.Length);
            for(int i = 0; i < RegionFlagsExtended.Length; ++i)
            {
                p.WriteUInt64(RegionFlagsExtended[i]);
            }
        }
    }
}
