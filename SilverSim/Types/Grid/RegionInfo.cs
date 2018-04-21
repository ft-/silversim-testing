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

using SilverSim.Threading;
using System;
using System.Net;

namespace SilverSim.Types.Grid
{
    public class RegionInfo
    {
        #region Constructor
        public RegionInfo()
        {
        }

        public RegionInfo(RegionInfo src)
        {
            ID = src.ID;
            Location = src.Location;
            Size = src.Size;
            Name = src.Name;
            ServerIP = src.ServerIP;
            ServerHttpPort = src.ServerHttpPort;
            ServerURI = src.ServerURI;
            ServerPort = src.ServerPort;
            RegionMapTexture = src.RegionMapTexture;
            ParcelMapTexture = src.ParcelMapTexture;
            Access = src.Access;
            RegionSecret = src.RegionSecret;
            Owner = new UGUI(src.Owner);
            Flags = src.Flags;
            ProductName = src.ProductName;
            ProtocolVariant = src.ProtocolVariant;
            GridURI = src.GridURI;
            AuthenticatingPrincipal = new UGUI(src.AuthenticatingPrincipal);
            AuthenticatingToken = src.AuthenticatingToken;
            ScopeID = src.ScopeID;
        }
        #endregion

        #region Region Information
        public UUID ID = UUID.Zero;
        public GridVector Location = GridVector.Zero;
        public GridVector Size = GridVector.Zero;
        public string Name = string.Empty;
        public string ServerIP = string.Empty;
        public uint ServerHttpPort;
        public string ServerURI = string.Empty;
        public uint ServerPort;
        public UUID RegionMapTexture = UUID.Zero;
        public UUID ParcelMapTexture = UUID.Zero;
        public RegionAccess Access;
        public string RegionSecret = string.Empty;
        public UGUI Owner = UGUI.Unknown;
        public RegionFlags Flags;
        public string ProductName = string.Empty; /* e.g. "Mainland" */
        public string ProtocolVariant = string.Empty; /* see ProtocolVariantId */
        public string GridURI = string.Empty; /* empty when addressing local grid */

        public EndPoint SimIP
        {
            get
            {
                if (m_SimIP == null)
                {
                    var addresses = DnsNameCache.GetHostAddresses(ServerIP, true);
                    if (addresses.Length == 0)
                    {
                        throw new InvalidOperationException();
                    }
                    m_SimIP = new IPEndPoint(addresses[0], (int)ServerPort);
                }
                return m_SimIP;
            }

            set { m_SimIP = value; }
        }

        protected EndPoint m_SimIP;

        public static class ProtocolVariantId
        {
            public static readonly string Local = string.Empty;
            public static readonly string OpenSim = "OpenSim";
        }
        #endregion

        #region Authentication Info
        public UGUI AuthenticatingPrincipal = UGUI.Unknown;
        public string AuthenticatingToken = string.Empty;
        #endregion

        #region Informational Fields
        public UUID ScopeID = UUID.Zero;
        #endregion
    }
}
