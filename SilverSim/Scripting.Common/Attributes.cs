// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Common
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class CompilerUsesRunAndCollectMode : Attribute
    {
        public CompilerUsesRunAndCollectMode()
        {

        }
    }
}
