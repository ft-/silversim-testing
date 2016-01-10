// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelDisableObjects)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelDisableObjects : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID;
        public UInt32 ReturnType;
        public List<UUID> TaskIDs = new List<UUID>();
        public List<UUID> OwnerIDs = new List<UUID>();

        public ParcelDisableObjects()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelDisableObjects m = new ParcelDisableObjects();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.ReturnType = p.ReadUInt32();

            uint cnt;
            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.TaskIDs.Add(p.ReadUUID());
            }

            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.OwnerIDs.Add(p.ReadUUID());
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteInt32(LocalID);
            p.WriteUInt32(ReturnType);

            p.WriteUInt8((byte)TaskIDs.Count);
            foreach(UUID id in TaskIDs)
            {
                p.WriteUUID(id);
            }
            p.WriteUInt8((byte)OwnerIDs.Count);
            foreach(UUID id in OwnerIDs)
            {
                p.WriteUUID(id);
            }
        }
    }
}
