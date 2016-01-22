// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;
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
            Owner = new UUI(src.Owner);
            Flags = src.Flags;
            ProductName = src.ProductName;
            ProtocolVariant = src.ProtocolVariant;
            GridURI = src.GridURI;
            AuthenticatingPrincipal = new UUI(src.AuthenticatingPrincipal);
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
        public UUI Owner = UUI.Unknown;
        public RegionFlags Flags;
        public string ProductName = string.Empty; /* e.g. "Mainland" */
        public string ProtocolVariant = string.Empty; /* see ProtocolVariantId */
        public string GridURI = string.Empty; /* empty when addressing local grid */
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public static class ProtocolVariantId
        {
            [SuppressMessage("Gendarme.Rules.Performance", "PreferLiteralOverInitOnlyFieldsRule")]
            public static readonly string Local = string.Empty;
            [SuppressMessage("Gendarme.Rules.Performance", "PreferLiteralOverInitOnlyFieldsRule")]
            public static readonly string OpenSim = "OpenSim";
        }
        #endregion

        #region Authentication Info
        public UUI AuthenticatingPrincipal = UUI.Unknown;
        public string AuthenticatingToken = string.Empty;
        #endregion

        #region Informational Fields
        public UUID ScopeID = UUID.Zero;
        #endregion
    }
}
