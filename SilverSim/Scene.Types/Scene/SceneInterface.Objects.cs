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
using SilverSim.LL.Messages;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public void HandleRequestPayPrice(Message m)
        {
            SilverSim.LL.Messages.Object.RequestPayPrice req = (SilverSim.LL.Messages.Object.RequestPayPrice)m;
        }

        public void HandleObjectSpinStart(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSpinStart req = (SilverSim.LL.Messages.Object.ObjectSpinStart)m;
        }

        public void HandleObjectSpinUpdate(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSpinUpdate req = (SilverSim.LL.Messages.Object.ObjectSpinUpdate)m;
        }

        public void HandleObjectSpinStop(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSpinStop req = (SilverSim.LL.Messages.Object.ObjectSpinStop)m;
        }

        public void HandleObjectShape(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectShape req = (SilverSim.LL.Messages.Object.ObjectShape)m;
        }

        public void HandleObjectSaleInfo(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSaleInfo req = (SilverSim.LL.Messages.Object.ObjectSaleInfo)m;
        }

        public void HandleObjectRotation(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectRotation req = (SilverSim.LL.Messages.Object.ObjectRotation)m;
        }

        public void HandleObjectPermissions(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectPermissions req = (SilverSim.LL.Messages.Object.ObjectPermissions)m;
        }

        public void HandleObjectOwner(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectOwner req = (SilverSim.LL.Messages.Object.ObjectOwner)m;
        }

        public void HandleObjectName(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectName req = (SilverSim.LL.Messages.Object.ObjectName)m;
        }

        public void HandleObjectLink(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectLink req = (SilverSim.LL.Messages.Object.ObjectLink)m;
        }

        public void HandleObjectDelink(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDelink req = (SilverSim.LL.Messages.Object.ObjectDelink)m;
        }

        public void HandleObjectGroup(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectGroup req = (SilverSim.LL.Messages.Object.ObjectGroup)m;
        }

        public void HandleObjectIncludeInSearch(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectIncludeInSearch req = (SilverSim.LL.Messages.Object.ObjectIncludeInSearch)m;
        }

        public void HandleObjectFlagUpdate(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectFlagUpdate req = (SilverSim.LL.Messages.Object.ObjectFlagUpdate)m;
        }

        public void HandleObjectMaterial(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectMaterial req = (SilverSim.LL.Messages.Object.ObjectMaterial)m;
        }

        public void HandleObjectExtraParams(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectExtraParams req = (SilverSim.LL.Messages.Object.ObjectExtraParams)m;
        }

        public void HandleObjectExportSelected(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectExportSelected req = (SilverSim.LL.Messages.Object.ObjectExportSelected)m;
        }

        public void HandleObjectSelect(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSelect req = (SilverSim.LL.Messages.Object.ObjectSelect)m;
        }

        public void HandleObjectDrop(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDrop req = (SilverSim.LL.Messages.Object.ObjectDrop)m;
        }

        public void HandleObjectAttach(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectAttach req = (SilverSim.LL.Messages.Object.ObjectAttach)m;
        }

        public void HandleObjectDetach(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDetach req = (SilverSim.LL.Messages.Object.ObjectDetach)m;
        }

        public void HandleObjectDescription(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDescription req = (SilverSim.LL.Messages.Object.ObjectDescription)m;
        }

        public void HandleObjectDeselect(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDeselect req = (SilverSim.LL.Messages.Object.ObjectDeselect)m;
        }

        public void HandleObjectClickAction(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectClickAction req = (SilverSim.LL.Messages.Object.ObjectClickAction)m;
        }

        public void HandleObjectCategory(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectCategory req = (SilverSim.LL.Messages.Object.ObjectCategory)m;
        }

        public void HandleObjectBuy(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectBuy req = (SilverSim.LL.Messages.Object.ObjectBuy)m;
        }

        public void HandleBuyObjectInventory(Message m)
        {
            SilverSim.LL.Messages.Object.BuyObjectInventory req = (SilverSim.LL.Messages.Object.BuyObjectInventory)m;
        }

        public void HandleObjectGrab(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectGrab req = (SilverSim.LL.Messages.Object.ObjectGrab)m;
        }

        public void HandleObjectGrabUpdate(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectGrabUpdate req = (SilverSim.LL.Messages.Object.ObjectGrabUpdate)m;
        }

        public void HandleObjectDeGrab(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDeGrab req = (SilverSim.LL.Messages.Object.ObjectDeGrab)m;
        }

        public void HandleRequestObjectPropertiesFamily(Message m)
        {
            SilverSim.LL.Messages.Object.RequestObjectPropertiesFamily req = (SilverSim.LL.Messages.Object.RequestObjectPropertiesFamily)m;
        }
    }
}
