// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public class ObjectPartInventory : RwLockedSortedDoubleDictionary<UUID, string, ObjectPartInventoryItem>
    {
        public enum ChangeAction
        {
            Add,
            Change,
            Remove
        }

        public event Action<ChangeAction /* change */, UUID /* primID */, UUID /* itemID */> OnChange;

        public int InventorySerial = 1;

        public UUID PartID { get; internal set; }
        readonly object m_DataLock = new object();

        public ObjectPartInventory()
        {
            PartID = UUID.Zero;
        }

        #region LSL style accessors
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public ObjectPartInventoryItem this[InventoryType type, uint index]
        {
            get
            {
                foreach (ObjectPartInventoryItem item in ValuesByKey2)
                {
                    if(type == item.InventoryType)
                    {
                        if(index == 0)
                        {
                            return item;
                        }
                        --index;
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public ObjectPartInventoryItem this[uint index]
        {
            get
            {
                foreach (ObjectPartInventoryItem item in ValuesByKey2)
                {
                    if (index-- == 0)
                    {
                        return item;
                    }
                }
                throw new KeyNotFoundException();
            }
        }
        #endregion

        #region Count specific types
        public int CountType(InventoryType type)
        {
            int n = 0;
            foreach(ObjectPartInventoryItem item in this.Values)
            {
                if(item.InventoryType == type)
                {
                    ++n;
                }
            }

            return n;
        }

        public int CountScripts
        {
            get
            {
                int n = 0;
                foreach (ObjectPartInventoryItem item in this.Values)
                {
                    if (item.InventoryType == InventoryType.LSLText || item.InventoryType == InventoryType.LSLBytecode)
                    {
                        ++n;
                    }
                }

                return n;
            }
        }

        public int CountRunningScripts
        {
            get
            {
                int n = 0;
                foreach (ObjectPartInventoryItem item in this.Values)
                {
                    if (item.InventoryType == InventoryType.LSLText || item.InventoryType == InventoryType.LSLBytecode)
                    {
                        ScriptInstance script = item.ScriptInstance;
                        if(script != null &&
                            script.IsRunning)
                        {
                            ++n;
                        }
                    }
                }

                return n;
            }
        }

        #endregion

        public void Add(ObjectPartInventoryItem item, bool nameChangeAllowed = true)
        {
            lock(m_DataLock)
            {
                if (nameChangeAllowed)
                {
                    int index = 1;
                    string name = item.Name;
                    while (ContainsKey(name))
                    {
                        name = string.Format("{0} {1}", item.Name, index++);
                        if (index > 1000)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    item.Name = name;
                }
                Add(item.ID, item.Name, item);
            }
        }

        #region Overrides
        public new void Add(UUID key1, string key2, ObjectPartInventoryItem item)
        {
            base.Add(key1, key2, item);
            Interlocked.Increment(ref InventorySerial);
            
            var addDelegate = OnChange;
            if(addDelegate != null)
            {
                foreach (Action<ChangeAction, UUID, UUID> d in addDelegate.GetInvocationList())
                {
                    d(ChangeAction.Add, PartID, item.ID);
                }
            }
        }

        public new void ChangeKey(string newKey, string oldKey)
        {
            ObjectPartInventoryItem item;
            lock (m_DataLock)
            {
                base.ChangeKey(newKey, oldKey);
                item = base[newKey];
            }
            Interlocked.Increment(ref InventorySerial);

            var updateDelegate = OnChange;
            if(updateDelegate != null)
            {
                foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                {
                    d(ChangeAction.Change, PartID, item.ID);
                }
            }
        }

        public void Replace(string name, ObjectPartInventoryItem newItem)
        {
            ObjectPartInventoryItem oldItem;
            ScriptInstance script;
            newItem.Name = name;
            lock(m_DataLock)
            {
                oldItem = this[name];
                script = oldItem.RemoveScriptInstance;
                Remove(name);
                if(ContainsKey(newItem.ID))
                {
                    newItem.ID = UUID.Random;
                }
                Add(newItem, false);
            }
            if (null != script)
            {
                script.Remove();
            }
            Interlocked.Increment(ref InventorySerial);
            var updateDelegate = OnChange;
            if (updateDelegate != null)
            {
                foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                {
                    d(ChangeAction.Add, PartID, newItem.ID);
                }
            }
        }

        public new bool Remove(UUID key1)
        {
            ObjectPartInventoryItem item;
            if (base.Remove(key1, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                if (null != script)
                {
                    script.Remove();
                }
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                    {
                        d(ChangeAction.Remove, PartID, key1);
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(string key2)
        {
            ObjectPartInventoryItem item;
            if (base.Remove(key2, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                if (null != script)
                {
                    script.Remove();
                }
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                    {
                        d(ChangeAction.Remove, PartID, item.ID);
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(UUID key1, out ObjectPartInventoryItem item)
        {
            if (base.Remove(key1, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                if (null != script)
                {
                    script.Remove();
                }
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                    {
                        d(ChangeAction.Remove, PartID, item.ID);
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(string key2, out ObjectPartInventoryItem item)
        {
            if (base.Remove(key2, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                if (null != script)
                {
                    script.Remove();
                }
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                    {
                        d(ChangeAction.Remove, PartID, item.ID);
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(UUID key1, string key2)
        {
            if (base.Remove(key1, key2))
            {
#warning check this for Script removal
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (Action<ChangeAction, UUID, UUID> d in updateDelegate.GetInvocationList())
                    {
                        d(ChangeAction.Remove, PartID, key1);
                    }
                }
                return true;
            }
            return false;
        }
        #endregion

        #region XML Deserialization
        ObjectPartInventoryItem FromXML(XmlTextReader reader, UUI currentOwner)
        {
            ObjectPartInventoryItem item = new ObjectPartInventoryItem();
            item.Owner = currentOwner;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
            bool ownerChanged = false;

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        
                        switch (reader.Name)
                        {
                            case "AssetID":
                                item.AssetID = reader.ReadContentAsUUID();
                                break;

                            case "BasePermissions":
                                item.Permissions.Base = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "CreationDate":
                                item.CreationDate = Date.UnixTimeToDateTime(reader.ReadElementValueAsULong());
                                break;

                            case "CreatorID":
                                item.Creator.ID = reader.ReadContentAsUUID();
                                break;

                            case "CreatorData":
                                item.Creator.CreatorData = reader.ReadElementValueAsString();
                                break;

                            case "Description":
                                item.Description = reader.ReadElementValueAsString();
                                break;

                            case "EveryonePermissions":
                                item.Permissions.EveryOne = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "Flags":
                                item.Flags = (InventoryFlags)reader.ReadElementValueAsUInt();
                                break;

                            case "GroupID":
                                item.Group.ID = reader.ReadContentAsUUID();
                                break;

                            case "GroupPermissions":
                                item.Permissions.Group = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "InvType":
                                item.InventoryType = (InventoryType)reader.ReadElementValueAsInt();
                                break;

                            case "ItemID":
                                item.ID = reader.ReadContentAsUUID();
                                break;

                            case "LastOwnerID":
                                item.LastOwner.ID = reader.ReadContentAsUUID();
                                break;

                            case "Name":
                                item.Name = reader.ReadElementValueAsString();
                                break;

                            case "NextPermissions":
                                item.Permissions.NextOwner = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "CurrentPermissions":
                                item.Permissions.Current = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "PermsGranter":
                                grantinfo.PermsGranter.ID = reader.ReadContentAsUUID();
                                break;

                            case "PermsMask":
                                grantinfo.PermsMask = (ScriptPermissions)reader.ReadElementValueAsUInt();
                                break;

                            case "Type":
                                item.AssetType = (AssetType)reader.ReadElementValueAsInt();
                                break;

                            case "OwnerChanged":
                                ownerChanged = reader.ReadElementValueAsBoolean();
                                break;

                            case "OwnerID": /* Do not trust this ever! */
                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "TaskInventoryItem")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        if(ownerChanged)
                        {
                            item.Owner = UUI.Unknown;
                        }
                        return item;

                    default:
                        break;
                }
            }
        }

        public void FillFromXml(XmlTextReader reader, UUI currentOwner)
        {
            ObjectPart part = new ObjectPart();
            part.Owner = currentOwner;
            if(reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch(reader.Name)
                        {
                            case "TaskInventoryItem":
                                Add(FromXML(reader, currentOwner), false);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "TaskInventory")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }
        #endregion

        #region XML Serialization
        public void ToXml(XmlTextWriter writer, XmlSerializationOptions options)
        {
            ToXml(writer, UUI.Unknown, options);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, XmlSerializationOptions options)
        {
            writer.WriteNamedValue("InventorySerial", InventorySerial);
            writer.WriteStartElement("TaskInventory");
            {
                ForEach(delegate(ObjectPartInventoryItem item)
                {
                    writer.WriteStartElement("TaskInventoryItem");
                    {
                        writer.WriteUUID("AssetID", item.AssetID);
                        writer.WriteNamedValue("BasePermissions", (uint)item.Permissions.Base);
                        writer.WriteNamedValue("CreationDate", item.CreationDate.AsUInt);
                        writer.WriteUUID("CreatorID", item.Creator.ID);
                        if (!string.IsNullOrEmpty(item.Creator.CreatorData))
                        {
                            writer.WriteNamedValue("CreatorData", item.Creator.CreatorData);
                        }
                        writer.WriteNamedValue("Description", item.Description);
                        writer.WriteNamedValue("EveryonePermissions", (uint)item.Permissions.EveryOne);
                        writer.WriteNamedValue("Flags", (uint)item.Flags);
                        if ((options & XmlSerializationOptions.WriteOwnerInfo) != XmlSerializationOptions.None)
                        {
                            writer.WriteNamedValue("GroupID", item.Group.ID);
                        }
                        else
                        {
                            writer.WriteNamedValue("GroupID", UUID.Zero);
                        }
                        writer.WriteNamedValue("GroupPermissions", (uint)item.Permissions.Group);
                        writer.WriteNamedValue("InvType", (uint)item.InventoryType);
                        writer.WriteUUID("ItemID", item.ID);
                        writer.WriteUUID("OldItemID", UUID.Zero);
                        writer.WriteUUID("LastOwnerID", item.LastOwner.ID);
                        writer.WriteNamedValue("Name", item.Name);
                        writer.WriteNamedValue("NextPermissions", (uint)item.Permissions.NextOwner);
                        if((options & XmlSerializationOptions.WriteOwnerInfo) != XmlSerializationOptions.None)
                        {
                            writer.WriteUUID("OwnerID", item.Owner.ID);
                            writer.WriteNamedValue("CurrentPermissions", (uint)item.Permissions.Current);
                        }
                        else if((options & XmlSerializationOptions.AdjustForNextOwner) != XmlSerializationOptions.None)
                        {
                            writer.WriteUUID("OwnerID", nextOwner.ID);
                            writer.WriteNamedValue("CurrentPermissions", (uint)item.Permissions.NextOwner);
                        }
                        else
                        {
                            writer.WriteUUID("OwnerID", UUID.Zero);
                            writer.WriteNamedValue("CurrentPermissions", (uint)item.Permissions.Current);
                        }
                        writer.WriteUUID("ParentID", item.ParentFolderID);
                        writer.WriteUUID("ParentPartID", item.ParentFolderID);
                        ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
                        writer.WriteUUID("PermsGranter", grantinfo.PermsGranter.ID);
                        writer.WriteNamedValue("PermsMask", (uint)grantinfo.PermsMask);
                        writer.WriteNamedValue("Type", (int)item.AssetType);
                        writer.WriteNamedValue("OwnerChanged", (options & XmlSerializationOptions.AdjustForNextOwner) != XmlSerializationOptions.None);
                    }
                    writer.WriteEndElement();
                });
            }
            writer.WriteEndElement();
        }
        #endregion
    }
}
