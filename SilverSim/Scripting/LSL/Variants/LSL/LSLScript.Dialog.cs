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

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llDialog(UUID avatar, string message, AnArray buttons, int channel)
        {
            SilverSim.LL.Messages.Script.ScriptDialog m = new SilverSim.LL.Messages.Script.ScriptDialog();
            m.Message = message.Substring(0, 256);
            m.ObjectID = Part.Group.ID;
            m.ImageID = UUID.Zero;
            m.ObjectName = Part.Group.Name;
            m.FirstName = Part.Group.Owner.FirstName;
            m.LastName = Part.Group.Owner.LastName;
            m.ChatChannel = channel;
            for (int c = 0; c < buttons.Count && c < 12; ++c )
            {
                if(buttons.ToString().Equals(""))
                {
                    throw new ArgumentException("button label cannot be blank");
                }
                m.Buttons.Add(buttons.ToString());
            }

            m.OwnerData.Add(Part.Group.Owner.ID);

            try
            {
                Part.Group.Scene.Agents[avatar].SendMessageAlways(m, Part.Group.Scene.ID);
            }
            catch
            {

            }
        }

        public void llTextBox(UUID avatar, string message, int channel)
        {
            AnArray buttons = new AnArray();
            buttons.Add("!!llTextBox!!");
            llDialog(avatar, message, buttons, channel);
        }

        public void llLoadURL(UUID avatar, string message, string url)
        {
            SilverSim.LL.Messages.Script.LoadURL m = new LL.Messages.Script.LoadURL();
            m.ObjectName = Part.Group.Name;
            m.ObjectID = Part.Group.ID;
            m.OwnerID = Part.Group.Owner.ID;
            m.Message = message;
            m.URL = url;

            try
            {
                Part.Group.Scene.Agents[avatar].SendMessageAlways(m, Part.Group.Scene.ID);
            }
            catch
            {

            }
        }
    }
}
