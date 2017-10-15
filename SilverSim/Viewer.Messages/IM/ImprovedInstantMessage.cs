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
using SilverSim.Types.IM;
using System;

namespace SilverSim.Viewer.Messages.IM
{
    [UDPMessage(MessageType.ImprovedInstantMessage)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ImprovedInstantMessage : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public bool FromGroup;
        public UUID ToAgentID;
        public UInt32 ParentEstateID;
        public UUID RegionID;
        public Vector3 Position;
        public bool IsOffline;
        public GridInstantMessageDialog Dialog;
        public UUID ID;
        public Date Timestamp;
        public string FromAgentName = string.Empty;
        public string Message = string.Empty;
        public byte[] BinaryBucket = new byte[0];

        public ImprovedInstantMessage()
        {

        }

        public ImprovedInstantMessage(GridInstantMessage gim)
        {
            AgentID = gim.IsFromGroup ? 
                gim.FromGroup.ID : 
                gim.FromAgent.ID;
            SessionID = UUID.Zero;
            FromAgentName = gim.FromAgent.FullName;
            ToAgentID = gim.ToAgent.ID;
            Dialog = gim.Dialog;
            FromGroup = gim.IsFromGroup;
            Message = gim.Message;
            ID = (gim.IMSessionID.Equals(UUID.Zero)) ?
                (gim.FromAgent.ID ^ gim.ToAgent.ID) :
                gim.IMSessionID;
            IsOffline = gim.IsOffline;
            Position = gim.Position;
            if (gim.BinaryBucket != null)
            {
                BinaryBucket = gim.BinaryBucket;
            }
            ParentEstateID = (uint)gim.ParentEstateID;
            RegionID = gim.RegionID;
            Timestamp = gim.Timestamp ?? new Date();
        }

        public static explicit operator GridInstantMessage(ImprovedInstantMessage m) => new GridInstantMessage
        {
            FromAgent = new UUI { ID = m.AgentID, FullName = m.FromAgentName },
            FromGroup = new UGI(m.AgentID),
            IsFromGroup = m.FromGroup,
            ToAgent = new UUI(m.ToAgentID),
            ParentEstateID = m.ParentEstateID,
            RegionID = m.RegionID,
            Position = m.Position,
            IsOffline = m.IsOffline,
            Dialog = m.Dialog,
            IMSessionID = m.ID,
            Timestamp = m.Timestamp,
            Message = m.Message,
            BinaryBucket = m.BinaryBucket
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteBoolean(FromGroup);
            p.WriteUUID(ToAgentID);
            p.WriteUInt32(ParentEstateID);
            p.WriteUUID(RegionID);
            p.WriteVector3f(Position);
            p.WriteBoolean(IsOffline);
            p.WriteUInt8((byte)Dialog);
            p.WriteUUID(ID);
            p.WriteUInt32((uint)Timestamp.DateTimeToUnixTime());
            p.WriteStringLen8(FromAgentName);
            p.WriteStringLen16(Message);
            p.WriteUInt16((ushort)BinaryBucket.Length);
            p.WriteBytes(BinaryBucket);
        }

        public static ImprovedInstantMessage Decode(UDPPacket p) => new ImprovedInstantMessage
        {
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),
            FromGroup = p.ReadBoolean(),
            ToAgentID = p.ReadUUID(),
            ParentEstateID = p.ReadUInt32(),
            RegionID = p.ReadUUID(),
            Position = p.ReadVector3f(),
            IsOffline = p.ReadBoolean(),
            Dialog = (GridInstantMessageDialog)p.ReadUInt8(),
            ID = p.ReadUUID(),
            Timestamp = Date.UnixTimeToDateTime(p.ReadUInt32()),
            FromAgentName = p.ReadStringLen8(),
            Message = p.ReadStringLen16(),
            BinaryBucket = p.ReadBytes((int)(uint)p.ReadUInt16())
        };
    }
}
