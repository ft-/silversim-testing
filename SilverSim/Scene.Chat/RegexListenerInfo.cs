// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
