using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;
using ArribaSim.Types.IM;
using ArribaSim.ServiceInterfaces.IM;
using ArribaSim.Scene.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llInstantMessage(UUID user, AString message)
        {
            IMServiceInterface imservice = Part.Group.Scene.GetService<IMServiceInterface>();
            GridInstantMessage im = new GridInstantMessage();
            im.FromAgent.ID = Part.Group.ID;
            im.FromAgent.FullName = Part.Group.Name;
            im.ToAgent.ID = user;
            im.Position = Part.Group.GlobalPosition;
            im.RegionID = Part.Group.Scene.ID;
            im.Message = message.ToString();
            im.OnResult = delegate(GridInstantMessage imret, bool success) { };
            imservice.Send(im);
        }
    }
}
