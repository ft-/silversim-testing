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
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        void WriteSyntaxElement(XmlTextWriter writer, string elem, string tooltip)
        {
            writer.WriteNamedValue("key", elem);
            writer.WriteStartElement("map");
            writer.WriteNamedValue("key", "tooltip");
            writer.WriteNamedValue("string", tooltip);
            writer.WriteEndElement();
        }

        public UUID GetLSLSyntaxId()
        {
            return LSLSyntaxId;
        }

        public void WriteLSLSyntaxFile(Stream outstream)
        {
            outstream.Write(LSLSyntaxFile, 0, LSLSyntaxFile.Length);
        }

        static byte[] LSLSyntaxFile = new byte[0];
        static UUID LSLSyntaxId = UUID.Zero;

        public void GenerateLSLSyntaxFile()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter writer = new XmlTextWriter(ms, new UTF8Encoding(false)))
                {
                    writer.WriteStartElement("map");
                    {
                        #region Controls
                        writer.WriteNamedValue("key", "controls");
                        writer.WriteStartElement("map");
                        {
                            WriteSyntaxElement(writer, "do", "do / while loop\ndo {\n...\n} while (<condition>);");
                            WriteSyntaxElement(writer, "else", "if / else block\nif (<condition>) {\n...\n[} else [if (<condition>) {\n...]]\n}");
                            WriteSyntaxElement(writer, "for", "for loop\nfor (<initializer>; <condition>; <post-iteration-statement>)\n{ ...\n}");
                            WriteSyntaxElement(writer, "if", "if / else block\nif (<condition>) {\n...\n[} else [if (<condition>) {\n...]]\n}");
                            WriteSyntaxElement(writer, "jump", "jump statement\njump <label>");
                            WriteSyntaxElement(writer, "return", "Leave current event or function.\nreturn [<variable>];\nOptionally pass back a variable's value, from a function.");
                            WriteSyntaxElement(writer, "state", "state <target>\nIf the target state is not the same as the current one, change to the target state.");
                            WriteSyntaxElement(writer, "while", "while loop\nwhile (<condition>) {\n,,,\n}");
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Types
                        writer.WriteNamedValue("key", "types");
                        writer.WriteStartElement("map");
                        {
                            WriteSyntaxElement(writer, "float", "32 bit floating point value.\nThe range is 1.175494351E-38 to 3.402823466E+38.");
                            WriteSyntaxElement(writer, "integer", "32 bit integer value.\n-2,147,483,648 and +2,147,483,647 (that is 0x80000000 to 0x7FFFFFFF in hex).");
                            WriteSyntaxElement(writer, "key", "A 128 bit unique identifier (UUID).\nThe key is represented as hexidecimal characters (A-F and 0-9), grouped into sections (8,4,4,4,12 characters) and separated by hyphens (for a total of 36 characters). e.g. \"A822FF2B-FF02-461D-B45D-DCD10A2DE0C2\".");
                            WriteSyntaxElement(writer, "list", "A collection of other data types.\nLists are signified by square brackets surrounding their elements; the elements inside are separated by commas. e.g. [0, 1, 2, 3, 4] or [\"Yes\", \"No\", \"Perhaps\"].");
                            WriteSyntaxElement(writer, "quaternion", "The quaternion type is a left over from way back when LSL was created. It was later renamed to &lt;rotation&gt; to make it more user friendly, but it appears someone forgot to remove it ;-)");
                            WriteSyntaxElement(writer, "rotation", "The rotation type is one of several ways to represent an orientation in 3D.\nIt is a mathematical object called a quaternion. You can think of a quaternion as four numbers (x, y, z, w), three of which represent the direction an object is facing and a fourth that represents the object's banking left or right around that direction.");
                            WriteSyntaxElement(writer, "string", "Text data.\nThe editor accepts UTF-8 encoded text.");
                            WriteSyntaxElement(writer, "vector", "A vector is a data type that contains a set of three float values.\nVectors are used to represent colors (RGB), positions, and directions/velocities.");
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Constants
                        writer.WriteNamedValue("key", "constants");
                        writer.WriteStartElement("map");
                        {
                            foreach (IScriptApi api in m_Apis)
                            {
                                foreach (FieldInfo fi in api.GetType().GetFields())
                                {
                                    APILevel level = (APILevel)System.Attribute.GetCustomAttribute(fi, typeof(APILevel));
                                    if (level != null)
                                    {
                                        if (IsValidType(fi.FieldType))
                                        {
                                            writer.WriteNamedValue("key", fi.Name);
                                            writer.WriteStartElement("map");
                                            {
                                                writer.WriteNamedValue("key", "type");
                                                writer.WriteNamedValue("string", MapType(fi.FieldType));
                                                writer.WriteNamedValue("key", "value");
                                                writer.WriteNamedValue("string", fi.GetValue(null).ToString());
                                                LSLTooltip tooltip = (LSLTooltip)System.Attribute.GetCustomAttribute(fi, typeof(LSLTooltip));
                                                writer.WriteNamedValue("key", "tooltip");
                                                string avail = "Supported for";
                                                if ((level.Flags & APIFlags.LSL) != APIFlags.None)
                                                {
                                                    avail += " LSL";
                                                }
                                                if ((level.Flags & APIFlags.LightShare) != APIFlags.None)
                                                {
                                                    avail += " Lightshare";
                                                }
                                                if ((level.Flags & APIFlags.ASSL) != APIFlags.None)
                                                {
                                                    avail += " ASSL";
                                                }
                                                if ((level.Flags & APIFlags.ASSL_Admin) != APIFlags.None)
                                                {
                                                    avail += " Admin";
                                                }
                                                if ((level.Flags & APIFlags.WindLight_New) != APIFlags.None)
                                                {
                                                    avail += " WindLight_New";
                                                }
                                                if ((level.Flags & APIFlags.OSSL) != APIFlags.None)
                                                {
                                                    avail += " OSSL";
                                                }

                                                if (tooltip != null)
                                                {
                                                    writer.WriteNamedValue("string", tooltip.Tooltip + "\\n" + avail);
                                                }
                                                else
                                                {
                                                    writer.WriteNamedValue("string", fi.Name + "\\n" + avail);
                                                }
                                            }
                                            writer.WriteEndElement();
                                        }
                                    }
                                }
                            }
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Events
                        writer.WriteNamedValue("key", "events");
                        writer.WriteStartElement("map");
                        {
                            foreach (IScriptApi api in m_Apis)
                            {
                                foreach (Type t in api.GetType().GetNestedTypes(BindingFlags.Public).Where(t => t.BaseType == typeof(MulticastDelegate)))
                                {
                                    APILevel level = (APILevel)System.Attribute.GetCustomAttribute(t, typeof(APILevel));
                                    if (level != null && System.Attribute.GetCustomAttribute(t, typeof(StateEventDelegate)) != null)
                                    {
                                        MethodInfo mi = t.GetMethod("Invoke");
                                        writer.WriteNamedValue("key", t.Name);
                                        writer.WriteStartElement("map");
                                        {
                                            writer.WriteNamedValue("key", "arguments");
                                            writer.WriteStartElement("map");
                                            foreach (ParameterInfo pi in mi.GetParameters())
                                            {
                                                writer.WriteNamedValue("key", pi.Name);
                                                writer.WriteNamedValue("string", MapType(pi.ParameterType));
                                                LSLTooltip ptooltip = (LSLTooltip)System.Attribute.GetCustomAttribute(pi, typeof(LSLTooltip));
                                                writer.WriteNamedValue("key", "tooltip");
                                                if (null != ptooltip)
                                                {
                                                    writer.WriteNamedValue("string", ptooltip.Tooltip);
                                                }
                                                else
                                                {
                                                    writer.WriteStartElement("string");
                                                    writer.WriteEndElement();
                                                }
                                            }
                                            writer.WriteEndElement();

                                            LSLTooltip tooltip = (LSLTooltip)System.Attribute.GetCustomAttribute(mi, typeof(LSLTooltip));
                                            writer.WriteNamedValue("key", "tooltip");
                                            string avail = "Supported for";
                                            if ((level.Flags & APIFlags.LSL) != APIFlags.None)
                                            {
                                                avail += " LSL";
                                            }
                                            if ((level.Flags & APIFlags.LightShare) != APIFlags.None)
                                            {
                                                avail += " Lightshare";
                                            }
                                            if ((level.Flags & APIFlags.ASSL) != APIFlags.None)
                                            {
                                                avail += " ASSL";
                                            }
                                            if ((level.Flags & APIFlags.ASSL_Admin) != APIFlags.None)
                                            {
                                                avail += " Admin";
                                            }
                                            if ((level.Flags & APIFlags.WindLight_New) != APIFlags.None)
                                            {
                                                avail += " WindLight_New";
                                            }
                                            if ((level.Flags & APIFlags.OSSL) != APIFlags.None)
                                            {
                                                avail += " OSSL";
                                            }

                                            if (tooltip != null)
                                            {
                                                writer.WriteNamedValue("string", tooltip.Tooltip + "\\n" + avail);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("string", t.Name + "\\n" + avail);
                                            }
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                            }
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Functions
                        writer.WriteNamedValue("key", "functions");
                        writer.WriteStartElement("map");
                        {
                            foreach (IScriptApi api in m_Apis)
                            {
                                foreach (MethodInfo mi in api.GetType().GetMethods())
                                {
                                    APILevel level = (APILevel)System.Attribute.GetCustomAttribute(mi, typeof(APILevel));
                                    if (level != null)
                                    {
                                        writer.WriteNamedValue("key", mi.Name);
                                        writer.WriteStartElement("map");
                                        {
                                            EnergyUsage energy = (EnergyUsage)System.Attribute.GetCustomAttribute(mi, typeof(EnergyUsage));
                                            ForcedSleep forcedSleep = (ForcedSleep)System.Attribute.GetCustomAttribute(mi, typeof(ForcedSleep));
                                            writer.WriteNamedValue("key", "energy");
                                            if (null != energy)
                                            {
                                                writer.WriteNamedValue("real", energy.Energy);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("real", 10f);
                                            }
                                            writer.WriteNamedValue("key", "sleep");
                                            if (null != forcedSleep)
                                            {
                                                writer.WriteNamedValue("real", forcedSleep.Seconds);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("real", "0.0");
                                            }
                                            writer.WriteNamedValue("key", "return");
                                            writer.WriteNamedValue("string", MapType(mi.ReturnType));

                                            writer.WriteNamedValue("key", "arguments");
                                            writer.WriteStartElement("map");
                                            foreach (ParameterInfo pi in mi.GetParameters())
                                            {
                                                writer.WriteNamedValue("key", pi.Name);
                                                writer.WriteNamedValue("string", MapType(pi.ParameterType));
                                                LSLTooltip ptooltip = (LSLTooltip)System.Attribute.GetCustomAttribute(pi, typeof(LSLTooltip));
                                                writer.WriteNamedValue("key", "tooltip");
                                                if (null != ptooltip)
                                                {
                                                    writer.WriteNamedValue("string", ptooltip.Tooltip);
                                                }
                                                else
                                                {
                                                    writer.WriteStartElement("string");
                                                    writer.WriteEndElement();
                                                }
                                            }
                                            writer.WriteEndElement();

                                            LSLTooltip tooltip = (LSLTooltip)System.Attribute.GetCustomAttribute(mi, typeof(LSLTooltip));
                                            writer.WriteNamedValue("key", "tooltip");
                                            string avail = "Supported for";
                                            if ((level.Flags & APIFlags.LSL) != APIFlags.None)
                                            {
                                                avail += " LSL";
                                            }
                                            if ((level.Flags & APIFlags.LightShare) != APIFlags.None)
                                            {
                                                avail += " Lightshare";
                                            }
                                            if ((level.Flags & APIFlags.ASSL) != APIFlags.None)
                                            {
                                                avail += " ASSL";
                                            }
                                            if ((level.Flags & APIFlags.ASSL_Admin) != APIFlags.None)
                                            {
                                                avail += " Admin";
                                            }
                                            if ((level.Flags & APIFlags.WindLight_New) != APIFlags.None)
                                            {
                                                avail += " WindLight_New";
                                            }
                                            if ((level.Flags & APIFlags.OSSL) != APIFlags.None)
                                            {
                                                avail += " OSSL";
                                            }

                                            if (tooltip != null)
                                            {
                                                writer.WriteNamedValue("string", tooltip.Tooltip + "\\n" + avail);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("string", mi.Name + "\\n" + avail);
                                            }
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                            }
                        }
                        writer.WriteEndElement();
                        #endregion

                        writer.WriteNamedValue("key", "llsd-lsl-syntax-version");
                        writer.WriteNamedValue("integer", 2);
                    }
                    writer.WriteEndElement();

                    MD5 md5 = MD5.Create();
                    LSLSyntaxFile = ms.GetBuffer();
                    byte[] hash = md5.ComputeHash(LSLSyntaxFile);
                    LSLSyntaxId = new UUID(hash, 0);
                }
            }
        }
    }
}
