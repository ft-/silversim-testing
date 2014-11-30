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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Reflection;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_UNKNOWN_DETAIL = -1;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_NAME = 1;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_DESC = 2;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_POS = 3;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_ROT = 4;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_VELOCITY = 5;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_OWNER = 6;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_GROUP = 7;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_CREATOR = 8;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_RUNNING_SCRIPT_COUNT = 9;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_TOTAL_SCRIPT_COUNT = 10;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_SCRIPT_MEMORY = 11;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_SCRIPT_TIME = 12;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_PRIM_EQUIVALENCE = 13;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_SERVER_COST = 14;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_STREAMING_COST = 15;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_PHYSICS_COST = 16;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_CHARACTER_TIME = 17;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_ROOT = 18;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_ATTACHED_POINT = 19;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_PATHFINDING_TYPE = 20;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_PHYSICS = 21;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_PHANTOM = 22;
        [APILevel(APIFlags.LSL)]
        public const int OBJECT_TEMP_ON_REZ = 23;

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetCenterOfMass()
        {
#warning Implement llGetCenterOfMass()
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetCreator()
        {
            return Part.ObjectGroup.RootPart.Creator.ID;
        }

        [APILevel(APIFlags.LSL)]
        public string llGetObjectDesc()
        {
            return Part.ObjectGroup.Name;
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetObjectDetails(AnArray param)
        {
            AnArray parout = new AnArray();
            lock (this)
            {
                Part.ObjectGroup.GetObjectDetails(param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        public string llGetObjectName()
        {
            lock (this)
            {
                return Part.ObjectGroup.Description;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetObjectDesc(string desc)
        {
            lock (this)
            {
                Part.ObjectGroup.Description = desc;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetObjectName(string name)
        {
            lock (this)
            {
                Part.ObjectGroup.Name = name;
            }
        }

        [APILevel(APIFlags.LSL)]
        public int llSetRegionPos(Vector3 pos)
        {
#warning Implement llSetRegionPos(Vector3)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetVel()
        {
            lock (this)
            {
                return Part.ObjectGroup.Velocity;
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetOwner()
        {
            lock (Instance)
            {
                return Part.ObjectGroup.Owner.ID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetOwnerKey(UUID id)
        {
            lock (Instance)
            {
                ObjectPart part;
                try
                {
                    part = Part.ObjectGroup.Scene.Primitives[id];
                }
                catch
                {
                    return id;
                }
                return part.Owner.ID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public int llGetNumberOfPrims()
        {
            lock (this)
            {
                return Part.ObjectGroup.Count;
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetLinkKey(int link)
        {
            lock (this)
            {
                if (link == LINK_THIS)
                {
                    return Part.ID;
                }
                else
                {
                    return Part.ObjectGroup[link].ID;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetLinkName(int link)
        {
            lock (this)
            {
                if (link == LINK_THIS)
                {
                    return Part.Name;
                }
                else
                {
                    return Part.ObjectGroup[link].Name;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public int llGetLinkNumber()
        {
            lock (this)
            {
                return Part.LinkNumber;
            }
        }

        #region osMessageObject
        [APILevel(APIFlags.OSSL)]
        public void osMessageObject(UUID objectUUID, string message)
        {
            lock (this)
            {
                Instance.CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ScriptInstance.ThreatLevelType.Low);

                IObject obj = Part.ObjectGroup.Scene.Objects[objectUUID];
                DataserverEvent ev = new DataserverEvent();
                ev.Data = message;
                ev.QueryID = Part.ObjectGroup.ID;
                obj.PostEvent(ev);
            }
        }
        #endregion
    }
}
