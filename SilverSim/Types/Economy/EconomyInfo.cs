// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Economy
{
    public class EconomyInfo
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

        public EconomyInfo()
        {

        }
    }
}
