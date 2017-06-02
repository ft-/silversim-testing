// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.Inventory;
using SilverSim.Types.Parcel;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    [ServerParam("estate_manager_is_god", ParameterType = typeof(bool), DefaultValue = false)]
    [ServerParam("region_owner_is_simconsole_user", ParameterType = typeof(bool), DefaultValue = false)]
    [ServerParam("estate_owner_is_simconsole_user", ParameterType = typeof(bool), DefaultValue = false)]
    [ServerParam("region_manager_is_simconsole_user", ParameterType = typeof(bool), DefaultValue = false)]
    [ServerParam("parcel_owner_is_admin", ParameterType = typeof(bool), DefaultValue = false)]
    [ServerParam("god_agents", ParameterType = typeof(string), DefaultValue = "")]
    public abstract partial class SceneInterface
    {
        private void ParameterUpdatedHandler(ref bool localval, ref bool globalval, ref bool settolocalval, UUID regionId, string value)
        {
            if (regionId == UUID.Zero)
            {
                if (string.IsNullOrEmpty(value))
                {
                    localval = false;
                }
                else if (!bool.TryParse(value, out globalval))
                {
                    localval = false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(value))
                {
                    settolocalval = false;
                }
                else if (!bool.TryParse(value, out localval))
                {
                    settolocalval = true;
                    localval = false;
                }
                else
                {
                    settolocalval = true;
                }
            }
        }

        private GroupPowers GetGroupPowers(UUI agentOwner, UGI group)
        {
            if(!IsGroupMember(agentOwner, group))
            {
                return GroupPowers.None;
            }

            List<GroupRole> roles = GroupsService.Roles[agentOwner, group, agentOwner];
            var powers = GroupPowers.None;
            foreach (GroupRole role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }

        public bool HasGroupPower(UUI agentOwner, UGI group, GroupPowers power) => (GetGroupPowers(agentOwner, group) & power) != 0;

        private bool IsGroupMember(UUI agentOwner, UGI group)
        {
            if (GroupsService == null || group.ID == UUID.Zero)
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

        public bool IsRegionOwner(UUI agent) => agent.EqualsGrid(Owner);

        /** <summary>This function also returns true if EO is passed</summary> */
        public bool IsEstateManager(UUI agent)
        {
            uint estateID;
            UUI estateOwner;

            return EstateService.RegionMap.TryGetValue(ID, out estateID) &&
                EstateService.EstateOwner.TryGetValue(estateID, out estateOwner) &&
                (agent.EqualsGrid(estateOwner) ||
                    EstateService.EstateManager[estateID, agent]);
        }

        public bool IsEstateOwner(UUI agent)
        {
            uint estateID;
            UUI estateOwner;

            return EstateService.RegionMap.TryGetValue(ID, out estateID) &&
                EstateService.EstateOwner.TryGetValue(estateID, out estateOwner) &&
                agent.EqualsGrid(estateOwner);
        }

        private bool m_EstateManagerIsGodLocal;
        private bool m_EstateManagerIsGodGlobal;
        private bool m_EstateManagerIsGodSetToLocal;

        private bool EstateManagerIsGod => m_EstateManagerIsGodSetToLocal ? m_EstateManagerIsGodLocal : m_EstateManagerIsGodGlobal;

        [ServerParam("estate_manager_is_god", ParameterType = typeof(bool))]
        public void EstateManagerIsGodUpdated(UUID regionID, string value)
        {
            ParameterUpdatedHandler(
                ref m_EstateManagerIsGodLocal,
                ref m_EstateManagerIsGodGlobal,
                ref m_EstateManagerIsGodSetToLocal,
                regionID, value);
        }

        private readonly RwLockedList<UUI> m_GodAgentsLocal = new RwLockedList<UUI>();
        private readonly RwLockedList<UUI> m_GodAgentsGlobal = new RwLockedList<UUI>();
        private bool m_GodAgentsSetToLocal;

        private void UpdateGodAgentsList(RwLockedList<UUI> list, UUID regionId, string value)
        {
            if(string.IsNullOrEmpty(value))
            {
                list.Clear();
            }
            else
            {
                string[] god_agents_list = value.Split(new char[] { ',' });
                List<UUI> new_gods = new List<UUI>();
                foreach (string god_agent in god_agents_list)
                {
                    UUI uui;
                    try
                    {
                        uui = new UUI(god_agent);
                    }
                    catch
                    {
                        m_Log.WarnFormat("Invalid UUI '{1}' found in {0}/god_agents variable", regionId.ToString(), god_agent);
                        continue;
                    }
                    new_gods.Add(uui);
                }

                foreach(UUI god in new List<UUI>(list))
                {
                    if (!new_gods.Contains(god))
                    {
                        list.Remove(god);
                    }
                }

                foreach(UUI god in new_gods)
                {
                    if(!list.Contains(god))
                    {
                        list.Add(god);
                    }
                }
            }
        }

        [ServerParam("god_agents")]
        public void GodAgentsUpdated(UUID regionID, string value)
        {
            if(regionID != UUID.Zero)
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_GodAgentsSetToLocal = false;
                    m_GodAgentsLocal.Clear();
                }
                UpdateGodAgentsList(m_GodAgentsLocal, regionID, value);
            }
            else
            {
                UpdateGodAgentsList(m_GodAgentsGlobal, regionID, value);
            }
        }

        private bool IsInGodAgents(UUI agent)
        {
            RwLockedList<UUI> activeList = m_GodAgentsSetToLocal ? m_GodAgentsLocal : m_GodAgentsGlobal;
            return activeList.Contains(agent);
        }

        public bool IsPossibleGod(UUI agent) => agent.EqualsGrid(Owner) ||
                (EstateManagerIsGod && IsEstateManager(agent)) ||
                IsInGodAgents(agent);

        private bool m_RegionOwnerIsSimConsoleUserLocal;
        private bool m_RegionOwnerIsSimConsoleUserGlobal;
        private bool m_RegionOwnerIsSimConsoleUserSetToLocal;

        private bool RegionOwnerIsSimConsoleUser => m_RegionOwnerIsSimConsoleUserSetToLocal ? m_RegionOwnerIsSimConsoleUserLocal : m_RegionOwnerIsSimConsoleUserGlobal;

        [ServerParam("region_owner_is_simconsole_user", ParameterType = typeof(bool))]
        public void RegionOwnerIsSimConsoleUserUpdated(UUID regionId, string value)
        {
            ParameterUpdatedHandler(
                ref m_RegionOwnerIsSimConsoleUserLocal,
                ref m_RegionOwnerIsSimConsoleUserGlobal,
                ref m_RegionOwnerIsSimConsoleUserSetToLocal,
                regionId,
                value);
        }

        private bool m_EstateOwnerIsSimConsoleUserLocal;
        private bool m_EstateOwnerIsSimConsoleUserGlobal;
        private bool m_EstateOwnerIsSimConsoleUserSetToLocal;

        private bool EstateOwnerIsSimConsoleUser => m_EstateOwnerIsSimConsoleUserSetToLocal ? m_EstateOwnerIsSimConsoleUserLocal : m_EstateOwnerIsSimConsoleUserGlobal;

        [ServerParam("estate_owner_is_simconsole_user", ParameterType = typeof(bool))]
        public void EstateOwnerIsSimConsoleUserUpdated(UUID regionId, string value)
        {
            ParameterUpdatedHandler(
                ref m_EstateOwnerIsSimConsoleUserLocal,
                ref m_EstateOwnerIsSimConsoleUserGlobal,
                ref m_EstateOwnerIsSimConsoleUserSetToLocal,
                regionId,
                value);
        }

        private bool m_EstateManagerIsSimConsoleUserLocal;
        private bool m_EstateManagerIsSimConsoleUserGlobal;
        private bool m_EstateManagerIsSimConsoleUserSetToLocal;

        private bool EstateManagerIsSimConsoleUser => m_EstateManagerIsSimConsoleUserSetToLocal ? m_EstateManagerIsSimConsoleUserLocal : m_EstateManagerIsSimConsoleUserGlobal;

        [ServerParam("estate_manager_is_simconsole_user", ParameterType = typeof(bool))]
        public void EstateManagerIsSimConsoleUserUpdated(UUID regionId, string value)
        {
            ParameterUpdatedHandler(
                ref m_EstateManagerIsSimConsoleUserLocal,
                ref m_EstateManagerIsSimConsoleUserGlobal,
                ref m_EstateManagerIsSimConsoleUserSetToLocal,
                regionId,
                value);
        }

        public bool IsSimConsoleAllowed(UUI agent)
        {
            if (RegionOwnerIsSimConsoleUser &&
                agent.EqualsGrid(Owner))
            {
                return true;
            }

            if (EstateOwnerIsSimConsoleUser &&
                IsEstateOwner(agent))
            {
                return true;
            }

            if (EstateManagerIsSimConsoleUser &&
                IsEstateManager(agent))
            {
                return true;
            }

            return false;
        }

        #region Object Permissions
        public bool CanRez(UUI agent, Vector3 location)
        {
            ParcelInfo pinfo;
            if (!Parcels.TryGetValue(location, out pinfo))
            {
                return false;
            }

            if ((pinfo.Flags & ParcelFlags.CreateObjects) != 0)
            {
                return true;
            }
            else if ((agent.EqualsGrid(pinfo.Owner)) || IsPossibleGod(agent))
            {
                return true;
            }
            else if ((pinfo.Flags & ParcelFlags.CreateGroupObjects) != 0 &&
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

        public bool CanRunScript(UUI agent, Vector3 location)
        {
            ParcelInfo pinfo;
            if (!Parcels.TryGetValue(location, out pinfo))
            {
                return false;
            }

            if ((pinfo.Flags & ParcelFlags.AllowOtherScripts) != 0)
            {
                return true;
            }
            else if ((agent.EqualsGrid(pinfo.Owner)) || IsPossibleGod(agent))
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

        public bool CanReclaimParcel(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            if (parcelInfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            return false;
        }

        public bool CanGodForceParcelOwner(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            return false;
        }

        public bool CanGodMarkParcelAsContent(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            return false;
        }

        public bool CanDeedParcel(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            if (parcelInfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            if (HasGroupPower(agentOwner, parcelInfo.Group, GroupPowers.LandDeed))
            {
                return true;
            }

            return false;
        }

        public bool CanDivideJoinParcel(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            if (parcelInfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            if (HasGroupPower(agentOwner, parcelInfo.Group, GroupPowers.LandDivideJoin))
            {
                return true;
            }

            return false;
        }

        public bool CanReleaseParcel(UUI agentOwner, ParcelInfo parcelInfo)
        {
            if (IsPossibleGod(agentOwner))
            {
                return true;
            }

            if (parcelInfo.Owner.EqualsGrid(agentOwner))
            {
                return true;
            }

            if (HasGroupPower(agentOwner, parcelInfo.Group, GroupPowers.LandRelease))
            {
                return true;
            }

            return false;
        }

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

        private bool m_ParcelOwnerIsAdminLocal;
        private bool m_ParcelOwnerIsAdminGlobal;
        private bool m_ParcelOwnerIsAdminSetToLocal;

        private bool ParcelOwnerIsAdmin => m_ParcelOwnerIsAdminSetToLocal ? m_ParcelOwnerIsAdminLocal : m_ParcelOwnerIsAdminGlobal;

        [ServerParam("parcel_owner_is_admin", ParameterType = typeof(bool))]
        public void ParcelOwnerIsAdminUpdated(UUID regionId, string value)
        {
            ParameterUpdatedHandler(
               ref m_ParcelOwnerIsAdminLocal,
               ref m_ParcelOwnerIsAdminGlobal,
               ref m_ParcelOwnerIsAdminSetToLocal,
               regionId,
               value);
        }

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
                ParcelOwnerIsAdmin)
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

            if (group.IsAttached)
            {
                /* should not be able to take attachments */
                return false;
            }

            ParcelInfo pinfo;
            if(Parcels.TryGetValue(location, out pinfo) &&
                pinfo.Owner.EqualsGrid(agentOwner) &&
                ParcelOwnerIsAdmin)
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
                /* no action required */
            }
            return false;
        }
    }
}
