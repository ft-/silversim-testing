using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types.Parcel;
using SilverSim.Scene.Types.Object;
using SilverSim.Types.Inventory;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        GroupPowers GetGroupPowers(IAgent agent, UGI group)
        {
            if(!IsGroupMember(agent, group))
            {
                return GroupPowers.None;
            }

            List<GroupRole> roles = GroupsService.Roles[agent.Owner, group, agent.Owner];
            GroupPowers powers = GroupPowers.None;
            foreach (GroupRole role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }

        bool HasGroupPower(IAgent agent, UGI group, GroupPowers power)
        {
            return (GetGroupPowers(agent, group) & power) != 0;
        }

        bool IsGroupMember(IAgent agent, UGI group)
        {
            if (null == GroupsService || group.ID == UUID.Zero)
            {
                return false;
            }
            GroupMember member;
            try
            {
                member = GroupsService.Members[agent.Owner, group, agent.Owner];
            }
            catch
            {
                return false;
            }

            /* care more for permissions by checking grid equality */
            if (!member.Principal.EqualsGrid(agent.Owner))
            {
                return false;
            }

            return true;
        }

        public bool IsEstateManager(UUI agent)
        {
            return false;
        }

        public bool IsPossibleGod(UUI agent)
        {
            if (agent.EqualsGrid(Owner))
            {
                return true;
            }

            if (ServerParamService.GetBoolean(ID, "estate_manager_is_god", false) && IsEstateManager(agent))
            {
                return true;
            }

            string god_agents;
            god_agents = ServerParamService.GetString(ID, "god_agents", "");
            string[] god_agents_list = god_agents.Split(new char[] { '|' });
            if(god_agents_list.Length != 1 || god_agents_list[0] != string.Empty)
            {
                foreach(string god_agent in god_agents_list)
                {
                    UUI uui;
                    try
                    {
                        uui = new UUI(god_agent);
                    }
                    catch
                    {
                        m_Log.WarnFormat("Invalid UUI '{0}' found in god_agents variable", god_agent);
                        continue;
                    }
                    if(uui.EqualsGrid(agent))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsSimConsoleAllowed(UUI agent)
        {
            if(IsPossibleGod(agent))
            {
                return true;
            }

            if (ServerParamService.GetBoolean(ID, "estate_manager_is_simconsole_user", false) && IsEstateManager(agent))
            {
                return true;
            }

            return false;
        }

        #region Object Permissions
        public bool CanRez(IAgent agent, Vector3 location)
        {
            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
            }
            catch
            {
                return false;
            }

            if((pinfo.Flags & ParcelFlags.CreateObjects) != 0)
            {
                return true;
            }
            else if((agent.Owner.EqualsGrid(pinfo.Owner)) || IsPossibleGod(agent.Owner))
            {
                return true;
            }
            else if((pinfo.Flags & ParcelFlags.CreateGroupObjects) != 0 &&
                pinfo.Group.ID != UUID.Zero &&
                HasGroupPower(agent, pinfo.Group, GroupPowers.AllowRez))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanRunScript(IAgent agent, Vector3 location)
        {
            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
            }
            catch
            {
                return false;
            }

            if ((pinfo.Flags & ParcelFlags.AllowOtherScripts) != 0)
            {
                return true;
            }
            else if ((agent.Owner.EqualsGrid(pinfo.Owner)) || IsPossibleGod(agent.Owner))
            {
                return true;
            }
            else if ((pinfo.Flags & ParcelFlags.AllowGroupScripts) != 0 &&
                pinfo.Group.ID != UUID.Zero &&
                IsGroupMember(agent, pinfo.Group))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanMove(IAgent agent, ObjectGroup group, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                if(group.RootPart.IsLocked && group.Owner.EqualsGrid(agent.Owner))
                {
                    return false;
                }
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(group.Owner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agent.Owner.EqualsGrid(group.Owner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanMove

            if(group.RootPart.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Modify))
            {
                return true;
            }
            else if ((group.RootPart.EveryoneMask & InventoryPermissionsMask.Move) != 0)
            {
                return true;
            }

            if (HasGroupPower(agent, group.Group, GroupPowers.ObjectManipulate))
            {
                return true;
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
                if (pinfo.Owner.EqualsGrid(agent.Owner))
                {
                    return true;
                }

                if (HasGroupPower(agent, pinfo.Group, GroupPowers.ObjectManipulate))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool CanEdit(IAgent agent, ObjectGroup group, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(group.Owner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agent.Owner.EqualsGrid(group.Owner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanEdit

            if (group.RootPart.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Modify))
            {
                return true;
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
                if (pinfo.Owner.EqualsGrid(agent.Owner))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool CanDelete(IAgent agent, ObjectGroup group, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(group.Owner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agent.Owner.EqualsGrid(group.Owner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanDelete

            if (HasGroupPower(agent, group.Group, GroupPowers.ObjectManipulate))
            {
                return true;
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
                if (pinfo.Owner.EqualsGrid(agent.Owner))
                {
                    return true;
                }

                if (HasGroupPower(agent, pinfo.Group, GroupPowers.ObjectManipulate))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool CanReturn(IAgent agent, ObjectGroup group, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(group.Owner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agent.Owner.EqualsGrid(group.Owner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanReturn?

            if (HasGroupPower(agent, group.Group, GroupPowers.ReturnGroupSet))
            {
                return true;
            }
            else if(group.IsGroupOwned)
            {
                if(HasGroupPower(agent, group.Group, GroupPowers.ReturnGroupOwned))
                {
                    return true;
                }
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
                if (pinfo.Owner.EqualsGrid(agent.Owner))
                {
                    return true;
                }

                if (!pinfo.Group.Equals(group.Group) && HasGroupPower(agent, pinfo.Group, GroupPowers.ReturnNonGroup))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public bool CanTakeCopy(IAgent agent, ObjectGroup group, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(group.Owner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agent.Owner.EqualsGrid(group.Owner))
            {
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

            InventoryPermissionsMask checkMask = InventoryPermissionsMask.Copy;
            if(!agent.Owner.EqualsGrid(group.Owner))
            {
                checkMask |= InventoryPermissionsMask.Transfer;
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
                if (pinfo.Owner.EqualsGrid(agent.Owner) && ServerParamService.GetBoolean(ID, "parcel_owner_is_admin", false))
                {
                    return true;
                }
            }
            catch
            {
            }

            if (group.RootPart.CheckPermissions(agent.Owner, group.Group, checkMask))
            {
                return true;
            }
            else if((group.RootPart.EveryoneMask & InventoryPermissionsMask.Copy) != 0)
            {
                return true;
            }
            return false;
        }

        public bool CanTake(IAgent agent, ObjectGroup group, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(group.Owner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agent.Owner.EqualsGrid(group.Owner))
            {
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];
                if (pinfo.Owner.EqualsGrid(agent.Owner) && ServerParamService.GetBoolean(ID, "parcel_owner_is_admin", false))
                {
                    return true;
                }
            }
            catch
            {
            }

            if (!agent.Owner.EqualsGrid(group.Owner))
            {
                if (group.RootPart.CheckPermissions(agent.Owner, group.Group, InventoryPermissionsMask.Transfer))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        public bool CanTerraform(IAgent agent, Vector3 location)
        {
            if (IsPossibleGod(agent.Owner))
            {
                return true;
            }
            else if(RegionSettings.BlockTerraform)
            {
                return false;
            }
            return false;
        }
    }
}
