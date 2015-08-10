// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Net;

namespace SilverSim.Types.Grid
{
    public class DestinationInfo : RegionInfo
    {
        #region Constructor
        public DestinationInfo()
        {

        }
        public DestinationInfo(RegionInfo ri)
        {
            ID = ri.ID;
            Location = ri.Location;
            Size = ri.Size;
            Name = ri.Name;
            ServerIP = ri.ServerIP;
            ServerHttpPort = ri.ServerHttpPort;
            ServerURI = ri.ServerURI;
            ServerPort = ri.ServerPort;
            RegionMapTexture = ri.RegionMapTexture;
            ParcelMapTexture = ri.ParcelMapTexture;
            Access = ri.Access;
            RegionSecret = ri.RegionSecret;
            Owner = new UUI(ri.Owner);
            Flags = ri.Flags;
            ScopeID = ri.ScopeID;
        }
        #endregion

        #region Fields
        public string GatekeeperURI = string.Empty;
        public EndPoint SimIP;
        public Vector3 Position = Vector3.Zero;
        public Vector3 LookAt = Vector3.Zero;
        public TeleportFlags TeleportFlags = TeleportFlags.None;
        public string StartLocation = string.Empty;
        public bool LocalToGrid = false;
        #endregion
    }
}
