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

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Chat
{
    public class ListenerInfo : ChatServiceInterface.Listener
    {
        private readonly string m_Name;
        private readonly UUID m_ID;
        private readonly string m_Message;
        private readonly Action<ListenEvent> m_Send;
        public override bool IsActive { get; set; }
        public override bool IsAgent { get; }

        private readonly ChatHandler m_Handler;

        internal ListenerInfo(
            ChatHandler handler,
            int channel,
            string name,
            UUID id,
            string message,
            Func<UUID> getuuid,
            Func<Vector3> getpos,
            Func<UUID> getowner,
            Action<ListenEvent> send,
            bool isAgent)
        {
            IsAgent = isAgent;
            IsActive = true;
            m_Handler = handler;
            Channel = channel;
            m_Name = name;
            m_ID = id;
            m_Message = message;
            GetUUID = getuuid;
            GetPosition = getpos;
            GetOwner = getowner;
            m_Send = send;
        }

        public override bool IsMatching(string name, UUID id, string message, Int32 regexBitfield) => m_Name == name &&
                m_ID == id &&
                m_Message == message &&
                0 == regexBitfield;

        public override void Serialize(List<object> res, int handle)
        {
            res.Add(IsActive);
            res.Add(handle);
            res.Add(Channel);
            res.Add(m_Name);
            res.Add(m_ID);
            res.Add(m_Message);
            res.Add(0);
        }

        public override void Remove()
        {
            m_Handler.Remove(this);
        }

        public override int Channel { get; }

        public override Func<Vector3> GetPosition { get; }

        public override Func<UUID> GetUUID { get; }

        public override Func<UUID> GetOwner { get; }

        public override void Send(ListenEvent ev)
        {
            if(!string.IsNullOrEmpty(m_Name) &&
                ev.Name != m_Name)
            {
                return;
            }
            if(m_ID != UUID.Zero)
            {
                if(m_ID.Equals(UUID.Zero))
                {
                    /* expected ID matches UUID.Zero, so we want to receive every possible UUID */
                }
                else if(!m_ID.Equals(ev.ID))
                {
                    return;
                }
            }
            if(!string.IsNullOrEmpty(m_Message) &&
                ev.Message != m_Message)
            {
                return;
            }

            if (IsActive)
            {
                m_Send(ev);
            }
        }
    }
}
