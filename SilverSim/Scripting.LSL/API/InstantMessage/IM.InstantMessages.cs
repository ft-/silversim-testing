// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.IM;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.IM;
using System.Text;
using System;

namespace SilverSim.Scripting.LSL.APIs.IM
{
    public partial class IM_API
    {
        [APILevel(APIFlags.LSL)]
        [ForcedSleep(2)]
        public void llInstantMessage(ScriptInstance Instance, LSLKey user, string message)
        {
            lock(Instance)
            {
                IMServiceInterface imservice = Instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                GridInstantMessage im = new GridInstantMessage();
                im.FromAgent.ID = Instance.Part.Owner.ID;
                im.FromAgent.FullName = Instance.Part.ObjectGroup.Name;
                im.IMSessionID = Instance.Part.ObjectGroup.ID;
                im.ToAgent.ID = user;
                im.Position = Instance.Part.ObjectGroup.GlobalPosition;
                im.RegionID = Instance.Part.ObjectGroup.Scene.ID;
                im.Message = message;
                im.Dialog = GridInstantMessageDialog.MessageFromObject;
                string binBuck = string.Format("{0}/{1}/{2}/{3}\0", 
                    Instance.Part.ObjectGroup.Scene.Name,
                    (int)Math.Floor(im.Position.X),
                    (int)Math.Floor(im.Position.Y),
                    (int)Math.Floor(im.Position.Z));
                im.BinaryBucket = UTF8NoBOM.GetBytes(binBuck);
                im.OnResult = delegate(GridInstantMessage imret, bool success) { };

                imservice.Send(im);
            }
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
