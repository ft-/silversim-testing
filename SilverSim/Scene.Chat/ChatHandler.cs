// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using ThreadedClasses;

namespace SilverSim.Scene.Chat
{
    #region Service Implementation
    class ChatHandler : ChatServiceInterface
    {
        private RwLockedDictionary<int, ChannelInfo> m_Channels = new RwLockedDictionary<int, ChannelInfo>();

        #region Constructor
        public ChatHandler(double whisperDistance, double sayDistance, double shoutDistance)
        {
            WhisperDistance = whisperDistance;
            SayDistance = sayDistance;
            ShoutDistance = shoutDistance;
        }
        #endregion

        #region Properties
        public double WhisperDistance { get; private set; }
        public double SayDistance { get; private set; }
        public double ShoutDistance { get; private set; }
        #endregion

        #region Send Chat
        public override void Send(ListenEvent ev)
        {
            ChannelInfo ci;
            if(m_Channels.TryGetValue(ev.Channel, out ci))
            {
                ci.Send(ev);
            }
        }
        #endregion

        #region Register Listeners
        public override Listener AddListen(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> send)
        {
            Listener li = new ListenerInfo(this, channel, name, id, message, getuuid, getpos, send, false);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, delegate()
            {
                ChannelInfo newci = new ChannelInfo(this);
                newci.Listeners.Add(li);
                return newci;
            });

            /* check whether we had a fresh add of ChannelInfo */
            if (ci.Listeners.Contains(li))
            {
                return li;
            }
            ci.Listeners.Add(li);
            return li;
        }

        public override Listener AddAgentListen(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> send)
        {
            Listener li = new ListenerInfo(this, channel, name, id, message, getuuid, getpos, send, true);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, delegate()
            {
                ChannelInfo newci = new ChannelInfo(this);
                newci.Listeners.Add(li);
                return newci;
            });

            /* check whether we had a fresh add of ChannelInfo */
            if (ci.Listeners.Contains(li))
            {
                return li;
            }
            ci.Listeners.Add(li);
            return li;
        }

        public override Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, GetUUIDDelegate getuuid, GetPositionDelegate getpos, Action<ListenEvent> send)
        {
            Listener li = new RegexListenerInfo(this, channel, name, id, message, regexBitfield, getuuid, getpos, send);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, delegate()
            {
                ChannelInfo newci = new ChannelInfo(this);
                newci.Listeners.Add(li);
                return newci;
            });

            /* check whether we had a fresh add of ChannelInfo */
            if (ci.Listeners.Contains(li))
            {
                return li;
            }
            ci.Listeners.Add(li);
            return li;
        }

        public override Listener AddRegionListener(int channel, string name, UUID id, string message, GetUUIDDelegate getuuid, Action<ListenEvent> send)
        {
            Listener li = new RegionListenerInfo(this, channel, name, id, message, getuuid, send);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, delegate()
            {
                ChannelInfo newci = new ChannelInfo(this);
                newci.Listeners.Add(li);
                return newci;
            });

            /* check whether we had a fresh add of ChannelInfo */
            if (ci.Listeners.Contains(li))
            {
                return li;
            }
            ci.Listeners.Add(li);
            return li;
        }
        #endregion

        #region Remove Listener
        protected internal void RemoveListener(Listener listener)
        {
            ChannelInfo channel;
            if (m_Channels.TryGetValue(listener.Channel, out channel))
            {
                channel.Listeners.Remove(listener);
                m_Channels.RemoveIf(listener.Channel, delegate(ChannelInfo ch) { return ch.Listeners.Count == 0; });
            }
        }
        #endregion
    }
    #endregion
}
