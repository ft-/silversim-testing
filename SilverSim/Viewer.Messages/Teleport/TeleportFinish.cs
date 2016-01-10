﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Net;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportFinish)]
    [Reliable]
    [EventQueueGet("TeleportFinish")]
    [Trusted]
    public class TeleportFinish : Message
    {
        public UUID AgentID;
        public UInt32 LocationID;
        public IPAddress SimIP;
        public UInt16 SimPort;
        public GridVector GridPosition;
        public string SeedCapability;
        public RegionAccess SimAccess;
        public TeleportFlags TeleportFlags;

        /* EQG extension */
        public GridVector RegionSize;

        public TeleportFinish()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32(LocationID);
            p.WriteBytes(SimIP.GetAddressBytes());
            p.WriteUInt16(SimPort);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteStringLen16(SeedCapability);
            p.WriteUInt8((byte)SimAccess);
            p.WriteUInt32((UInt32)TeleportFlags);
        }

        public override IValue SerializeEQG()
        {
            Types.Map m = new Types.Map();
            m.Add("AgentID", AgentID);
            m.Add("LocationID", LocationID);
            m.Add("RegionHandle", new BinaryData(GridPosition.AsBytes));
            m.Add("SeedCapability", SeedCapability);
            m.Add("SimAccess", (byte)SimAccess);
            m.Add("SimIP", new BinaryData(SimIP.GetAddressBytes()));
            m.Add("SimPort", SimPort);
            m.Add("TeleportFlags", (uint)TeleportFlags);
            m.Add("RegionSizeX", RegionSize.X);
            m.Add("RegionSizeY", RegionSize.Y);
            return m;
        }
    }
}
