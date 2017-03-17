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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8(Action);
            p.WriteUInt8(Size);
            p.WriteFloat((float)Seconds);
            p.WriteFloat((float)Height);

            p.WriteUInt8((byte)ParcelData.Count);
            foreach(Data d in ParcelData)
            {
                p.WriteInt32(d.LocalID);
                p.WriteFloat((float)d.West);
                p.WriteFloat((float)d.South);
                p.WriteFloat((float)d.East);
                p.WriteFloat((float)d.North);
            }

            p.WriteUInt8((byte)ParcelData.Count);
            foreach(Data d in ParcelData)
            {
                p.WriteFloat((float)d.BrushSize);
            }
        }
    }
}
