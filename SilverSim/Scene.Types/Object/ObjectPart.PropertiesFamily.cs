// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.LL.Messages.Object;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public ObjectPropertiesFamily PropertiesFamily
        {
            get
            {
                ObjectPropertiesFamily fam = new ObjectPropertiesFamily();
                fam.ObjectID = ID;
                fam.OwnerID = Owner.ID;
                fam.GroupID = ObjectGroup.Group.ID;
                fam.BaseMask = m_Permissions.Base;
                fam.OwnerMask = m_Permissions.Current;
                fam.GroupMask = m_Permissions.Group;
                fam.EveryoneMask = m_Permissions.EveryOne;
                fam.NextOwnerMask = m_Permissions.NextOwner;
                fam.OwnershipCost = ObjectGroup.OwnershipCost;
                fam.SaleType = ObjectGroup.SaleType;
                fam.SalePrice = ObjectGroup.SalePrice;
                fam.Category = ObjectGroup.Category;
                fam.LastOwnerID = ObjectGroup.LastOwner.ID;
                fam.Name = Name;
                fam.Description = Description;
                return fam;
            }
        }
    }
}
