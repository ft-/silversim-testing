﻿/*

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

using ArribaSim.ServiceInterfaces.IM;
using ArribaSim.Types;
using ArribaSim.Types.IM;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llInstantMessage(UUID user, string message)
        {
            IMServiceInterface imservice = Part.Group.Scene.GetService<IMServiceInterface>();
            GridInstantMessage im = new GridInstantMessage();
            im.FromAgent.ID = Part.Group.ID;
            im.FromAgent.FullName = Part.Group.Name;
            im.ToAgent.ID = user;
            im.Position = Part.Group.GlobalPosition;
            im.RegionID = Part.Group.Scene.ID;
            im.Message = message;
            im.OnResult = delegate(GridInstantMessage imret, bool success) { };
            imservice.Send(im);
        }
    }
}
