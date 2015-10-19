// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.ServiceInterfaces.Chat
{
    public abstract class ChatServiceInterface
    {
        public abstract class Listener
        {
            public abstract void Remove();

            public Listener()
            {

            }

            public abstract int Channel
            {
                get;
            }

            public abstract Func<Vector3> GetPosition
            {
                get;
            }

            public abstract Func<UUID> GetUUID
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

        public abstract Listener AddListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> action);

        public abstract Listener AddAgentListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> send);

        public const Int32 ListenRegexName = 1;
        public const Int32 ListenRegexMessage = 2;

        public abstract Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> action);

        public abstract Listener AddRegionListener(int channel, string name, UUID id, string message, Func<UUID> getuuid, Action<ListenEvent> send);

        /* only to be used for SimCircuit */
        public abstract Listener AddChatPassListener(Action<ListenEvent> send);
    }
}
