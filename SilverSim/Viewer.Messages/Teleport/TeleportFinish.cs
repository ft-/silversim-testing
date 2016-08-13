// SilverSim is distributed under the terms of the
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

        public static Message Decode(UDPPacket p)
        {
            TeleportFinish m = new TeleportFinish();
            m.AgentID = p.ReadUUID();
            m.LocationID = p.ReadUInt32();
            m.SimIP = new IPAddress(p.ReadBytes(4));
            m.SimPort = p.ReadUInt16();
            m.GridPosition.RegionHandle = p.ReadUInt32();
            m.SeedCapability = p.ReadStringLen16();
            m.SimAccess = (RegionAccess)p.ReadUInt8();
            m.TeleportFlags = (TeleportFlags)p.ReadUInt32();
            return m;
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
            Types.Map om = new Types.Map();
            Types.Map m = new Types.Map();
            AnArray array = new AnArray();
            array.Add(m);
            om.Add("Info", array);
            m.Add("AgentID", AgentID);
            m.Add("LocationID", (int)LocationID);
            m.Add("RegionHandle", new BinaryData(GridPosition.AsBytes));
            m.Add("SeedCapability", SeedCapability);
            m.Add("SimAccess", (byte)SimAccess);
            m.Add("SimIP", new BinaryData(SimIP.GetAddressBytes()));
            m.Add("SimPort", SimPort);
            byte[] b = BitConverter.GetBytes((ulong)TeleportFlags);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            m.Add("TeleportFlags", new BinaryData(b));

            b = BitConverter.GetBytes(RegionSize.X);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            m.Add("RegionSizeX", new BinaryData(b));

            b = BitConverter.GetBytes(RegionSize.Y);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            m.Add("RegionSizeY", new BinaryData(b));
            return om;
        }
    }
}
