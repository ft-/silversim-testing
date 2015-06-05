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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.LSL
{
    [Flags]
    public enum APIFlags
    {
        None = 0,
        LSL = 1 << 0,
        LightShare = 1 << 1,
        OSSL = 1 << 2,
        ASSL = 1 << 3,
        ASSL_Admin = 1 << 4,
        WindLight_Aurora = 1 << 5,
        WindLight_New = 1 << 6
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, Inherited = false)]
    public class APILevel : Attribute
    {
        public readonly APIFlags Flags;
        public APILevel(APIFlags flags)
        {
            Flags = flags;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ForcedSleep : Attribute
    {
        public readonly double Seconds;
        public ForcedSleep(double seconds)
        {
            Seconds = seconds;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ExecutedOnStateChange : Attribute
    {
        public ExecutedOnStateChange()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ExecutedOnScriptReset : Attribute
    {
        public ExecutedOnScriptReset()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class LSLImplementation : Attribute
    {
        public LSLImplementation()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate, Inherited = false)]
    public class StateEventDelegate : Attribute
    {
        public StateEventDelegate()
        {

        }
    }
}
