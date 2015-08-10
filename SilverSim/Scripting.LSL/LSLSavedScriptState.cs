// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SilverSim.Scripting.LSL
{
    public partial class Script
    {
        public class SavedScriptState : IScriptState
        {
            public Dictionary<string, object> Variables = new Dictionary<string, object>();
            public bool IsRunning = false;
            public string CurrentState = "default";

            static void ScriptPermissionsFromXML(XmlTextReader reader, ObjectPartInventoryItem item)
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
                                case "mask":
                                    uint mask = (uint)reader.ReadContentAsLong();
                                    item.PermsGranter.PermsMask = (ScriptPermissions)mask;
                                    break;

                                case "granter":
                                    item.PermsGranter.PermsGranter.ID = reader.ReadContentAsUUID();
                                    break;

                                default:
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Permissions")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return;

                        default:
                            break;
                    }
                }
            }

            static void ListItemFromXml(XmlTextReader reader, AnArray array)
            {
                string type = "";
                string attrname = "";
                while (reader.ReadAttributeValue())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Attribute:
                            attrname = reader.Value;
                            break;

                        case XmlNodeType.Text:
                            switch (attrname)
                            {
                                case "type":
                                    type = reader.Value;
                                    break;

                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (type == "")
                {
                    throw new InvalidObjectXmlException();
                }
                string vardata;

                switch (type)
                {
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                        vardata = reader.ReadElementValueAsString();
                        array.Add(Quaternion.Parse(vardata));
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                        vardata = reader.ReadElementValueAsString();
                        array.Add(Vector3.Parse(vardata));
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                        array.Add(reader.ReadElementValueAsInt());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                        array.Add(reader.ReadElementValueAsFloat());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                        array.Add(reader.ReadElementValueAsString());
                        break;

                    default:
                        throw new InvalidObjectXmlException();
                }
            }

            static AnArray ListFromXml(XmlTextReader reader)
            {
                AnArray array = new AnArray();
                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch(reader.Name)
                            {
                                case "ListItem":
                                    ListItemFromXml(reader, array);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "Variable")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return array;

                        default:
                            break;
                    }
                }
            }

            static void VariableFromXml(XmlTextReader reader, SavedScriptState state)
            {
                string type = "";
                string varname = "";
                if(reader.MoveToFirstAttribute())
                {
                    do
                    {
                        switch (reader.Name)
                        {
                            case "type":
                                type = reader.Value;
                                break;

                            case "name":
                                varname = reader.Value;
                                break;

                            default:
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                }

                if(varname == "" || type == "")
                {
                    throw new InvalidObjectXmlException();
                }
                string vardata;

                switch(type)
                {
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                        vardata = reader.ReadElementValueAsString();
                        state.Variables[varname] = Quaternion.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                        vardata = reader.ReadElementValueAsString();
                        state.Variables[varname] = Vector3.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                        vardata = reader.ReadElementValueAsString();
                        state.Variables[varname] = Vector3.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                        state.Variables[varname] = reader.ReadElementValueAsInt();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                        state.Variables[varname] = reader.ReadElementValueAsFloat();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                        state.Variables[varname] = reader.ReadElementValueAsString();
                        break;

                    case "list":
                        state.Variables[varname] = ListFromXml(reader);
                        break;

                    default:
                        throw new InvalidObjectXmlException();
                }
            }
            static void VariablesFromXml(XmlTextReader reader, SavedScriptState state)
            {
                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.IsEmptyElement)
                            {
                                break;
                            }

                            VariableFromXml(reader, state);
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "Variables")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return;
                            
                        default:
                            break;
                    }
                }
            }

            static SavedScriptState ScriptStateFromXML(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
            {
                SavedScriptState state = new SavedScriptState();
                for (; ;)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch(reader.Name)
                            {
                                case "State":
                                    state.CurrentState = reader.ReadElementValueAsString();
                                    break;

                                case "Running":
                                    state.IsRunning = reader.ReadElementValueAsBoolean();
                                    break;

                                case "Variables":
                                    VariablesFromXml(reader, state);
                                    break;

                                case "Queue":
#warning TODO: Implement queue deserialization for LSL
                                    reader.ReadToEndElement();
                                    break;

                                case "Plugins":
#warning TODO: Implement Plugins deserialization for LSL
                                    reader.ReadToEndElement();
                                    break;

                                case "Permissions":
                                    ScriptPermissionsFromXML(reader, item);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "ScriptState")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return state;
                    }
                }
            }

            public static SavedScriptState FromXML(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
            {
                SavedScriptState state = new SavedScriptState();
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
                                case "ScriptState":
                                    state = ScriptStateFromXML(reader, attrs, item);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "State")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return state;
                    }
                }
            }

            public void ToXml(XmlTextWriter writer)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
