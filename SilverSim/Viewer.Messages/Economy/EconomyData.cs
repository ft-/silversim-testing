// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.Economy
{
    [UDPMessage(MessageType.EconomyData)]
    [Reliable]
    [Trusted]
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
