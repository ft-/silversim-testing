// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

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
