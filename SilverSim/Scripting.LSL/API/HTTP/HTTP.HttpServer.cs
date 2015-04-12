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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;

namespace SilverSim.Scripting.LSL.API.HTTP
{
    public partial class HTTP_API
    {
        [APILevel(APIFlags.LSL)]
        public UUID llRequestURL(ScriptInstance Instance)
        {
            lock(Instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    UUID urlID = m_HTTPHandler.RequestURL(Instance.Part, Instance.Item);
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_GRANTED;
                    ev.Body = "";
                    Instance.PostEvent(ev);
                }
                catch
                {
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_DENIED;
                    ev.Body = "";
                    Instance.PostEvent(ev);
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llReleaseURL(ScriptInstance Instance, string url)
        {
#warning Implement llReleaseURL()
            lock (Instance)
            {
                m_HTTPHandler.ReleaseURL(url);
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llRequestSecureURL(ScriptInstance Instance)
        {
            lock (Instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    UUID urlID = m_HTTPHandler.RequestSecureURL(Instance.Part, Instance.Item);
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_GRANTED;
                    ev.Body = "https://" + m_HTTPHandler;
                    Instance.PostEvent(ev);
                }
                catch
                {
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_DENIED;
                    ev.Body = "";
                    Instance.PostEvent(ev);
                }
                return reqID;
            }
        }

        [ExecutedOnScriptReset]
        public static void ResetURLs(ScriptInstance Instance)
        {
            Script script = (Script)Instance;
            lock (script)
            {
            }
        }
    }
}
