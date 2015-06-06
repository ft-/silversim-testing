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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Experience
{
    [ScriptApiName("Experience")]
    [LSLImplementation]
    public partial class Experience_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Experience_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_THROTTLED = 1;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCES_DISABLED = 2;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_INVALID_PARAMETERS = 3;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_PERMITTED = 4;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NO_EXPERIENCE = 5;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_FOUND = 6;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_INVALID_EXPERIENCE = 7;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCE_DISABLED = 8;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCE_SUSPENDED = 9;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_UNKNOWN_ERROR = 10;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_QUOTA_EXCEEDED = 11;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_STORE_DISABLED = 12;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_STORAGE_EXCEPTION = 13;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_KEY_NOT_FOUND = 14;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_RETRY_UPDATE = 15;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_MATURITY_EXCEEDED = 16;

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void experience_permissions(LSLKey agent_id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void experience_permissions_denied(LSLKey agent_id, int reason);

        [APILevel(APIFlags.LSL)]
        public int llAgentInExperience(ScriptInstance Instance, UUID agent)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public UUID llCreateKeyValue(ScriptInstance Instance, string k, string v)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public UUID llDataSizeKeyValue(ScriptInstance Instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public UUID llDeleteKeyValue(ScriptInstance Instance, string k)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetExperienceDetails(ScriptInstance Instance, UUID experience_id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public string llGetExperienceErrorMessage(ScriptInstance Instance, int error)
        {
            switch(error)
            {
                case XP_ERROR_NONE: return "no error";
                case XP_ERROR_THROTTLED: return "exceeded throttle";
                case XP_ERROR_EXPERIENCES_DISABLED: return "experiences are disabled";
                case XP_ERROR_INVALID_PARAMETERS: return "invalid parameters";
                case XP_ERROR_NOT_PERMITTED: return "operation not permitted";
                case XP_ERROR_NO_EXPERIENCE: return "script not associated with an experience";
                case XP_ERROR_NOT_FOUND: return "not found";
                case XP_ERROR_INVALID_EXPERIENCE: return "invalid experience";
                case XP_ERROR_EXPERIENCE_DISABLED: return "experience is disabled";
                case XP_ERROR_EXPERIENCE_SUSPENDED: return "experience is suspended";
                case XP_ERROR_UNKNOWN_ERROR: return "unknown error";
                case XP_ERROR_QUOTA_EXCEEDED: return "experience data quota exceeded";
                case XP_ERROR_STORE_DISABLED: return "key-value store is disabled";
                case XP_ERROR_STORAGE_EXCEPTION: return "key-value store communication failed";
                case XP_ERROR_KEY_NOT_FOUND: return "key doesn't exist";
                case XP_ERROR_RETRY_UPDATE: return "retry update";
                case XP_ERROR_MATURITY_EXCEEDED: return "experience content rating too high";
                default: return "unknown error";
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llKeyCountKeyValue(ScriptInstance Instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public UUID llKeysKeyValue(ScriptInstance Instance, int first, int count)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public UUID llReadKeyValue(ScriptInstance Instance, string k)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llRequestExperiencePermissions(ScriptInstance Instance, UUID agent, string name /* unused */)
        {

        }

        [APILevel(APIFlags.LSL)]
        public UUID llUpdateKeyValue(ScriptInstance Instance, string k, string v, int checked_orig, string original_value)
        {
            throw new NotImplementedException();
        }
    }
}
