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
