// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Chat
{
    public class RegionListenerInfo : ListenerInfo
    {
        public override bool IsIgnorePosition
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

        internal RegionListenerInfo(
            ChatHandler handler,
            int channel, 
            string name,
            UUID id, 
            string message,
            Func<UUID> getuuid, 
            Action<ListenEvent> send)
            : base(handler, channel, name, id, message, getuuid, GetPositionFunc, send, false)
        {

        }
    }
}
