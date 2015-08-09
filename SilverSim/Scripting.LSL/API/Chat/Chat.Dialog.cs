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

using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Chat
{
    public partial class Chat_API
    {
        [APILevel(APIFlags.LSL)]
        [ForcedSleep(1)]
        public void llDialog(ScriptInstance Instance, LSLKey avatar, string message, AnArray buttons, int channel)
        {
            lock (Instance)
            {
                if (message.Length > 511)
                {
                    throw new ArgumentException("Message more than 511 characters");
                }
                else if(message == "")
                {
                    throw new ArgumentException("Message is empty");
                }
                else if(buttons.Count > 12)
                {
                    throw new ArgumentException("Too many buttons");
                }
                else if(buttons.Count == 0)
                {
                    throw new ArgumentException("At least one button must be defined");
                }
                SilverSim.LL.Messages.Script.ScriptDialog m = new SilverSim.LL.Messages.Script.ScriptDialog();
                m.Message = message.Substring(0, 256);
                m.ObjectID = Instance.Part.ObjectGroup.ID;
                m.ImageID = UUID.Zero;
                m.ObjectName = Instance.Part.ObjectGroup.Name;
                m.FirstName = Instance.Part.ObjectGroup.Owner.FirstName;
                m.LastName = Instance.Part.ObjectGroup.Owner.LastName;
                m.ChatChannel = channel;
                for (int c = 0; c < buttons.Count && c < 12; ++c )
                {
                    if(buttons[c].ToString().Equals(""))
                    {
                        throw new ArgumentException("button label cannot be blank");
                    }
                    else if(buttons[c].ToString().Length > 24)
                    {
                        throw new ArgumentException("button label cannot be more than 24 characters");
                    }
                    m.Buttons.Add(buttons.ToString());
                }

                m.OwnerData.Add(Instance.Part.ObjectGroup.Owner.ID);

                try
                {
                    Instance.Part.ObjectGroup.Scene.Agents[avatar].SendMessageAlways(m, Instance.Part.ObjectGroup.Scene.ID);
                }
                catch
                {

                }
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(1)]
        public void llTextBox(ScriptInstance Instance, LSLKey avatar, string message, int channel)
        {
            AnArray buttons = new AnArray();
            buttons.Add("!!llTextBox!!");
            llDialog(Instance, avatar, message, buttons, channel);
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(10)]
        public void llLoadURL(ScriptInstance Instance, LSLKey avatar, string message, string url)
        {
            lock (Instance)
            {
                SilverSim.LL.Messages.Script.LoadURL m = new LL.Messages.Script.LoadURL();
                m.ObjectName = Instance.Part.ObjectGroup.Name;
                m.ObjectID = Instance.Part.ObjectGroup.ID;
                m.OwnerID = Instance.Part.ObjectGroup.Owner.ID;
                m.Message = message;
                m.URL = url;

                try
                {
                    Instance.Part.ObjectGroup.Scene.Agents[avatar].SendMessageAlways(m, Instance.Part.ObjectGroup.Scene.ID);
                }
                catch
                {

                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llMapDestination(ScriptInstance Instance, string simname, Vector3 pos, Vector3 look_at)
        {
            lock(Instance)
            {
                Script script = (Script)Instance;

                foreach (DetectInfo detinfo in script.m_Detected)
                {
                    try
                    {
                        SilverSim.LL.Messages.Script.ScriptTeleportRequest m = new LL.Messages.Script.ScriptTeleportRequest();
                        m.ObjectName = Instance.Part.ObjectGroup.Name;
                        m.SimName = simname;
                        m.SimPosition = pos;
                        m.LookAt = look_at;

                        Instance.Part.ObjectGroup.Scene.Agents[detinfo.Object.ID].SendMessageAlways(m, Instance.Part.ObjectGroup.Scene.ID);
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
