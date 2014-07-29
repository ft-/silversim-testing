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
        public const int OS_LISTEN_REGEX_NAME = 1;
        public const int OS_LISTEN_REGEX_MESSAGE = 2;

        #region osListenRegex
        public int osListenRegex(int channel, string name, UUID id, string msg, int regexBitfield)
        {
            if (m_Listeners.Count >= MaxListenerHandles)
            {
                return -1;
            }
            ChatServiceInterface chatservice = Part.Group.Scene.GetService<ChatServiceInterface>();

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
            return -1;
        }
        #endregion
    }
}
