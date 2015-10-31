// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Agent
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum AgentState : byte
    {
        Walking = 0,
        Mouselook = 1,
        Typing = 2
    }
}
