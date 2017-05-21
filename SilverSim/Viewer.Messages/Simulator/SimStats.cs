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
        public List<UInt64> RegionFlagsExtended = new List<UInt64>();

        public override void Serialize(UDPPacket p)
        {
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
            p.WriteUInt8((byte)RegionFlagsExtended.Count);
            for(int i = 0; i < RegionFlagsExtended.Count; ++i)
            {
                p.WriteUInt64(RegionFlagsExtended[i]);
            }
        }
    }
}
