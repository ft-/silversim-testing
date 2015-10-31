// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Account
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct SessionInfo
    {
        public UUID SessionID;
        public UUID SecureSessionID;
        public string ServiceSessionID;
    }
}
