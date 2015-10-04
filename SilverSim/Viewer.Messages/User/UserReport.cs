// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.User
{
    [UDPMessage(MessageType.UserReport)]
    [Reliable]
    [NotTrusted]
    public class UserReport : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public byte ReportType;
        public byte Category;
        public Vector3 Position;
        public byte CheckFlags;
        public UUID ScreenshotID;
        public UUID ObjectID;
        public UUID AbuserID;
        public string AbuseRegionName;
        public UUID AbuseRegionID;
        public string Summary;
        public string Details;
        public string VersionString;

        public UserReport()
        {

        }

        public static UserReport Decode(UDPPacket p)
        {
            UserReport m = new UserReport();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ReportType = p.ReadUInt8();
            m.Category = p.ReadUInt8();
            m.Position = p.ReadVector3f();
            m.CheckFlags = p.ReadUInt8();
            m.ScreenshotID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.AbuserID = p.ReadUUID();
            m.AbuseRegionName = p.ReadStringLen8();
            m.AbuseRegionID = p.ReadUUID();
            m.Summary = p.ReadStringLen8();
            m.Details = p.ReadStringLen16();
            m.VersionString = p.ReadStringLen8();
            return m;
        }
    }
}
