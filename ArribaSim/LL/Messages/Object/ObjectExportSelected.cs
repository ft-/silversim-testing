using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.LL.Messages.Object
{
    public class ObjectExportSelected : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public Int16 VolumeDetail = 0;

        public List<UUID> ObjectIDs = new List<UUID>();

        public ObjectExportSelected()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ObjectExportSelected;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ObjectExportSelected m = new ObjectExportSelected();
            m.AgentID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.VolumeDetail = p.ReadInt16();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectIDs.Add(p.ReadUUID());
            }
            return m;
        }
    }
}
