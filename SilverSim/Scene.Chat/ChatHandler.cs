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
    public class ChatHandler : ChatServiceInterface
    {
        readonly RwLockedDictionary<int, ChannelInfo> m_Channels = new RwLockedDictionary<int, ChannelInfo>();
        readonly RwLockedList<Listener> m_ChatPass = new RwLockedList<Listener>();

        #region Constructor
        internal ChatHandler(double whisperDistance, double sayDistance, double shoutDistance)
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
            if(ev.Channel != ListenEvent.DEBUG_CHANNEL)
            {
                switch(ev.Type)
                {
                    case ListenEvent.ChatType.Say:
                        ev.Distance = SayDistance;
                        break;

                    case ListenEvent.ChatType.Shout:
                        ev.Distance = ShoutDistance;
                        break;

                    case ListenEvent.ChatType.Whisper:
                        ev.Distance = WhisperDistance;
                        break;

                    case ListenEvent.ChatType.Region:
                        break;

                    default:
                        return;
                }
                if(ev.SourceType == ListenEvent.ChatSourceType.Object &&
                    ev.OriginSceneID == UUID.Zero)
                {
                    foreach(Listener li in m_ChatPass)
                    {
                        li.Send(ev);
                    }
                }
            }
        }
        #endregion

        #region Register Listeners
        public override Listener AddListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> send)
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

        public override Listener AddAgentListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> send)
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

        public override Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, Func<UUID> getuuid, Func<Vector3> getpos, Action<ListenEvent> send)
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

        public override Listener AddRegionListener(int channel, string name, UUID id, string message, Func<UUID> getuuid, Action<ListenEvent> send)
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

        /* only to be used for SimCircuit */
        UUID GetPassListenerUUID()
        {
            return UUID.Zero;
        }

        public override Listener AddChatPassListener(Action<ListenEvent> send)
        {
            Listener li = new RegionListenerInfo(this, 0, string.Empty, UUID.Zero, string.Empty, GetPassListenerUUID, send);
            m_ChatPass.Add(li);
            return li;
        }

        #endregion

        #region Remove Listener
        protected internal void Remove(Listener listener)
        {
            ChannelInfo channel;
            if (m_Channels.TryGetValue(listener.Channel, out channel))
            {
                channel.Listeners.Remove(listener);
                m_Channels.RemoveIf(listener.Channel, delegate(ChannelInfo ch) { return ch.Listeners.Count == 0; });
            }
            m_ChatPass.Remove(listener);
        }
        #endregion
    }
    #endregion
}
