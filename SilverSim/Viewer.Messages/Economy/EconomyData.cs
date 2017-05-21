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

        public static Message Decode(UDPPacket p) => new EconomyData()
        {
            ObjectCapacity = p.ReadInt32(),
            ObjectCount = p.ReadInt32(),
            PriceEnergyUnit = p.ReadInt32(),
            PriceObjectClaim = p.ReadInt32(),
            PricePublicObjectDecay = p.ReadInt32(),
            PricePublicObjectDelete = p.ReadInt32(),
            PriceParcelClaim = p.ReadInt32(),
            PriceParcelClaimFactor = p.ReadFloat(),
            PriceUpload = p.ReadInt32(),
            PriceRentLight = p.ReadInt32(),
            TeleportMinPrice = p.ReadInt32(),
            TeleportPriceExponent = p.ReadFloat(),
            EnergyEfficiency = p.ReadFloat(),
            PriceObjectRent = p.ReadFloat(),
            PriceObjectScaleFactor = p.ReadFloat(),
            PriceParcelRent = p.ReadInt32(),
            PriceGroupCreate = p.ReadInt32()
        };
    }
}
