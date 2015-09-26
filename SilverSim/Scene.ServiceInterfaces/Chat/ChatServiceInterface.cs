// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

            public abstract bool IsAgent { get; }
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

        public abstract Listener AddAgentListen(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> send);

        public const Int32 ListenRegexName = 1;
        public const Int32 ListenRegexMessage = 2;

        public abstract Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> action);

        public abstract Listener AddRegionListener(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, Action<ListenEvent> send);

        /* only to be used for SimCircuit */
        public abstract Listener AddChatPassListener(Action<ListenEvent> send);
    }
}
