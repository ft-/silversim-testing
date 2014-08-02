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
using SilverSim.Scene.Types.Object;
using ThreadedClasses;
using SilverSim.Types;

namespace SilverSim.Scripting.LSL.Variants.OSSL
{
    public partial class OSSLScript : LSL.LSLScript
    {
        public OSSLScript(ObjectPart part)
            : base(part)
        {

        }

        public class Permissions
        {
            public RwLockedList<UUI> Creators = new RwLockedList<UUI>();
            public RwLockedList<UUI> Owners = new RwLockedList<UUI>();
            public bool IsAllowedForParcelOwner;
            public bool IsAllowedForParcelMember;
            public bool IsAllowedForEstateOwner;
            public bool IsAllowedForEstateManager;

            public Permissions()
            {

            }
        }

        public enum ThreatLevelType : uint
        {
            None = 0,
            Nuisance = 1,
            VeryLow = 2,
            Low = 3,
            Moderate = 4,
            High = 5,
            VeryHigh = 6,
            Severe = 7
        }

        public ThreatLevelType ThreatLevel { get; protected set; }

        public static readonly RwLockedDictionary<string, Permissions> OSSLPermissions = new RwLockedDictionary<string, Permissions>();

        public void CheckThreatLevel(string name, ThreatLevelType level)
        {
            if((int)level >= (int)ThreatLevel)
            {
                return;
            }

            Permissions perms;
            if(OSSLPermissions.TryGetValue(name, out perms))
            {
                if(perms.Creators.Contains(Part.Group.Creator))
                {
                    return;
                }
                if(perms.Owners.Contains(Part.Group.Owner))
                {
                    return;
                }
                /* TODO: implement parcel rights */

                if(perms.IsAllowedForEstateOwner)
                {
                    if(Part.Group.Scene.Owner == Part.Group.Owner)
                    {
                        return;
                    }
                }

                if(perms.IsAllowedForEstateManager)
                {
                    /* TODO: implement estate managers */
                }
            }
            throw new Exception(string.Format("OSSL Function {0} not allowed", name));
        }
    }
}
