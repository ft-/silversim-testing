// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
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
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        GroupPowers GetGroupPowers(UUI agentOwner, UGI group)
        {
            if(!IsGroupMember(agentOwner, group))
            {
                return GroupPowers.None;
            }

            List<GroupRole> roles = GroupsService.Roles[agentOwner, group, agentOwner];
            GroupPowers powers = GroupPowers.None;
            foreach (GroupRole role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }

        public bool HasGroupPower(UUI agentOwner, UGI group, GroupPowers power)
        {
            return (GetGroupPowers(agentOwner, group) & power) != 0;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        bool IsGroupMember(UUI agentOwner, UGI group)
        {
            if (null == GroupsService || group.ID == UUID.Zero)
            {
                return false;
            }
            GroupMember member;
            try
            {
                member = GroupsService.Members[agentOwner, group, agentOwner];
            }
            catch
            {
                return false;
            }

            /* care more for permissions by checking grid equality */
            if (!member.Principal.EqualsGrid(agentOwner))
            {
                return false;
            }

            return true;
        }

        public bool IsEstateManager(UUI agent)
        {
            if(agent.EqualsGrid(Owner))
            {
                return true;
            }
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
            god_agents = ServerParamService.GetString(ID, "god_agents", string.Empty);
            string[] god_agents_list = god_agents.Split(new char[] { '|' });
            if(god_agents_list.Length != 1 || god_agents_list[0].Length != 0)
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
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanRez(IAgent agent, Vector3 location)
        {
            ParcelInfo pinfo;
            if (!Parcels.TryGetValue(location, out pinfo))
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
                HasGroupPower(agent.Owner, pinfo.Group, GroupPowers.AllowRez))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanRunScript(IAgent agent, Vector3 location)
        {
            ParcelInfo pinfo;
            if(!Parcels.TryGetValue(location, out pinfo))
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
                IsGroupMember(agent.Owner, pinfo.Group))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanMove(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;

            if (IsPossibleGod(agentOwner))
            {
                if(group.RootPart.IsLocked && groupOwner.EqualsGrid(agentOwner))
                {
                    return false;
                }
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agentOwner.EqualsGrid(groupOwner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanMove

            if(group.RootPart.CheckPermissions(agentOwner, agent.Group, InventoryPermissionsMask.Modify))
            {
                return true;
            }
            else if ((group.RootPart.EveryoneMask & InventoryPermissionsMask.Move) != 0)
            {
                return true;
            }

            if (HasGroupPower(agent.Owner, group.Group, GroupPowers.ObjectManipulate))
            {
                return true;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo))
            {
                if (pinfo.Owner.EqualsGrid(agentOwner))
                {
                    return true;
                }

                if (HasGroupPower(agent.Owner, pinfo.Group, GroupPowers.ObjectManipulate))
                {
                    return true;
                }
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanEdit(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;

            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanEdit

            if (group.RootPart.CheckPermissions(agentOwner, agent.Group, InventoryPermissionsMask.Modify))
            {
                return true;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo) &&
                pinfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanChangeGroup(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;

            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanChangeGroup

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo) &&
                pinfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanEditParcelDetails(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            if (parcelInfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            if (HasGroupPower(agentOwner, parcelInfo.Group, GroupPowers.LandEdit))
            {
                return true;
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanDelete(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agentOwner.EqualsGrid(groupOwner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanDelete

            if (HasGroupPower(agent.Owner, group.Group, GroupPowers.ObjectManipulate))
            {
                return true;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo))
            {
                if (pinfo.Owner.EqualsGrid(agentOwner))
                {
                    return true;
                }

                if (HasGroupPower(agent.Owner, pinfo.Group, GroupPowers.ObjectManipulate))
                {
                    return true;
                }
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanReturn(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agentOwner.EqualsGrid(groupOwner))
            {
                return true;
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

#warning Add Friends Rights to CanReturn?

            if (HasGroupPower(agent.Owner, group.Group, GroupPowers.ReturnGroupSet) ||
                (group.IsGroupOwned &&
                HasGroupPower(agent.Owner, group.Group, GroupPowers.ReturnGroupOwned)))
            {
                return true;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo))
            {
                if (pinfo.Owner.EqualsGrid(agentOwner))
                {
                    return true;
                }

                if (!pinfo.Group.Equals(group.Group) && HasGroupPower(agent.Owner, pinfo.Group, GroupPowers.ReturnNonGroup))
                {
                    return true;
                }
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanTakeCopy(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agentOwner.EqualsGrid(groupOwner))
            {
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

            InventoryPermissionsMask checkMask = InventoryPermissionsMask.Copy;
            if(!agentOwner.EqualsGrid(groupOwner))
            {
                checkMask |= InventoryPermissionsMask.Transfer;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo) &&
                pinfo.Owner.EqualsGrid(agentOwner) && 
                ServerParamService.GetBoolean(ID, "parcel_owner_is_admin", false))
            {
                return true;
            }

            if (group.RootPart.CheckPermissions(agentOwner, group.Group, checkMask))
            {
                return true;
            }
            else if((group.RootPart.EveryoneMask & InventoryPermissionsMask.Copy) != 0)
            {
                return true;
            }
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanTake(IAgent agent, ObjectGroup group, Vector3 location)
        {
            UUI agentOwner = agent.Owner;
            UUI groupOwner = group.Owner;
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            /* deny modification of admin objects by non-admins */
            else if (IsPossibleGod(groupOwner))
            {
                return false;
            }

            /* check locked state */
            if (group.RootPart.IsLocked)
            {
                return false;
            }

            /* check object owner */
            if (agentOwner.EqualsGrid(groupOwner))
            {
            }
            else if (group.IsAttached)
            {
                /* others should not be able to edit attachments */
                return false;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo) &&
                pinfo.Owner.EqualsGrid(agentOwner) && 
                ServerParamService.GetBoolean(ID, "parcel_owner_is_admin", false))
            {
                return true;
            }

            if (!agentOwner.EqualsGrid(groupOwner) &&
                group.RootPart.CheckPermissions(agentOwner, group.Group, InventoryPermissionsMask.Transfer))
            {
                return true;
            }
            return false;
        }
        #endregion

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public bool CanTerraform(UUI agentOwner, Vector3 location)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }
            else if(RegionSettings.BlockTerraform)
            {
                return false;
            }

            ParcelInfo pinfo;
            try
            {
                pinfo = Parcels[location];

                if(0 != (pinfo.Flags & ParcelFlags.AllowTerraform))
                {
                    return true;
                }

                if(pinfo.Owner.EqualsGrid(agentOwner))
                {
                    return true;
                }

                if(HasGroupPower(agentOwner, pinfo.Group, GroupPowers.AllowEditLand))
                {
                    return true;
                }
            }
            catch
            {

            }
            return false;
        }
    }
}
