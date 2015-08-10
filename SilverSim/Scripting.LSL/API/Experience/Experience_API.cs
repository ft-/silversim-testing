// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        public int llAgentInExperience(ScriptInstance Instance, LSLKey agent)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llCreateKeyValue(ScriptInstance Instance, string k, string v)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llDataSizeKeyValue(ScriptInstance Instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llDeleteKeyValue(ScriptInstance Instance, string k)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetExperienceDetails(ScriptInstance Instance, LSLKey experience_id)
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
        public LSLKey llKeyCountKeyValue(ScriptInstance Instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llKeysKeyValue(ScriptInstance Instance, int first, int count)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llReadKeyValue(ScriptInstance Instance, string k)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llRequestExperiencePermissions(ScriptInstance Instance, LSLKey agent, string name /* unused */)
        {

        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llUpdateKeyValue(ScriptInstance Instance, string k, string v, int checked_orig, string original_value)
        {
            throw new NotImplementedException();
        }
    }
}
