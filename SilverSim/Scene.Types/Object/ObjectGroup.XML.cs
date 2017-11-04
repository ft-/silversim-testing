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
using SilverSim.Types;
using System.Collections.Generic;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        #region XML Serialization
        public void ToXml(XmlTextWriter writer, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, UUI.Unknown, Vector3.Zero, options, false);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, nextOwner, Vector3.Zero, options, false);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, Vector3 offsetpos, XmlSerializationOptions options = XmlSerializationOptions.None, bool writeOffsetPos = true)
        {
            List<ObjectPart> parts = Values;
            writer.WriteStartElement("SceneObjectGroup");
            if (writeOffsetPos)
            {
                Vector3 opos = Position - offsetpos;
                writer.WriteAttributeString("x", opos.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("y", opos.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("z", opos.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if ((options & XmlSerializationOptions.WriteXml2) == 0)
            {
                writer.WriteStartElement("RootPart");
            }
            RootPart.ToXml(writer, options);
            if ((options & XmlSerializationOptions.WriteXml2) == 0)
            {
                writer.WriteEndElement();
            }
            writer.WriteStartElement("OtherParts");
            foreach (ObjectPart p in parts)
            {
                if (p.ID != RootPart.ID)
                {
                    if ((options & XmlSerializationOptions.WriteXml2) == 0)
                    {
                        writer.WriteStartElement("Part");
                    }
                    p.ToXml(writer, nextOwner, options);
                    if ((options & XmlSerializationOptions.WriteXml2) == 0)
                    {
                        writer.WriteEndElement();
                    }
                }
            }
            writer.WriteEndElement();

            bool haveScriptState = false;
            foreach (ObjectPart p in parts)
            {
                foreach (ObjectPartInventoryItem i in p.Inventory.Values)
                {
                    IScriptState scriptState = i.ScriptState;
                    if (scriptState != null)
                    {
                        if (!haveScriptState)
                        {
                            writer.WriteStartElement("GroupScriptStates");
                            haveScriptState = true;
                        }

                        writer.WriteStartElement("SavedScriptState");
                        writer.WriteAttributeString("UUID", i.ID.ToString());

                        scriptState.ToXml(writer);

                        writer.WriteEndElement();
                    }
                }
            }
            if (haveScriptState)
            {
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        #endregion

        #region XML Deserialization
        private static ObjectPart ParseOtherPart(XmlTextReader reader, ObjectGroup group, UUI currentOwner, XmlDeserializationOptions options)
        {
            ObjectPart otherPart = null;
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

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "SceneObjectPart":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (otherPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                otherPart = ObjectPart.FromXml(reader, null, currentOwner, options);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "Part")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return otherPart;

                    default:
                        break;
                }
            }
        }

        private static void FromXmlOtherParts(XmlTextReader reader, ObjectGroup group, UUI currentOwner, XmlDeserializationOptions options)
        {
            ObjectPart part;
            var links = new SortedDictionary<int, ObjectPart>();
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

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "Part":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                part = ParseOtherPart(reader, group, currentOwner, options);
                                links.Add(part.LoadedLinkNumber, part);
                                break;

                            case "SceneObjectPart":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                part = ObjectPart.FromXml(reader, null, currentOwner, options);
                                try
                                {
                                    part.LoadedLinkNumber = links.Count + 2;
                                    links.Add(part.LoadedLinkNumber, part);
                                }
                                catch
                                {
                                    throw new ObjectDeserializationFailedDueKeyException();
                                }
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "OtherParts")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        foreach (KeyValuePair<int, ObjectPart> kvp in links)
                        {
                            group.Add(kvp.Key, kvp.Value.ID, kvp.Value);
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static ObjectPart ParseRootPart(XmlTextReader reader, ObjectGroup group, UUI currentOwner, XmlDeserializationOptions options)
        {
            ObjectPart rootPart = null;
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

                string nodeName = reader.Name;
                bool isEmptyElement = reader.IsEmptyElement;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "SceneObjectPart":
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ObjectPart.FromXml(reader, group, currentOwner, options);
                                group.Add(LINK_ROOT, rootPart.ID, rootPart);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "RootPart")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return rootPart;

                    default:
                        break;
                }
            }
        }

        public static ObjectGroup FromXml(XmlTextReader reader, UUI currentOwner, XmlDeserializationOptions options) =>
            FromXml(reader, currentOwner, options: options);

        public static ObjectGroup FromXml(XmlTextReader reader, UUI currentOwner, bool inRootPart = false, XmlDeserializationOptions options = XmlDeserializationOptions.None)
        {
            var group = new ObjectGroup();
            ObjectPart rootPart = null;
            if (reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for (; ; )
            {
                if (inRootPart)
                {
                    inRootPart = false;
                }
                else if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "RootPart":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ParseRootPart(reader, group, currentOwner, options);
                                break;

                            case "SceneObjectPart":
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ObjectPart.FromXml(reader, group, currentOwner, options);
                                group.Add(LINK_ROOT, rootPart.ID, rootPart);
                                break;

                            case "OtherParts":
                                if (isEmptyElement)
                                {
                                    break;
                                }
                                FromXmlOtherParts(reader, group, currentOwner, options);
                                break;

                            case "GroupScriptStates":
                                if (isEmptyElement)
                                {
                                    break;
                                }
                                FromXmlGroupScriptStates(reader, group);
                                break;

                            case "KeyframeMotion":
                                if (isEmptyElement)
                                {
                                    break;
                                }
                                reader.ReadToEndElement();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "SceneObjectGroup")
                        {
                            throw new InvalidObjectXmlException();
                        }

                        foreach (ObjectPart part in group.Values)
                        {
                            part.Owner = currentOwner;
                            if ((options & XmlDeserializationOptions.RestoreIDs) == 0)
                            {
                                foreach (UUID key in part.Inventory.Keys1)
                                {
                                    UUID newid = UUID.Random;
                                    part.Inventory[key].SetNewID(newid);
                                    part.Inventory.ChangeKey(newid, key);
                                }
                            }
                        }
                        group.FinalizeObject();
                        return group;

                    default:
                        break;
                }
            }
        }

        public void FinalizeObject()
        {
            ObjectPart rootPart = RootPart;

            foreach (ObjectPart part in Values)
            {
                /* make those parameters align well */
                part.IsPhantom = rootPart.IsPhantom;
                part.IsPhysics = rootPart.IsPhysics;
                part.IsVolumeDetect = rootPart.IsVolumeDetect;

                part.ObjectGroup = this;
                part.UpdateData(ObjectPart.UpdateDataFlags.All);
            }
        }

        private static void FromXmlGroupScriptStates(XmlTextReader reader, ObjectGroup group)
        {
            var itemID = UUID.Zero;

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (isEmptyElement)
                        {
                            break;
                        }
                        switch (nodeName)
                        {
                            case "SavedScriptState":
                                itemID = UUID.Zero;
                                if (reader.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        switch (reader.Name)
                                        {
                                            case "UUID":
                                                if (!UUID.TryParse(reader.Value, out itemID))
                                                {
                                                    throw new InvalidObjectXmlException();
                                                }
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                    while (reader.MoveToNextAttribute());
                                }

                                FromXmlSavedScriptState(reader, group, itemID);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "GroupScriptStates")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static void FromXmlSavedScriptStateInner(XmlTextReader reader, ObjectGroup group, UUID itemID)
        {
            string tagname = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;
            var attrs = new Dictionary<string, string>();
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    attrs[reader.Name] = reader.Value;
                } while (reader.MoveToNextAttribute());
            }
            ObjectPartInventoryItem item = null;

            if (!attrs.ContainsKey("Asset") || !attrs.ContainsKey("Engine"))
            {
                if (!isEmptyElement)
                {
                    reader.ReadToEndElement(tagname);
                }
                return;
            }

            foreach (ObjectPart part in group.Values)
            {
                if (part.Inventory.ContainsKey(itemID))
                {
                    item = part.Inventory[itemID];
                    UUID assetid;

                    /* validate inventory item */
                    if (!UUID.TryParse(attrs["Asset"], out assetid) ||
                        item.AssetType != SilverSim.Types.Asset.AssetType.LSLText ||
                        item.InventoryType != SilverSim.Types.Inventory.InventoryType.LSL ||
                        assetid != item.AssetID)
                    {
                        item = null;
                    }
                    break;
                }
            }

            if (item == null)
            {
                if (!isEmptyElement)
                {
                    reader.ReadToEndElement(tagname);
                }
            }
            else
            {
                IScriptCompiler compiler;
                try
                {
                    compiler = CompilerRegistry[attrs["Engine"]];
                }
                catch
                {
                    if (!isEmptyElement)
                    {
                        reader.ReadToEndElement(tagname);
                    }
                    return;
                }

                try
                {
                    item.ScriptState = compiler.StateFromXml(reader, attrs, item);
                }
                catch (ScriptStateLoaderNotImplementedException)
                {
                    if (!isEmptyElement)
                    {
                        reader.ReadToEndElement(tagname);
                    }
                    return;
                }
            }
        }

        private static void FromXmlSavedScriptState(XmlTextReader reader, ObjectGroup group, UUID itemID)
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
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "State":
                                FromXmlSavedScriptStateInner(reader, group, itemID);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "SavedScriptState")
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
    }
}
