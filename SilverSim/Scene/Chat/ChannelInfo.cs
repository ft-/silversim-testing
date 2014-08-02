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

using SilverSim.Types;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using ThreadedClasses;

namespace SilverSim.Scene.Chat
{
    class ChannelInfo
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
                }
            });
        }
    }
}
