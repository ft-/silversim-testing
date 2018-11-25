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

using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectPartInventory : RwLockedSortedDoubleDictionary<UUID, string, ObjectPartInventoryItem>
    {
        public enum ChangeAction
        {
            Add,
            Change,
            Remove,
            NextOwnerAssetID
        }

        public event Action<ChangeAction /* change */, UUID /* primID */, UUID /* itemID */> OnChange;
        public event Action<ObjectInventoryUpdateInfo> OnInventoryUpdate;

        public int InventorySerial = 1;

        private UUID m_PartID;
        public UUID PartID
        {
            get
            {
                return m_PartID;
            }
            internal set
            {
                m_PartID = value;
                foreach(ObjectPartInventoryItem item in ValuesByKey1)
                {
                    item.ParentFolderID = value;
                    item.UpdateInfo.UpdateIDs();
                }
            }
        }

        private readonly object m_DataLock = new object();

        private int m_SuspendCount;
        public void SuspendScripts()
        {
            Interlocked.Increment(ref m_SuspendCount);
        }

        public void ResumeScripts()
        {
            Interlocked.Decrement(ref m_SuspendCount);
        }

        protected bool AreScriptsSuspended()
        {
            return m_SuspendCount > 0;
        }

        public ObjectPartInventory()
        {
            PartID = UUID.Zero;
        }

        #region LSL style accessors
        public ObjectPartInventoryItem this[InventoryType type, uint index]
        {
            get
            {
                foreach (var item in ValuesByKey2)
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
                foreach (var item in ValuesByKey2)
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
            foreach(var item in this.Values)
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
                foreach (var item in this.Values)
                {
                    if (item.InventoryType == InventoryType.LSL)
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
                foreach (var item in this.Values)
                {
                    if (item.InventoryType == InventoryType.LSL)
                    {
                        var script = item.ScriptInstance;
                        if(script?.IsRunning ?? false)
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
                    var name = item.Name;
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
            item.ParentFolderID = PartID;
            item.UpdateInfo.UpdateIDs();
            base.Add(key1, key2, item);
            Interlocked.Increment(ref InventorySerial);

            OnInventoryUpdate?.Invoke(item.UpdateInfo);
            OnChange?.Invoke(ChangeAction.Add, PartID, item.ID);
        }

        public void SetAssetID(UUID key1, UUID assetID)
        {
            ObjectPartInventoryItem item;
            if (TryGetValue(key1, out item))
            {
                item.AssetID = assetID;
                Interlocked.Increment(ref InventorySerial);

                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.Change, PartID, item.ID);
            }
        }

        public void SetNextOwnerAssetID(UUID key1, UUID assetID)
        {
            ObjectPartInventoryItem item;
            if(TryGetValue(key1, out item))
            {
                item.NextOwnerAssetID = assetID;
                Interlocked.Increment(ref InventorySerial);

                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.NextOwnerAssetID, PartID, item.ID);
            }
        }

        public new void ChangeKey(string newKey, string oldKey)
        {
            ObjectPartInventoryItem item;
            lock (m_DataLock)
            {
                base.ChangeKey(newKey, oldKey);
                item = base[newKey];
                item.Name = newKey;
            }
            Interlocked.Increment(ref InventorySerial);

            OnInventoryUpdate?.Invoke(item.UpdateInfo);
            OnChange?.Invoke(ChangeAction.Change, PartID, item.ID);
        }

        public bool Rename(string newKey, UUID itemid)
        {
            bool renamed = false;
            ObjectPartInventoryItem item;
            lock (m_DataLock)
            {
                if (base.TryGetValue(itemid, out item) && item.Name != newKey)
                {
                    base.ChangeKey(newKey, item.Name);
                    item.Name = newKey;
                    renamed = true;
                }
            }

            if (renamed)
            {
                Interlocked.Increment(ref InventorySerial);

                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.Change, PartID, item.ID);
            }
            return renamed;
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
                    newItem.SetNewID(UUID.Random);
                }
                Add(newItem, false);
            }
            script?.Remove();
            Interlocked.Increment(ref InventorySerial);
            oldItem.UpdateInfo.SetRemovedItem();
            OnInventoryUpdate?.Invoke(oldItem.UpdateInfo);
            OnInventoryUpdate?.Invoke(newItem.UpdateInfo);
            OnChange?.Invoke(ChangeAction.Add, PartID, newItem.ID);
        }

        public new bool Remove(UUID key1)
        {
            ObjectPartInventoryItem item;
            if (base.Remove(key1, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                script?.Remove();
                Interlocked.Increment(ref InventorySerial);
                item.UpdateInfo.SetRemovedItem();
                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.Remove, PartID, key1);
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
                script?.Remove();
                Interlocked.Increment(ref InventorySerial);
                item.UpdateInfo.SetRemovedItem();
                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.Remove, PartID, item.ID);
                return true;
            }
            return false;
        }

        public new bool Remove(UUID key1, out ObjectPartInventoryItem item)
        {
            if (base.Remove(key1, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                script?.Remove();
                Interlocked.Increment(ref InventorySerial);
                item.UpdateInfo.SetRemovedItem();
                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.Remove, PartID, item.ID);
                return true;
            }
            return false;
        }

        public new bool Remove(string key2, out ObjectPartInventoryItem item)
        {
            if (base.Remove(key2, out item))
            {
                ScriptInstance script = item.RemoveScriptInstance;
                script?.Remove();
                Interlocked.Increment(ref InventorySerial);
                item.UpdateInfo.SetRemovedItem();
                OnInventoryUpdate?.Invoke(item.UpdateInfo);
                OnChange?.Invoke(ChangeAction.Remove, PartID, item.ID);
                return true;
            }
            return false;
        }

        public new bool Remove(UUID key1, string key2)
        {
            throw new NotSupportedException("ObjectPartInventory.Remove(UUID, string)");
        }
        #endregion

        #region XML Deserialization
        private static void CollisionFilterFromXml(ObjectPartInventoryItem item, XmlTextReader reader)
        {
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "Name")
                        {
                            ObjectPartInventoryItem.CollisionFilterParam p = item.CollisionFilter;
                            p.Name = reader.ReadElementValueAsString();
                            item.CollisionFilter = p;
                        }
                        else if (reader.Name == "ID")
                        {
                            ObjectPartInventoryItem.CollisionFilterParam p = item.CollisionFilter;
                            string v = reader.ReadElementValueAsString();
                            if (!UUID.TryParse(v, out p.ID))
                            {
                                p.ID = UUID.Zero;
                            }
                            item.CollisionFilter = p;
                        }
                        else if (reader.Name == "Type")
                        {
                            ObjectPartInventoryItem.CollisionFilterParam p = item.CollisionFilter;
                            p.Type = (ObjectPartInventoryItem.CollisionFilterEnum)Enum.Parse(typeof(ObjectPartInventoryItem.CollisionFilterEnum), reader.ReadElementContentAsString());
                            item.CollisionFilter = p;
                        }
                        else
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "CollisionFilter")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;
                }
            }
            throw new InvalidObjectXmlException();
        }


        private ObjectPartInventoryItem FromXML(XmlTextReader reader, UGUI currentOwner, XmlDeserializationOptions options, out UUID origid)
        {
            origid = UUID.Zero;
            var item = new ObjectPartInventoryItem
            {
                Owner = currentOwner
            };
            var grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
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
                            case "CollisionFilter":
                                CollisionFilterFromXml(item, reader);
                                break;

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
                                try
                                {
                                    item.Creator.CreatorData = reader.ReadElementValueAsString();
                                }
                                catch(UriFormatException)
                                {
                                    /* ignore this if it fails */
                                }
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
                                origid = reader.ReadContentAsUUID();
                                item.SetNewID(origid);
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

                            case "ExperienceID":
                                item.ExperienceID = new UEI(reader.ReadContentAsString());
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
                            item.Owner = UGUI.Unknown;
                        }
                        return item;

                    default:
                        break;
                }
            }
        }

        public void FillFromXml(XmlTextReader reader, UGUI currentOwner, XmlDeserializationOptions options)
        {
            var part = new ObjectPart
            {
                Owner = currentOwner
            };
            if (reader.IsEmptyElement)
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
                                UUID origid;
                                ObjectPartInventoryItem item = FromXML(reader, currentOwner, options, out origid);
                                if(ContainsKey(item.ID))
                                {
                                    /* skip duplicate ids. There seems to be at least one OpenSim version generating bogus task inventory */
                                    break;
                                }
                                try
                                {
                                    Add(item, true);
                                }
                                catch
                                {
                                    throw new InvalidObjectXmlException(string.Format("Duplicate task inventory name {0} ({1})", item.Name, origid));
                                }
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
            ToXml(writer, UGUI.Unknown, options);
        }

        public void ToXml(XmlTextWriter writer, UGUI nextOwner, XmlSerializationOptions options)
        {
            writer.WriteNamedValue("InventorySerial", InventorySerial);
            writer.WriteStartElement("TaskInventory");
            {
                foreach(ObjectPartInventoryItem item in Values)
                {
                    writer.WriteStartElement("TaskInventoryItem");
                    {
                        writer.WriteUUID("AssetID", (options & XmlSerializationOptions.AdjustForNextOwner) != 0 ? item.NextOwnerAssetID : item.AssetID);
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
                        if ((options & XmlSerializationOptions.WriteOwnerInfo) != XmlSerializationOptions.None)
                        {
                            writer.WriteUUID("OwnerID", item.Owner.ID);
                            writer.WriteNamedValue("CurrentPermissions", (uint)item.Permissions.Current);
                        }
                        else if ((options & XmlSerializationOptions.AdjustForNextOwner) != XmlSerializationOptions.None)
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
                        var grantinfo = item.PermsGranter;
                        writer.WriteUUID("PermsGranter", grantinfo.PermsGranter.ID);
                        writer.WriteNamedValue("PermsMask", (uint)grantinfo.PermsMask);
                        writer.WriteNamedValue("Type", (int)item.AssetType);
                        writer.WriteNamedValue("OwnerChanged", (options & XmlSerializationOptions.AdjustForNextOwner) != XmlSerializationOptions.None);
                        UEI experienceID = item.ExperienceID;
                        if(experienceID != UEI.Unknown)
                        {
                            writer.WriteNamedValue("ExperienceID", experienceID.ToString());
                        }
                        {
                            ObjectPartInventoryItem.CollisionFilterParam p = item.CollisionFilter;
                            if (p.ID != UUID.Zero || p.Name?.Length != 0)
                            {
                                writer.WriteStartElement("CollisionFilter");
                                writer.WriteNamedValue("Name", p.Name);
                                writer.WriteNamedValue("ID", p.ID);
                                writer.WriteNamedValue("Type", p.Type.ToString());
                                writer.WriteEndElement();
                            }
                        }
                    }
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }
        #endregion
    }
}
