// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Estate
{
    public class EstateInfo
    {
        public uint ID;
        public uint ParentEstateID = 1;
        public string Name = "My Estate";
        public RegionOptionFlags Flags = RegionOptionFlags.AllowDirectTeleport | 
            RegionOptionFlags.AllowLandmark |
            RegionOptionFlags.AllowSetHome | 
            RegionOptionFlags.AllowVoice | 
            RegionOptionFlags.PublicAllowed;
        public UUI Owner = UUI.Unknown;
        public int PricePerMeter;
        public double BillableFactor = 1;
        public double SunPosition = 1;
        public string AbuseEmail = string.Empty;
        public bool UseGlobalTime = true;
        public UUID CovenantID = UUID.Zero;
        public Date CovenantTimestamp = new Date();

        public EstateInfo()
        {

        }
    }
}
