/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Scene.ServiceInterfaces.Chat;
using ArribaSim.Scene.Types.Script.Events;
using ArribaSim.Types;
using System.Collections.Generic;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        private void sendChat(ListenEvent ev)
        {
            ev.ID = Part.Group.ID;
            ev.Name = Part.Group.Name;
            Part.Group.Scene.GetService<ChatServiceInterface>().Send(ev);
        }
        public void llShout(int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Message = message;
            sendChat(ev);
        }

        public void llSay(int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Say;
            ev.Message = message;
            sendChat(ev);
        }

        public void llWhisper(int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Whisper;
            ev.Message = message;
            sendChat(ev);
        }

        public void llOwnerSay(string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = PUBLIC_CHANNEL;
            ev.Type = ListenEvent.ChatType.OwnerSay;
            ev.Message = message;
            ev.TargetID = Part.Group.Owner.ID;
            sendChat(ev);
        }

        public void llRegionSay(int channel, string message)
        {
            if (channel != PUBLIC_CHANNEL)
            {
                ListenEvent ev = new ListenEvent();
                ev.Type = ListenEvent.ChatType.Region;
                ev.Channel = channel;
                ev.Message = message;
                sendChat(ev);
            }
        }

        public void llRegionSayTo(UUID target, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Region;
            ev.Message = message;
            ev.TargetID = target;
            sendChat(ev);
        }

        protected void onListen(ListenEvent ev)
        {
            PostEvent(ev);
        }

        public Integer llListen(int channel, string name, UUID id, string msg)
        {
            if(m_Listeners.Count >= MaxListenerHandles)
            {
                return new Integer(-1);
            }
            ChatServiceInterface chatservice = Part.Group.Scene.GetService<ChatServiceInterface>();

            int newhandle = 0;
            ChatServiceInterface.Listener l;
            for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle )
            {
                if(!m_Listeners.TryGetValue(newhandle, out l))
                {
                    l = chatservice.AddListen(
                        channel, 
                        name, 
                        id, 
                        msg, 
                        delegate() { return Part.ID; },
                        delegate() { return Part.GlobalPosition; }, 
                        onListen);
                    try
                    {
                        m_Listeners.Add(newhandle, l);
                        return new Integer(newhandle);
                    }
                    catch
                    {
                        l.Remove();
                        return new Integer(-1);
                    }
                }
            }
            return new Integer(-1);
        }

        public void llListenRemove(int handle)
        {
            ChatServiceInterface.Listener l;
            if(m_Listeners.Remove(handle, out l))
            {
                l.Remove();
            }
        }

        public void llListenControl(int handle, int active)
        {
            ChatServiceInterface.Listener l;
            if(m_Listeners.TryGetValue(handle, out l))
            {
                l.IsActive = active != 0;
            }
        }

        public void ResetListeners()
        {
            ICollection<ChatServiceInterface.Listener> coll = m_Listeners.Values;
            m_Listeners.Clear();
            foreach (ChatServiceInterface.Listener l in coll)
            {
                l.Remove();
            }
        }
    }
}
