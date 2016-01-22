// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            RegionOptionFlags.ExternallyVisible;
        public UUI Owner = UUI.Unknown;
        public int PricePerMeter = 1;
        public double BillableFactor = 1;
        public double SunPosition;
        public string AbuseEmail = string.Empty;
        public bool UseGlobalTime = true;
        public UUID CovenantID = UUID.Zero;
        public Date CovenantTimestamp = new Date();

        public EstateInfo()
        {

        }

        public EstateInfo(EstateInfo src)
        {
            ID = src.ID;
            ParentEstateID = src.ParentEstateID;
            Name = src.Name;
            Flags = src.Flags;
            Owner = new UUI(src.Owner);
            PricePerMeter = src.PricePerMeter;
            BillableFactor = src.BillableFactor;
            SunPosition = src.SunPosition;
            AbuseEmail = src.AbuseEmail;
            UseGlobalTime = src.UseGlobalTime;
            CovenantID = src.CovenantID;
            CovenantTimestamp = new Date(src.CovenantTimestamp);
        }
    }
}
