// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Grid
{
    public class RegionInfo
    {
        #region Constructor
        public RegionInfo()
        {

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
        public UUI Owner = UUI.Unknown;
        public RegionFlags Flags;
        public string ProtocolVariant = ProtocolVariantId.Local;
        public string GridURI = string.Empty; /* empty when addressing local grid */
        public static class ProtocolVariantId
        {
            public const string Local = "";
            public const string OpenSim = "OpenSim";
        }
        #endregion

        #region Authentication Info
        public UUID AuthenticatingPrincipalID = UUID.Zero;
        public string AuthenticatingToken = string.Empty;
        #endregion

        #region Informational Fields
        public UUID ScopeID = UUID.Zero;
        #endregion
    }
}
