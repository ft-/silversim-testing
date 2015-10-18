// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using ThreadedClasses;

namespace SilverSim.Scene.Chat
{
    sealed class ChannelInfo
    {
        public RwLockedList<ChatServiceInterface.Listener> Listeners = new RwLockedList<ChatServiceInterface.Listener>();
        private ChatHandler m_Handler;

        public ChannelInfo(ChatHandler handler)
        {
            m_Handler = handler;
        }

        public void Send(ListenEvent ev)
        {
            double sayDistanceSquared = m_Handler.SayDistance * m_Handler.SayDistance;
            double shoutDistanceSquared = m_Handler.ShoutDistance * m_Handler.ShoutDistance;
            double whisperDistanceSquared = m_Handler.WhisperDistance * m_Handler.WhisperDistance;

            Listeners.ForEach(delegate(ChatServiceInterface.Listener listener)
            {
                switch(ev.Type)
                {
                    case ListenEvent.ChatType.Region:
                        listener.Send(ev);
                        break;

                    case ListenEvent.ChatType.Say:
                        if (ev.TargetID.Equals(UUID.Zero))
                        {
                            if ((ev.GlobalPosition - listener.GetPosition()).LengthSquared <= sayDistanceSquared ||
                                listener.IsIgnorePosition)
                            {
                                listener.Send(ev);
                            }
                        }
                        else if (listener.GetUUID().Equals(ev.TargetID))
                        {
                            listener.Send(ev);
                        }
                        break;

                    case ListenEvent.ChatType.Shout:
                        if (ev.TargetID.Equals(UUID.Zero))
                        {
                            if ((ev.GlobalPosition - listener.GetPosition()).LengthSquared <= shoutDistanceSquared ||
                                listener.IsIgnorePosition)
                            {
                                listener.Send(ev);
                            }
                        }
                        else if (listener.GetUUID().Equals(ev.TargetID))
                        {
                            listener.Send(ev);
                        }
                        break;

                    case ListenEvent.ChatType.Whisper:
                        if (ev.TargetID.Equals(UUID.Zero))
                        {
                            if ((ev.GlobalPosition - listener.GetPosition()).LengthSquared <= whisperDistanceSquared ||
                                listener.IsIgnorePosition)
                            {
                                listener.Send(ev);
                            }
                        }
                        else if (listener.GetUUID().Equals(ev.TargetID))
                        {
                            listener.Send(ev);
                        }
                        break;

                    case ListenEvent.ChatType.OwnerSay:
                        if(listener.GetUUID().Equals(ev.TargetID))
                        {
                            listener.Send(ev);
                        }
                        break;

                    case ListenEvent.ChatType.StartTyping:
                    case ListenEvent.ChatType.StopTyping:
                        if(!listener.IsAgent)
                        {

                        }
                        else if (ev.TargetID.Equals(UUID.Zero))
                        {
                            if ((ev.GlobalPosition - listener.GetPosition()).LengthSquared <= sayDistanceSquared ||
                                listener.IsIgnorePosition)
                            {
                                listener.Send(ev);
                            }
                        }
                        else if (listener.GetUUID().Equals(ev.TargetID))
                        {
                            listener.Send(ev);
                        }
                        break;
                }
            });
        }
    }
}
