using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;
using ArribaSim.Scene.ServiceInterfaces.Chat;

namespace ArribaSim.Scripting.LSL.Variants.OSSL
{
    public partial class OSSLScript
    {
        public Integer osListenRegex(Integer channel, AString name, UUID id, AString msg, Integer regexBitfield)
        {
            if (m_Listeners.Count >= MaxListenerHandles)
            {
                return new Integer(-1);
            }
            ChatServiceInterface chatservice = Part.Group.Scene.GetService<ChatServiceInterface>();

            int newhandle = 0;
            ChatServiceInterface.Listener l;
            for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
            {
                if (!m_Listeners.TryGetValue(newhandle, out l))
                {
                    l = chatservice.AddListenRegex(
                        channel.AsInt,
                        name.ToString(),
                        id,
                        msg.ToString(),
                        regexBitfield.AsInt,
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
}
