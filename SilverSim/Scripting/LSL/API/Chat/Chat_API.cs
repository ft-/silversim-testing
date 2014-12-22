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
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL.API.Chat
{
    [LSLImplementation]
    [ScriptApiName("Chat")]
    public partial class Chat_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int PUBLIC_CHANNEL = 0;
        [APILevel(APIFlags.LSL)]
        public const int DEBUG_CHANNEL = 0x7FFFFFFF;

        private UUID getOwner(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.ObjectGroup.Owner.ID;
            }
        }

        private void sendChat(ScriptInstance Instance, ListenEvent ev)
        {
            lock (Instance)
            {
                ev.ID = Instance.Part.ObjectGroup.ID;
                ev.Name = Instance.Part.ObjectGroup.Name;
                Instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>().Send(ev);
            }
        }

        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_NAME = 0x1;
        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_MESSAGE = 0x2;

        public Chat_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
