// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "FlagsShouldNotDefineAZeroValueRule")]
    public enum XmlSerializationOptions
    {
        None = 0,
        WriteOwnerInfo = 1,
        AdjustForNextOwner = 2,
        WriteXml2 = 4
    }
}
