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

using log4net;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Viewer.Messages
{
    public sealed class EQGDecoder
    {
        private static readonly ILog m_Log = LogManager.GetLogger("EQG DECODER");
        public readonly Dictionary<string, Func<IValue, Message>> EQGDecoders = new Dictionary<string, Func<IValue, Message>>();

        public EQGDecoder(bool allowtrusteddecode = false)
        {
            /* validation of table */
            int numeqgtypes = 0;
            foreach (Type t in GetType().Assembly.GetTypes())
            {
                if (t.IsSubclassOf(typeof(Message)))
                {
                    if (Attribute.GetCustomAttribute(t, typeof(TrustedAttribute)) != null && !allowtrusteddecode)
                    {
                        continue;
                    }
                    var m = (EventQueueGetAttribute)Attribute.GetCustomAttribute(t, typeof(EventQueueGetAttribute));
                    if (m != null)
                    {
                        MethodInfo mi = t.GetMethod("DeserializeEQG", new Type[] { typeof(IValue) });
                        if (mi == null)
                        {
                            /* packet does not have DeserializeEQG method */
                        }
                        else if ((mi.Attributes & MethodAttributes.Static) == 0 ||
                            (mi.ReturnType != typeof(Message) && !mi.ReturnType.IsSubclassOf(typeof(Message))))
                        {
                            m_Log.WarnFormat("Type {0} does not contain a static Decode method with correct return type", t.FullName);
                        }
                        else if (EQGDecoders.ContainsKey(m.Name))
                        {
                            m_Log.WarnFormat("Type {0} Decode method definition duplicates another", t.FullName);
                        }
                        else
                        {
                            EQGDecoders.Add(m.Name, (Func<IValue, Message>)Delegate.CreateDelegate(typeof(Func<IValue, Message>), mi));
                            ++numeqgtypes;
                        }
                    }
                }
            }
            m_Log.InfoFormat("Initialized {0} EQG decoders", numeqgtypes);
        }

        public void CheckInit()
        {
            m_Log.InfoFormat("Early initialization of EQG decoder: {0} message types", EQGDecoders.Count);
        }
    }
}
