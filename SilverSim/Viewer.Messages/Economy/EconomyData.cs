﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.Economy
{
    [UDPMessage(MessageType.EconomyData)]
    [Reliable]
    [Trusted]
    public class EconomyData : Message
    {
        public Int32 ObjectCapacity;
        public Int32 ObjectCount;
        public Int32 PriceEnergyUnit;
        public Int32 PriceObjectClaim;
        public Int32 PricePublicObjectDecay;
        public Int32 PricePublicObjectDelete;
        public Int32 PriceParcelClaim;
        public double PriceParcelClaimFactor = 1;
        public Int32 PriceUpload;
        public Int32 PriceRentLight;
        public Int32 TeleportMinPrice;
        public double TeleportPriceExponent;
        public double EnergyEfficiency;
        public double PriceObjectRent;
        public double PriceObjectScaleFactor = 1;
        public Int32 PriceParcelRent;
        public Int32 PriceGroupCreate;

        public EconomyData()
        {

        }

        public override void Serialize(UDPPacket p)
        {
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

        public static Message Decode(UDPPacket p)
        {
            EconomyData m = new EconomyData();
            m.ObjectCapacity = p.ReadInt32();
            m.ObjectCount = p.ReadInt32();
            m.PriceEnergyUnit = p.ReadInt32();
            m.PriceObjectClaim = p.ReadInt32();
            m.PricePublicObjectDecay = p.ReadInt32();
            m.PricePublicObjectDelete = p.ReadInt32();
            m.PriceParcelClaim = p.ReadInt32();
            m.PriceParcelClaimFactor = p.ReadFloat();
            m.PriceUpload = p.ReadInt32();
            m.PriceRentLight = p.ReadInt32();
            m.TeleportMinPrice = p.ReadInt32();
            m.TeleportPriceExponent = p.ReadFloat();
            m.EnergyEfficiency = p.ReadFloat();
            m.PriceObjectRent = p.ReadFloat();
            m.PriceObjectScaleFactor = p.ReadFloat();
            m.PriceParcelRent = p.ReadInt32();
            m.PriceGroupCreate = p.ReadInt32();

            return m;
        }
    }
}
