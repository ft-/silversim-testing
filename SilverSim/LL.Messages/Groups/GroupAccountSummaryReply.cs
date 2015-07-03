/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
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
        public int IntervalDays = 0;
        public int CurrentInterval = 0;
        public string StartDate = string.Empty;
        public int Balance = 0;
        public int TotalCredits = 0;
        public int TotalDebits = 0;
        public int ObjectTaxCurrent = 0;
        public int LightTaxCurrent = 0;
        public int LandTaxCurrent = 0;
        public int GroupTaxCurrent = 0;
        public int ParcelDirFeeCurrent = 0;
        public int ObjectTaxEstimate = 0;
        public int LightTaxEstimate = 0;
        public int LandTaxEstimate = 0;
        public int GroupTaxEstimate = 0;
        public int ParcelDirFeeEstimate = 0;
        public int NonExemptMembers = 0;
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
