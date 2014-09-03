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

using System;

namespace SilverSim.LL.Messages.Economy
{
    public class EconomyData : Message
    {
        public Int32 ObjectCapacity = 0;
        public Int32 ObjectCount = 0;
        public Int32 PriceEnergyUnit = 0;
        public Int32 PriceObjectClaim = 0;
        public Int32 PricePublicObjectDecay = 0;
        public Int32 PricePublicObjectDelete = 0;
        public Int32 PriceParcelClaim = 0;
        public double PriceParcelClaimFactor = 1;
        public Int32 PriceUpload = 0;
        public Int32 PriceRentLight = 0;
        public Int32 TeleportMinPrice = 0;
        public double TeleportPriceExponent = 0;
        public double EnergyEfficiency = 0;
        public double PriceObjectRent = 0;
        public double PriceObjectScaleFactor = 1;
        public Int32 PriceParcelRent = 0;
        public Int32 PriceGroupCreate = 0;

        public EconomyData()
        {

        }

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override MessageType Number
        {
            get
            {
                return MessageType.EconomyData;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteInt32(ObjectCapacity);
            p.WriteInt32(ObjectCount);
            p.WriteInt32(PriceEnergyUnit);
            p.WriteInt32(PriceObjectClaim);
            p.WriteInt32(PricePublicObjectDecay);
            p.WriteInt32(PricePublicObjectDelete);
            p.WriteInt32(PriceParcelClaim);
            p.WriteFloat((float)PriceParcelClaimFactor);
            p.WriteInt32(PriceUpload);
            p.WriteInt32(PriceRentLight);
            p.WriteInt32(TeleportMinPrice);
            p.WriteFloat((float)TeleportPriceExponent);
            p.WriteFloat((float)EnergyEfficiency);
            p.WriteFloat((float)PriceObjectRent);
            p.WriteFloat((float)PriceObjectScaleFactor);
            p.WriteInt32(PriceParcelRent);
            p.WriteInt32(PriceGroupCreate);
        }
    }
}
