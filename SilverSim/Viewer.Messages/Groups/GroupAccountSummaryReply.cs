// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            p.WriteMessageType(Number);
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
    }
}
