// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Land
{
    [UDPMessage(MessageType.ModifyLand)]
    [Reliable]
    [NotTrusted]
    public class ModifyLand : Message
    {
        public struct Data
        {
            public Int32 LocalID;
            public double West;
            public double South;
            public double East;
            public double North;

            public double BrushSize;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        
        public byte Action;
        public byte Size;
        public double Seconds;
        public double Height;

        public List<Data> ParcelData = new List<Data>();

        public ModifyLand()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ModifyLand m = new ModifyLand();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Action = p.ReadUInt8();
            m.Size = p.ReadUInt8();
            double defBrushSize = (1 << (m.Size));
            m.Seconds = p.ReadFloat();
            m.Height = p.ReadFloat();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.LocalID = p.ReadInt32();
                d.West = p.ReadFloat();
                d.South = p.ReadFloat();
                d.East = p.ReadFloat();
                d.North = p.ReadFloat();
                d.BrushSize = defBrushSize;
                m.ParcelData.Add(d);
            }

            c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = m.ParcelData[(int)i];
                d.BrushSize = p.ReadFloat();
                m.ParcelData[(int)i] = d;
            }
            return m;
        }
    }
}
