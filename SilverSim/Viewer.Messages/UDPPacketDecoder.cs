// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Viewer.Messages
{
    public class UDPPacketDecoder
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LLUDP PACKET DECODER");

        public readonly Dictionary<MessageType, Func<UDPPacket, Message>> PacketTypes = new Dictionary<MessageType, Func<UDPPacket, Message>>();

        public UDPPacketDecoder(bool allowtrusteddecode = false)
        {
            /* validation of table */
            int numpackettypes = 0;
            foreach(Type t in GetType().Assembly.GetTypes())
            {
                if(t.IsSubclassOf(typeof(Message)))
                {
                    if(null != Attribute.GetCustomAttribute(t, typeof(TrustedAttribute)) && !allowtrusteddecode)
                    {
                        continue;
                    }
                    UDPMessageAttribute m = (UDPMessageAttribute)Attribute.GetCustomAttribute(t, typeof(UDPMessageAttribute));
                    if(m != null)
                    {
                        MethodInfo mi = t.GetMethod("Decode", new Type[] { typeof(UDPPacket) });
                        if(mi == null)
                        {
                        }
                        else if((mi.Attributes & MethodAttributes.Static) == 0 ||
                            (mi.ReturnType != typeof(Message) && !mi.ReturnType.IsSubclassOf(typeof(Message))))
                        {
                            m_Log.WarnFormat("Type {0} does not contain a static Decode method with correct return type", t.FullName);
                        }
                        else if(PacketTypes.ContainsKey(m.Number))
                        {
                            m_Log.WarnFormat("Type {0} Decode method definition duplicates another", t.FullName);
                        }
                        else
                        {
                            PacketTypes.Add(m.Number, (Func<UDPPacket, Message>)Delegate.CreateDelegate(typeof(Func<UDPPacket, Message>), mi));
                            ++numpackettypes;
                        }
                    }
                }
            }
            m_Log.InfoFormat("Initialized {0} packet decoders", numpackettypes);
        }
    }
}
