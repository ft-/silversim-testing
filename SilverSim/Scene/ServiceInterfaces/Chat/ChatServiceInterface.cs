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

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.ServiceInterfaces.Chat
{
    public abstract class ChatServiceInterface
    {
        public abstract class Listener : IDisposable
        {
            public abstract void Remove();

            public Listener()
            {

            }

            public void Dispose()
            {
                Remove();
            }

            public abstract int Channel
            {
                get;
            }

            public abstract GetPositionDelegate GetPosition
            {
                get;
            }

            public abstract GetUUIDDelegate GetUUID
            {
                get;
            }

            public virtual bool IsIgnorePosition
            {
                get
                {
                    return false;
                }
            }

            public abstract void Send(ListenEvent ev);

            public abstract bool IsActive { get; set; }
        }

        #region Constructor
        public ChatServiceInterface()
        {

        }
        #endregion

        public abstract void Send(ListenEvent ev);

        public delegate Vector3 GetPositionDelegate();
        public delegate UUID GetUUIDDelegate();

        public abstract Listener AddListen(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> action);

        public const Int32 ListenRegexName = 1;
        public const Int32 ListenRegexMessage = 2;

        public abstract Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> action);

        public abstract Listener AddRegionListener(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, Action<ListenEvent> send);
    }
}
