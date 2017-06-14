// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Chat
{
    #region Service Implementation
    public class ChatHandler : ChatServiceInterface
    {
        private readonly RwLockedDictionary<int, ChannelInfo> m_Channels = new RwLockedDictionary<int, ChannelInfo>();
        private readonly RwLockedList<Listener> m_ChatPass = new RwLockedList<Listener>();

        #region Constructor
        internal ChatHandler(double whisperDistance, double sayDistance, double shoutDistance)
        {
            WhisperDistance = whisperDistance;
            SayDistance = sayDistance;
            ShoutDistance = shoutDistance;
        }
        #endregion

        #region Properties
        public double WhisperDistance { get; internal set; }
        public double SayDistance { get; internal set; }
        public double ShoutDistance { get; internal set; }
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
        public override Listener AddListen(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<Vector3> getpos, Func<UUID> getowner, Action<ListenEvent> send)
        {
            var li = new ListenerInfo(this, channel, name, id, message, getuuid, getpos, getowner, send, false);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, () =>
            {
                var newci = new ChannelInfo(this);
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
            var li = new ListenerInfo(this, channel, name, id, message, getuuid, getpos, getuuid, send, true);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, () =>
            {
                var newci = new ChannelInfo(this);
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

        public override Listener AddListenRegex(int channel, string name, UUID id, string message, Int32 regexBitfield, Func<UUID> getuuid, Func<Vector3> getpos, Func<UUID> getowner, Action<ListenEvent> send)
        {
            var li = new RegexListenerInfo(this, channel, name, id, message, regexBitfield, getuuid, getpos, getowner, send);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, () =>
            {
                var newci = new ChannelInfo(this);
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

        public override Listener AddRegionListener(int channel, string name, UUID id, string message, Func<UUID> getuuid, Func<UUID> getowner, Action<ListenEvent> send)
        {
            var li = new RegionListenerInfo(this, channel, name, id, message, getuuid, getowner, send);

            ChannelInfo ci = m_Channels.GetOrAddIfNotExists(channel, () =>
            {
                var newci = new ChannelInfo(this);
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
        private UUID GetPassListenerUUID() => UUID.Zero;

        public override Listener AddChatPassListener(Action<ListenEvent> send)
        {
            var li = new RegionListenerInfo(this, 0, string.Empty, UUID.Zero, string.Empty, GetPassListenerUUID, null, send);
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
                m_Channels.RemoveIf(listener.Channel, (ChannelInfo ch) => ch.Listeners.Count == 0);
            }
            m_ChatPass.Remove(listener);
        }
        #endregion
    }
    #endregion
}
