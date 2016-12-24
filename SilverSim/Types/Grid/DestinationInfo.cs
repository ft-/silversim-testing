// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using System.Net;
using System;

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

        public void UpdateFromRegion(RegionInfo ri)
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
        public Vector3 Position = Vector3.Zero;
        public Vector3 LookAt = Vector3.Zero;
        public TeleportFlags TeleportFlags;
        public string StartLocation = string.Empty;
        public bool LocalToGrid;
        #endregion

        public EndPoint SimIP
        {
            get
            {
                if(null == m_SimIP)
                {
                    IPAddress[] addresses = DnsNameCache.GetHostAddresses(ServerIP, true);
                    if(addresses.Length == 0)
                    {
                        throw new InvalidOperationException();
                    }
                    m_SimIP = new IPEndPoint(addresses[0], (int)ServerPort);
                }
                return m_SimIP;
            }
            set
            {
                m_SimIP = value;
            }
        }

        EndPoint m_SimIP;
    }
}
