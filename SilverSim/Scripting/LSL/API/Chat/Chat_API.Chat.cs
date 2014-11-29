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

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL.API.Chat
{
    [ScriptApiName("Chat")]
    public partial class Chat_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public static int MaxListenerHandles = 64;

        ObjectPart Part;
        ObjectPartInventoryItem ScriptItem;
        ScriptInstance Instance;

        public void Initialize(ScriptInstance instance, ObjectPart part, ObjectPartInventoryItem scriptItem)
        {
            Part = part;
            ScriptItem = scriptItem;
            Instance = instance;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        protected RwLockedDictionary<int, ChatServiceInterface.Listener> m_Listeners = new RwLockedDictionary<int, ChatServiceInterface.Listener>();

        [APILevel(APIFlags.LSL)]
        public const int PUBLIC_CHANNEL = 0;
        [APILevel(APIFlags.LSL)]
        public const int DEBUG_CHANNEL = 0x7FFFFFFF;

        [APILevel(APIFlags.LSL)]
        private UUID getOwner()
        {
            lock (Instance)
            {
                return Part.ObjectGroup.Owner.ID;
            }
        }

        private void sendChat(ListenEvent ev)
        {
            lock (Instance)
            {
                ev.ID = Part.ObjectGroup.ID;
                ev.Name = Part.ObjectGroup.Name;
                Part.ObjectGroup.Scene.GetService<ChatServiceInterface>().Send(ev);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llShout(int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner();
            sendChat(ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llSay(int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Say;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner();
            sendChat(ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llWhisper(int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Whisper;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner();
            sendChat(ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llOwnerSay(string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = PUBLIC_CHANNEL;
            ev.Type = ListenEvent.ChatType.OwnerSay;
            ev.Message = message;
            ev.TargetID = Part.ObjectGroup.Owner.ID;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner();
            sendChat(ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llRegionSay(int channel, string message)
        {
            if (channel != PUBLIC_CHANNEL)
            {
                ListenEvent ev = new ListenEvent();
                ev.Type = ListenEvent.ChatType.Region;
                ev.Channel = channel;
                ev.Message = message;
                ev.OwnerID = getOwner();
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                sendChat(ev);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRegionSayTo(UUID target, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Region;
            ev.Message = message;
            ev.TargetID = target;
            ev.OwnerID = getOwner();
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            sendChat(ev);
        }

        protected void onListen(ListenEvent ev)
        {
            Instance.PostEvent(ev);
        }

        [APILevel(APIFlags.LSL)]
        public Integer llListen(int channel, string name, UUID id, string msg)
        {
            lock (Instance)
            {
                if (m_Listeners.Count >= MaxListenerHandles)
                {
                    return new Integer(-1);
                }
                ChatServiceInterface chatservice = Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();

                int newhandle = 0;
                ChatServiceInterface.Listener l;
                for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
                {
                    if (!m_Listeners.TryGetValue(newhandle, out l))
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
        }

        [APILevel(APIFlags.LSL)]
        public void llListenRemove(int handle)
        {
            ChatServiceInterface.Listener l;
            lock (Instance)
            {
                if (m_Listeners.Remove(handle, out l))
                {
                    l.Remove();
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llListenControl(int handle, int active)
        {
            ChatServiceInterface.Listener l;
            lock (Instance)
            {
                if (m_Listeners.TryGetValue(handle, out l))
                {
                    l.IsActive = active != 0;
                }
            }
        }

        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_NAME = 1;
        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_MESSAGE = 2;

        #region osListenRegex
        [APILevel(APIFlags.OSSL)]
        public int osListenRegex(int channel, string name, UUID id, string msg, int regexBitfield)
        {
            if (m_Listeners.Count >= MaxListenerHandles)
            {
                return -1;
            }
            ChatServiceInterface chatservice = Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();

            lock (Instance)
            {
                int newhandle = 0;
                ChatServiceInterface.Listener l;
                for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
                {
                    if (!m_Listeners.TryGetValue(newhandle, out l))
                    {
                        l = chatservice.AddListenRegex(
                            channel,
                            name,
                            id,
                            msg,
                            regexBitfield,
                            delegate() { return Part.ID; },
                            delegate() { return Part.GlobalPosition; },
                            onListen);
                        try
                        {
                            m_Listeners.Add(newhandle, l);
                            return newhandle;
                        }
                        catch
                        {
                            l.Remove();
                            return -1;
                        }
                    }
                }
            }
            return -1;
        }
        #endregion

        [ExecutedOnStateChange]
        public void ResetListeners()
        {
            ICollection<ChatServiceInterface.Listener> coll = m_Listeners.Values;
            lock (Instance)
            {
                m_Listeners.Clear();
                foreach (ChatServiceInterface.Listener l in coll)
                {
                    l.Remove();
                }
            }
        }
    }
}
