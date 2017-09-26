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

        public static Message Decode(UDPPacket p)
        {
            var m = new TeleportFinish()
            {
                AgentID = p.ReadUUID(),
                LocationID = p.ReadUInt32(),
                SimIP = new IPAddress(p.ReadBytes(4)),
                SimPort = p.ReadUInt16()
            };
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
            var m = new Types.Map
            {
                { "AgentID", AgentID },
                { "LocationID", (int)LocationID },
                { "RegionHandle", new BinaryData(GridPosition.AsBytes) },
                { "SeedCapability", SeedCapability },
                { "SimAccess", (byte)SimAccess },
                { "SimIP", new BinaryData(SimIP.GetAddressBytes()) },
                { "SimPort", SimPort }
            };
            var array = new AnArray
            {
                m
            };
            var om = new Types.Map
            {
                ["Info"] = array
            };
            byte[] b = BitConverter.GetBytes((ulong)TeleportFlags);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            m.Add("TeleportFlags", new BinaryData(b));

            b = BitConverter.GetBytes(RegionSize.X);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            m.Add("RegionSizeX", new BinaryData(b));

            b = BitConverter.GetBytes(RegionSize.Y);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            m.Add("RegionSizeY", new BinaryData(b));
            return om;
        }
    }
}
