// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.Chat
{
    public abstract class ChatServiceInterface
    {
        public abstract class Listener
        {
            public abstract void Remove();

            public abstract int Channel { get; }

            public abstract Func<Vector3> GetPosition { get; }

            public abstract Func<UUID> GetUUID { get; }

            public abstract Func<UUID> GetOwner { get; }

            public virtual bool IsIgnorePosition => false;

            public abstract void Send(ListenEvent ev);

            public abstract bool IsActive { get; set; }

            public abstract bool IsAgent { get; }

            public abstract void Serialize(List<object> res, int handle);

            public abstract bool IsMatching(string name, UUID id, string message, Int32 regexBitfield);
        }

        public abstract void Send(ListenEvent ev);

        public Listener AddListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> action) =>
            AddListen(channel, name, id, message, getuuid, getpos, null, action);

        public abstract Listener AddListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Func<UUID> getowner, Action<ListenEvent> action);

        public abstract Listener AddAgentListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> send);

        public const Int32 ListenRegexName = 1;
        public const Int32 ListenRegexMessage = 2;

        public Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> action) =>
            AddListenRegex(channel, name, id, message, regexBitfield, getuuid, getpos, null, action);

        public abstract Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, Func<UUID> getuuid, Func<Vector3> getpos, Func<UUID> getowner, Action<ListenEvent> action);

        public Listener AddRegionListener(int channel, string name, UUID id, string message, Func<UUID> getuuid, Action<ListenEvent> send) =>
            AddRegionListener(channel, name, id, message, getuuid, null, send);

        public abstract Listener AddRegionListener(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<UUID> getowner, Action<ListenEvent> send);

        /* only to be used for SimCircuit */
        public abstract Listener AddChatPassListener(Action<ListenEvent> send);
    }
}
