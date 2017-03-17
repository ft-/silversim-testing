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

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountSummaryReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupAccountSummaryReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public UUID RequestID = UUID.Zero;
        public int IntervalDays;
        public int CurrentInterval;
        public string StartDate = string.Empty;
        public int Balance;
        public int TotalCredits;
        public int TotalDebits;
        public int ObjectTaxCurrent;
        public int LightTaxCurrent;
        public int LandTaxCurrent;
        public int GroupTaxCurrent;
        public int ParcelDirFeeCurrent;
        public int ObjectTaxEstimate;
        public int LightTaxEstimate;
        public int LandTaxEstimate;
        public int GroupTaxEstimate;
        public int ParcelDirFeeEstimate;
        public int NonExemptMembers;
        public string LastTaxDate = string.Empty;
        public string TaxDate = string.Empty;

        public GroupAccountSummaryReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteInt32(IntervalDays);
            p.WriteInt32(CurrentInterval);
            p.WriteStringLen8(StartDate);
            p.WriteInt32(Balance);
            p.WriteInt32(TotalCredits);
            p.WriteInt32(TotalDebits);
            p.WriteInt32(ObjectTaxCurrent);
            p.WriteInt32(LightTaxCurrent);
            p.WriteInt32(LandTaxCurrent);
            p.WriteInt32(GroupTaxCurrent);
            p.WriteInt32(ParcelDirFeeCurrent);
            p.WriteInt32(ObjectTaxEstimate);
            p.WriteInt32(LightTaxEstimate);
            p.WriteInt32(LandTaxEstimate);
            p.WriteInt32(GroupTaxEstimate);
            p.WriteInt32(ParcelDirFeeEstimate);
            p.WriteInt32(NonExemptMembers);
            p.WriteStringLen8(LastTaxDate);
            p.WriteStringLen8(TaxDate);
        }

        public static Message Decode(UDPPacket p)
        {
            GroupAccountSummaryReply m = new GroupAccountSummaryReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.IntervalDays = p.ReadInt32();
            m.CurrentInterval = p.ReadInt32();
            m.StartDate = p.ReadStringLen8();
            m.Balance = p.ReadInt32();
            m.TotalCredits = p.ReadInt32();
            m.TotalDebits = p.ReadInt32();
            m.ObjectTaxCurrent = p.ReadInt32();
            m.LightTaxCurrent = p.ReadInt32();
            m.LandTaxCurrent = p.ReadInt32();
            m.GroupTaxCurrent = p.ReadInt32();
            m.ParcelDirFeeCurrent = p.ReadInt32();
            m.ObjectTaxEstimate = p.ReadInt32();
            m.LightTaxEstimate = p.ReadInt32();
            m.GroupTaxEstimate = p.ReadInt32();
            m.ParcelDirFeeEstimate = p.ReadInt32();
            m.NonExemptMembers = p.ReadInt32();
            m.LastTaxDate = p.ReadStringLen8();
            m.TaxDate = p.ReadStringLen8();
            return m;
        }
    }
}
