using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;
using ArribaSim.Scene.Types.Script.Events;
using ArribaSim.Scene.Types.Scene;
using ArribaSim.Scene.ServiceInterfaces.Chat;
using ThreadedClasses;

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
        public void llShout(Integer channel, AString message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Message = message.ToString();
            sendChat(ev);
        }

        public void llSay(Integer channel, AString message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Say;
            ev.Message = message.ToString();
            sendChat(ev);
        }

        public void llWhisper(Integer channel, AString message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Whisper;
            ev.Message = message.ToString();
            sendChat(ev);
        }

        public void llOwnerSay(AString message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = PUBLIC_CHANNEL;
            ev.Type = ListenEvent.ChatType.OwnerSay;
            ev.Message = message.ToString();
            ev.TargetID = Part.Group.Owner.ID;
            sendChat(ev);
        }

        public void llRegionSay(Integer channel, AString message)
        {
            if (channel != PUBLIC_CHANNEL)
            {
                ListenEvent ev = new ListenEvent();
                ev.Type = ListenEvent.ChatType.Region;
                ev.Channel = channel;
                ev.Message = message.ToString();
                sendChat(ev);
            }
        }

        public void llRegionSayTo(UUID target, Integer channel, AString message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Region;
            ev.Message = message.ToString();
            ev.TargetID = target;
            sendChat(ev);
        }

        private void onListen(ListenEvent ev)
        {
            PostEvent(ev);
        }

        public Integer llListen(Integer channel, AString name, UUID id, AString msg)
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
                        channel.AsInt, 
                        name.ToString(), 
                        id, 
                        msg.ToString(), 
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

        public void llListenRemove(Integer handle)
        {
            ChatServiceInterface.Listener l;
            if(m_Listeners.Remove(handle.AsInt, out l))
            {
                l.Remove();
            }
        }

        public void llListenControl(Integer handle, Integer active)
        {
            ChatServiceInterface.Listener l;
            if(m_Listeners.TryGetValue(handle.AsInt, out l))
            {
                l.IsActive = active;
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
