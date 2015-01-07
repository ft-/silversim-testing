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

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Text.RegularExpressions;

namespace SilverSim.Scene.Chat
{
    class RegexListenerInfo : ChatServiceInterface.Listener
    {
        private int m_Channel;
        private Regex m_Name = null;
        private string m_NamePlain = string.Empty;
        private UUID m_ID;
        private Regex m_Message = null;
        private string m_MessagePlain = string.Empty;
        private Int32 m_RegexBitfield = 0;
        private ChatServiceInterface.GetUUIDDelegate m_GetUUID;
        private ChatServiceInterface.GetPositionDelegate m_GetPos;
        private Action<ListenEvent> m_Send;
        public override bool IsActive { get; set; }
        public override bool IsAgent
        {
            get 
            {
                return false;
            }
        }

        private ChatHandler m_Handler;

        public RegexListenerInfo(
            ChatHandler handler,
            int channel, 
            string name,
            UUID id, 
            string message,
            Int32 regexBitfield,
            ChatServiceInterface.GetUUIDDelegate getuuid, 
            ChatServiceInterface.GetPositionDelegate getpos, 
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

        public override void Remove()
        {
            m_Handler.RemoveListener(this);
        }

        public override int Channel
        {
            get
            {
                return m_Channel;
            }
        }

        public override ChatServiceInterface.GetPositionDelegate GetPosition
        {
            get
            {
                return m_GetPos;
            }
        }

        public override ChatServiceInterface.GetUUIDDelegate GetUUID
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
