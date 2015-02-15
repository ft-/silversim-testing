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
                        vardata = reader.ReadContentAsString();
                        array.Add(Quaternion.Parse(vardata));
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                        vardata = reader.ReadContentAsString();
                        array.Add(Vector3.Parse(vardata));
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                        array.Add(reader.ReadContentAsInt());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                        array.Add(reader.ReadContentAsFloat());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                        array.Add(reader.ReadContentAsString());
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
                                    reader.Skip();
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
                string attrname = "";
                string varname = "";
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

                                case "name":
                                    varname = reader.Value;
                                    break;

                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }

                if(varname == "" || type == "")
                {
                    throw new InvalidObjectXmlException();
                }
                string vardata;

                switch(type)
                {
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                        vardata = reader.ReadContentAsString();
                        state.Variables[varname] = Quaternion.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                        vardata = reader.ReadContentAsString();
                        state.Variables[varname] = Vector3.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                        state.Variables[varname] = reader.ReadContentAsInt();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                        state.Variables[varname] = reader.ReadContentAsFloat();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                        state.Variables[varname] = reader.ReadContentAsString();
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

            public static SavedScriptState FromXML(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
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
                                    state.CurrentState = reader.ReadContentAsString();
                                    break;

                                case "Running":
                                    state.IsRunning = reader.ReadContentAsBoolean();
                                    break;

                                case "Variables":
                                    reader.Skip();
                                    break;

                                case "Queue":
                                    reader.Skip();
                                    break;

                                case "Plugins":
                                    reader.Skip();
                                    break;

                                case "Permissions":
                                    ScriptPermissionsFromXML(reader, item);
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "State")
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
