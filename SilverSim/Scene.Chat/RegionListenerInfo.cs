// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Chat
{
    class RegionListenerInfo : ListenerInfo
    {
        public new bool IsIgnorePosition
        {
            get
            {
                return true;
            }
        }

        private static Vector3 GetPositionFunc()
        {
            return Vector3.Zero;
        }

        public RegionListenerInfo(
            ChatHandler handler,
            int channel, 
            string name,
            UUID id, 
            string message,
            ChatServiceInterface.GetUUIDDelegate getuuid, 
            Action<ListenEvent> send)
            : base(handler, channel, name, id, message, getuuid, GetPositionFunc, send, false)
        {

        }
    }
}
