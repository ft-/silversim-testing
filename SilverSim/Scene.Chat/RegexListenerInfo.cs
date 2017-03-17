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
using System.Text.RegularExpressions;

namespace SilverSim.Scene.Chat
{
    public sealed class RegexListenerInfo : ChatServiceInterface.Listener
    {
        readonly int m_Channel;
        readonly Regex m_Name;
        readonly string m_NamePlain = string.Empty;
        readonly UUID m_ID;
        readonly Regex m_Message;
        readonly string m_MessagePlain = string.Empty;
        readonly Int32 m_RegexBitfield;
        readonly Func<UUID> m_GetUUID;
        readonly Func<Vector3> m_GetPos;
        readonly Action<ListenEvent> m_Send;
        public override bool IsActive { get; set; }
        public override bool IsAgent
        {
            get 
            {
                return false;
            }
        }

        readonly ChatHandler m_Handler;

        public override bool IsMatching(string name, UUID id, string message, Int32 regexBitfield)
        {
            return m_NamePlain == name &&
                m_ID == id &&
                m_MessagePlain == message &&
                m_RegexBitfield == regexBitfield;
        }

        internal RegexListenerInfo(
            ChatHandler handler,
            int channel, 
            string name,
            UUID id, 
            string message,
            Int32 regexBitfield,
            Func<UUID> getuuid, 
            Func<Vector3> getpos, 
            Action<ListenEvent> send)
        {
            m_RegexBitfield = regexBitfield;
            IsActive = true;
            m_Handler = handler;
            m_Channel = channel;
            if(!String.IsNullOrEmpty(name))
            {
                if((m_RegexBitfield & 1) != 0)
                {
                    m_Name = new Regex(name);
                    m_NamePlain = name;
                }
                else
                {
                    m_NamePlain = name;
                }
            }
            m_ID = id;
            if(!String.IsNullOrEmpty(name))
            {
                if ((m_RegexBitfield & 2) != 0)
                {
                    m_Message = new Regex(message);
                    m_MessagePlain = name;
                }
                else
                {
                    m_MessagePlain = message;
                }
            }
            m_GetUUID = getuuid;
            m_GetPos = getpos;
            m_Send = send;
        }

        public override void Serialize(List<object> res, int handle)
        {
            res.Add(IsActive);
            res.Add(handle);
            res.Add(Channel);
            res.Add(m_NamePlain);
            res.Add(m_ID);
            res.Add(m_MessagePlain);
            res.Add(m_RegexBitfield);
        }

        public override void Remove()
        {
            m_Handler.Remove(this);
        }

        public override int Channel
        {
            get
            {
                return m_Channel;
            }
        }

        public override Func<Vector3> GetPosition
        {
            get
            {
                return m_GetPos;
            }
        }

        public override Func<UUID> GetUUID
        {
            get
            {
                return m_GetUUID;
            }
        }

        public override void Send(ListenEvent ev)
        {
            if(m_Name != null)
            {
                if ((m_RegexBitfield & 1) != 0)
                {
                    if (!m_Name.IsMatch(ev.Name))
                    {
                        return;
                    }
                }
                else
                {
                    if(m_NamePlain != ev.Name)
                    {
                        return;
                    }
                }
            }
            if(m_ID != null)
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
            if(m_Message != null)
            {
                if ((m_RegexBitfield & 2) != 0)
                {
                    if (!m_Message.IsMatch(ev.Message))
                    {
                        return;
                    }
                }
                else
                {
                    if(m_MessagePlain != ev.Message)
                    {
                        return;
                    }
                }
            }

            if (IsActive)
            {
                m_Send(ev);
            }
        }
    }
}
