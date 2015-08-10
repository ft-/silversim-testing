// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Chat
{
    class ListenerInfo : ChatServiceInterface.Listener
    {
        private int m_Channel;
        private string m_Name;
        private UUID m_ID;
        private string m_Message;
        private ChatServiceInterface.GetUUIDDelegate m_GetUUID;
        private ChatServiceInterface.GetPositionDelegate m_GetPos;
        private Action<ListenEvent> m_Send;
        public override bool IsActive { get; set; }
        private bool m_IsAgent;
        public override bool IsAgent { get { return m_IsAgent; } }

        private ChatHandler m_Handler;

        public ListenerInfo(
            ChatHandler handler,
            int channel, 
            string name,
            UUID id, 
            string message,
            ChatServiceInterface.GetUUIDDelegate getuuid, 
            ChatServiceInterface.GetPositionDelegate getpos, 
            Action<ListenEvent> send,
            bool isAgent)
        {
            m_IsAgent = isAgent;
            IsActive = true;
            m_Handler = handler;
            m_Channel = channel;
            m_Name = name;
            m_ID = id;
            m_Message = message;
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
            if(!String.IsNullOrEmpty(m_Name))
            {
                if(ev.Name != m_Name)
                {
                    return;
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
            if(!String.IsNullOrEmpty(m_Message))
            {
                if(ev.Message != m_Message)
                {
                    return;
                }
            }

            if (IsActive)
            {
                m_Send(ev);
            }
        }
    }
}
