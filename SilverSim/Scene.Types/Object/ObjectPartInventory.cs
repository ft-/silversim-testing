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

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectPartInventory : RwLockedSortedDoubleDictionary<UUID, string, ObjectPartInventoryItem>
    {
        public delegate void OnChangeDelegate();
        public event OnChangeDelegate OnChange;

        public int InventorySerial = 1;

        public ObjectPartInventory()
        {
        }

        #region LSL style accessors
        public ObjectPartInventoryItem this[InventoryType type, uint index]
        {
            get
            {
                foreach (ObjectPartInventoryItem item in ValuesByKey2)
                {
                    if(type == item.InventoryType)
                    {
                        if(index-- == 0)
                        {
                            return item;
                        }
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
                        if(script != null)
                        {
                            if(script.IsRunning)
                            {
                                ++n;
                            }
                        }
                    }
                }

                return n;
            }
        }

        #endregion

        public void Add(ObjectPartInventoryItem item, bool nameChangeAllowed = true)
        {
            lock(this)
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
                foreach (OnChangeDelegate d in addDelegate.GetInvocationList())
                {
                    d();
                }
            }
        }

        public new void ChangeKey(string newKey, string oldKey)
        {
            base.ChangeKey(newKey, oldKey);
            Interlocked.Increment(ref InventorySerial);

            var updateDelegate = OnChange;
            if(updateDelegate != null)
            {
                foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                {
                    d();
                }
            }
        }

        public void Replace(string name, ObjectPartInventoryItem newItem)
        {
            ObjectPartInventoryItem oldItem;
            newItem.Name = name;
            lock(this)
            {
                oldItem = this[name];
                Remove(name);
                if(ContainsKey(newItem.ID))
                {
                    newItem.ID = UUID.Random;
                }
                Add(newItem, false);
            }
            if(oldItem != null)
            {
                oldItem.Dispose();
            }
            Interlocked.Increment(ref InventorySerial);
            var updateDelegate = OnChange;
            if (updateDelegate != null)
            {
                foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                {
                    d();
                }
            }
        }

        public new bool Remove(UUID key1)
        {
            if (base.Remove(key1))
            {
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                    {
                        d();
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(string key2)
        {
            if (base.Remove(key2))
            {
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                    {
                        d();
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
                Interlocked.Increment(ref InventorySerial);
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                    {
                        d();
                    }
                }
                return true;
            }
            return false;
        }
        #endregion

        #region XML Deserialization
        ObjectPartInventoryItem fromXML(XmlTextReader reader, UUI currentOwner)
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
                                item.Flags = reader.ReadElementValueAsUInt();
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

                            case "OwnerID":
                                /* Do not trust this ever! */
                                reader.ReadToEndElement();
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
                                Add(fromXML(reader, currentOwner), false);
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
