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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Scripting.Common.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.LSL
{
    [CompilerUsesRunAndCollectMode]
    public partial class LSLCompiler : IScriptCompiler, IPlugin, IPluginSubFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LSL COMPILER");
        List<IScriptApi> m_Apis = new List<IScriptApi>();
        Dictionary<string, APIFlags> m_Constants = new Dictionary<string, APIFlags>();
        List<KeyValuePair<IScriptApi, MethodInfo>> m_Methods = new List<KeyValuePair<IScriptApi,MethodInfo>>();
        Dictionary<string, MethodInfo> m_EventDelegates = new Dictionary<string, MethodInfo>();
        List<Script.StateChangeEventDelegate> m_StateChangeDelegates = new List<ScriptInstance.StateChangeEventDelegate>();
        List<Script.ScriptResetEventDelegate> m_ScriptResetDelegates = new List<ScriptInstance.ScriptResetEventDelegate>();
        List<string> m_ReservedWords = new List<string>();
        List<string> m_MethodNames = new List<string>();
        List<char> m_SingleOps = new List<char>();
        List<char> m_MultiOps = new List<char>();
        List<char> m_NumericChars = new List<char>();
        List<char> m_OpChars = new List<char>();
        Resolver m_Resolver;

        class LineInfo
        {
            public readonly List<string> Line;
            public readonly int LineNumber;

            public LineInfo(List<string> line, int lineNo)
            {
                Line = line;
                LineNumber = lineNo;
            }
        }

        enum ControlFlowType
        {
            Entry,
            UnconditionalBlock,
            If,
            Else,
            ElseIf,
            For,
            DoWhile,
            While
        }

        class ControlFlowElement
        {
            public bool IsExplicitBlock;
            public bool PopNextImplicit = false;
            public ControlFlowType Type;
            public Label? LoopLabel;
            public Label? EndOfControlFlowLabel;
            public Label? EndOfIfFlowLabel;

            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, bool popOneImplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                PopNextImplicit = popOneImplicit;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, Label eoiflabel)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                EndOfIfFlowLabel = eoiflabel;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, Label eoiflabel, bool popOneImplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                EndOfIfFlowLabel = eoiflabel;
                PopNextImplicit = popOneImplicit;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
            }
        }

        class CompileState
        {
            public bool EmitDebugSymbols = false;
            public APIFlags AcceptedFlags;
            public Dictionary<string, MethodBuilder> m_FunctionInfo = new Dictionary<string, MethodBuilder>();
            public Dictionary<string, Type> m_VariableDeclarations = new Dictionary<string, Type>();
            public Dictionary<string, FieldBuilder> m_VariableFieldInfo = new Dictionary<string, FieldBuilder>();
            public Dictionary<string, LineInfo> m_VariableInitValues = new Dictionary<string, LineInfo>();
            public List<List<string>> m_LocalVariables = new List<List<string>>();
            public Dictionary<string, List<LineInfo>> m_Functions = new Dictionary<string, List<LineInfo>>();
            public Dictionary<string, Dictionary<string, List<LineInfo>>> m_States = new Dictionary<string, Dictionary<string, List<LineInfo>>>();
            public FieldBuilder InstanceField;
            public Dictionary<string, FieldBuilder> m_ApiFieldInfo = new Dictionary<string, FieldBuilder>();
            List<ControlFlowElement> m_ControlFlowStack = new List<ControlFlowElement>();
            public ControlFlowElement LastBlock = null;

            public void InitControlFlow()
            {
                m_ControlFlowStack.Clear();
                PushControlFlow(new ControlFlowElement(ControlFlowType.Entry, true));
            }

            public void PushControlFlow(ControlFlowElement e)
            {
                m_ControlFlowStack.Insert(0, e);
                LastBlock = null;
            }

            public string GetControlFlowInfo(int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                switch(m_ControlFlowStack[0].Type)
                {
                    case ControlFlowType.Entry: return "function entry";
                    case ControlFlowType.If: return "if";
                    case ControlFlowType.Else: return "else";
                    case ControlFlowType.ElseIf: return "else if";
                    case ControlFlowType.For: return "for";
                    case ControlFlowType.DoWhile: return "do ... while";
                    case ControlFlowType.While: return "while";
                    default: throw new ArgumentException(m_ControlFlowStack[0].Type.ToString());
                }
            }

            public bool IsImplicitControlFlow(int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                return !m_ControlFlowStack[0].IsExplicitBlock;
            }

            public void PopControlFlowImplicit(ILGenerator ilgen, int lineNumber)
            {
                if(m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else if(!m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    if(null != elem.EndOfIfFlowLabel)
                    {
                        ilgen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }
            }

            public void PopControlFlowImplicits(ILGenerator ilgen, int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else while (!m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    if (null != elem.EndOfIfFlowLabel)
                    {
                        ilgen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }
            }

            public ControlFlowElement PopControlFlowExplicit(ILGenerator ilgen, int lineNumber)
            {
                while (m_ControlFlowStack.Count != 0 && !m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    if (null != elem.EndOfIfFlowLabel)
                    {
                        ilgen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }

                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    if (null != elem.EndOfIfFlowLabel)
                    {
                        ilgen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                        elem.EndOfControlFlowLabel = null;
                    }
                    return elem;
                }
            }

            public CompileState()
            {

            }
        }

        public LSLCompiler()
        {
            m_ReservedWords.Add("integer");
            m_ReservedWords.Add("vector");
            m_ReservedWords.Add("list");
            m_ReservedWords.Add("float");
            m_ReservedWords.Add("string");
            m_ReservedWords.Add("key");
            m_ReservedWords.Add("rotation");
            m_ReservedWords.Add("if");
            m_ReservedWords.Add("while");
            m_ReservedWords.Add("jump");
            m_ReservedWords.Add("for");
            m_ReservedWords.Add("do");
            m_ReservedWords.Add("return");
            m_ReservedWords.Add("state");
            m_ReservedWords.Add("void");
            m_ReservedWords.Add("quaternion");

            m_MultiOps.Add('+');
            m_MultiOps.Add('-');
            m_MultiOps.Add('*');
            m_MultiOps.Add('/');
            m_MultiOps.Add('%');
            m_MultiOps.Add('<');
            m_MultiOps.Add('>');
            m_MultiOps.Add('=');
            m_MultiOps.Add('&');
            m_MultiOps.Add('|');
            m_MultiOps.Add('^');

            m_SingleOps.Add('~');
            m_SingleOps.Add('.');
            m_SingleOps.Add('(');
            m_SingleOps.Add(')');
            m_SingleOps.Add('[');
            m_SingleOps.Add(']');
            m_SingleOps.Add('!');
            m_SingleOps.Add(',');
            m_SingleOps.Add('@');

            m_NumericChars.Add('.');
            m_NumericChars.Add('A');
            m_NumericChars.Add('B');
            m_NumericChars.Add('C');
            m_NumericChars.Add('D');
            m_NumericChars.Add('E');
            m_NumericChars.Add('F');
            m_NumericChars.Add('a');
            m_NumericChars.Add('b');
            m_NumericChars.Add('c');
            m_NumericChars.Add('d');
            m_NumericChars.Add('e');
            m_NumericChars.Add('f');
            m_NumericChars.Add('x');
            m_NumericChars.Add('+');
            m_NumericChars.Add('-');

            m_OpChars = new List<char>();
            m_OpChars.AddRange(m_MultiOps);
            m_OpChars.AddRange(m_SingleOps);
        }

        public void AddPlugins(ConfigurationLoader loader)
        {
            loader.AddPlugin("LSLHTTP", new LSLHTTP());
            loader.AddPlugin("LSLHttpClient", new LSLHTTPClient_RequestQueue());
            Type[] types = GetType().Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (typeof(IScriptApi).IsAssignableFrom(type))
                {
                    System.Attribute scriptApiAttr = System.Attribute.GetCustomAttribute(type, typeof(ScriptApiName));
                    System.Attribute impTagAttr = System.Attribute.GetCustomAttribute(type, typeof(LSLImplementation));
                    if (null != impTagAttr && null != scriptApiAttr)
                    {
                        IPlugin factory = (IPlugin)Activator.CreateInstance(type);
                        loader.AddPlugin("LSL_API_" + ((ScriptApiName)scriptApiAttr).Name, factory);
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            List<IScriptApi> apis = loader.GetServicesByValue<IScriptApi>();
            foreach (IScriptApi api in apis)
            {
                System.Attribute attr = System.Attribute.GetCustomAttribute(api.GetType(), typeof(LSLImplementation));
                if(attr != null && !m_Apis.Contains(api))
                {
                    m_Apis.Add(api);
                }
            }

            foreach (IScriptApi api in apis)
            {
                #region Collect constants
                foreach (FieldInfo f in api.GetType().GetFields())
                {
                    APILevel attr = (APILevel) System.Attribute.GetCustomAttribute(f, typeof(APILevel));
                    if(attr != null)
                    {
                        if ((f.Attributes & FieldAttributes.Static) != 0)
                        {
                            if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                            {
                                if (IsValidType(f.FieldType))
                                {
                                    try
                                    {
                                        m_Constants.Add(f.Name, attr.Flags);
                                    }
                                    catch
                                    {
                                        m_Constants[f.Name] |= attr.Flags;
                                    }
                                }
                                else
                                {
                                    m_Log.DebugFormat("Invalid constant '{0}' in '{1}' has APILevel attribute. It does not have LSL compatible type '{2}'.",
                                        f.Name,
                                        f.DeclaringType.FullName,
                                        f.FieldType.FullName);
                                }
                            }
                            else
                            {
                                m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                            }
                        }
                    }
                }
                #endregion

                #region Collect event definitions
                foreach (Type t in api.GetType().GetNestedTypes(BindingFlags.Public).Where(t => t.BaseType == typeof(MulticastDelegate)))
                {
                    System.Attribute attr = System.Attribute.GetCustomAttribute(t, typeof(APILevel));
                    if(attr != null)
                    {
                        MethodInfo mi = t.GetMethod("Invoke");
                        ParameterInfo[] pi = mi.GetParameters();
                        /* validate parameters */
                        bool eventValid = true;
                        for (int i = 0; i < pi.Length; ++i)
                        {
                            if (!IsValidType(pi[i].ParameterType))
                            {
                                eventValid = false;
                                m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has APILevel attribute. Parameter '{2}' does not have LSL compatible type '{3}'.",
                                    mi.Name,
                                    mi.DeclaringType.FullName,
                                    pi[i].Name,
                                    pi[i].ParameterType.FullName);
                            }
                        }
                        if (mi.ReturnType != typeof(void))
                        {
                            eventValid = false;
                            m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has APILevel attribute. Return value is not void. Found: '{2}'",
                                mi.Name,
                                mi.DeclaringType.FullName,
                                mi.ReturnType.FullName);
                        }

                        if (eventValid)
                        {
                            m_EventDelegates.Add(t.Name, mi);
                        }
                    }
                }
                #endregion

                #region Collect API functions, reset delegates and state change delegates
                foreach (MethodInfo m in api.GetType().GetMethods())
                {
                    System.Attribute attr = System.Attribute.GetCustomAttribute(m, typeof(APILevel));
                    if(attr != null)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if (pi.Length >= 1)
                        {
                            if (pi[0].ParameterType.Equals(typeof(ScriptInstance)))
                            {
                                /* validate parameters */
                                bool methodValid = true;
                                if((m.Attributes & MethodAttributes.Static) != 0)
                                {
                                    methodValid = false;
                                    m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel attribute. Method is declared static.",
                                        m.Name,
                                        m.DeclaringType.FullName);
                                }
                                for (int i = 1; i < pi.Length; ++i)
                                {
                                    if(!IsValidType(pi[i].ParameterType))
                                    {
                                        methodValid = false;
                                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel attribute. Parameter '{2}' does not have LSL compatible type '{3}'.",
                                            m.Name,
                                            m.DeclaringType.FullName,
                                            pi[i].Name,
                                            pi[i].ParameterType.FullName);
                                    }
                                }
                                if (!IsValidType(m.ReturnType))
                                {
                                    methodValid = false;
                                    m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel attribute. Return value does not have LSL compatible type '{2}'.",
                                        m.Name,
                                        m.DeclaringType.FullName,
                                        m.ReturnType.FullName);
                                }

                                if (methodValid)
                                {
                                    m_Methods.Add(new KeyValuePair<IScriptApi, MethodInfo>(api, m));
                                    if (!m_MethodNames.Contains(m.Name))
                                    {
                                        m_MethodNames.Add(m.Name);
                                    }
                                }
                            }
                        }
                    }

                    attr = System.Attribute.GetCustomAttribute(m, typeof(ExecutedOnStateChange));
                    if (attr != null && (m.Attributes & MethodAttributes.Static) != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if(pi.Length != 1)
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Parameter count does not match.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else if (m.ReturnType != typeof(void))
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Return type is not void.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else if(pi[0].ParameterType != typeof(ScriptInstance))
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Parameter type is not ScriptInstance.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else
                        {
                            Delegate d = Delegate.CreateDelegate(typeof(Script.StateChangeEventDelegate), null, m);
                            m_StateChangeDelegates.Add((Script.StateChangeEventDelegate)d);
                        }
                    }

                    attr = System.Attribute.GetCustomAttribute(m, typeof(ExecutedOnScriptReset));
                    if (attr != null && (m.Attributes & MethodAttributes.Static) != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if(pi.Length != 1)
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Parameter count does not match.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else if (m.ReturnType != typeof(void))
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Return type is not void.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else if(pi[0].ParameterType != typeof(ScriptInstance))
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Parameter type is not ScriptInstance.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else
                        {
                            Delegate d = Delegate.CreateDelegate(typeof(Script.ScriptResetEventDelegate), null, m);
                            m_ScriptResetDelegates.Add((Script.ScriptResetEventDelegate)d);
                        }
                    }
                }
                #endregion
            }

            List<Dictionary<string, Resolver.OperatorType>> operators = new List<Dictionary<string, Resolver.OperatorType>>();
            Dictionary<string, string> blockOps = new Dictionary<string, string>();
            blockOps.Add("(", ")");
            blockOps.Add("[", "]");

            Dictionary<string, Resolver.OperatorType> plist;
            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("@", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("++", Resolver.OperatorType.RightUnary);
            plist.Add("--", Resolver.OperatorType.RightUnary);
            plist.Add(".", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("++", Resolver.OperatorType.LeftUnary);
            plist.Add("--", Resolver.OperatorType.LeftUnary);
            plist.Add("+", Resolver.OperatorType.LeftUnary);
            plist.Add("-", Resolver.OperatorType.LeftUnary);
            plist.Add("!", Resolver.OperatorType.LeftUnary);
            plist.Add("~", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("*", Resolver.OperatorType.Binary);
            plist.Add("/", Resolver.OperatorType.Binary);
            plist.Add("%", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("+", Resolver.OperatorType.Binary);
            plist.Add("-", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("<<", Resolver.OperatorType.Binary);
            plist.Add(">>", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("<", Resolver.OperatorType.Binary);
            plist.Add("<=", Resolver.OperatorType.Binary);
            plist.Add(">", Resolver.OperatorType.Binary);
            plist.Add(">=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("==", Resolver.OperatorType.Binary);
            plist.Add("!=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("&", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("^", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("|", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("&&", Resolver.OperatorType.Binary);
            plist.Add("||", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("=", Resolver.OperatorType.Binary);
            plist.Add("+=", Resolver.OperatorType.Binary);
            plist.Add("-=", Resolver.OperatorType.Binary);
            plist.Add("*=", Resolver.OperatorType.Binary);
            plist.Add("/=", Resolver.OperatorType.Binary);
            plist.Add("%=", Resolver.OperatorType.Binary);

            m_Resolver = new Resolver(m_ReservedWords, operators, blockOps);

            GenerateLSLSyntaxFile();

            SilverSim.Scripting.Common.CompilerRegistry.ScriptCompilers["lsl"] = this;
            SilverSim.Scripting.Common.CompilerRegistry.ScriptCompilers["XEngine"] = this; /* we won't be supporting anything beyond LSL compatibility */
        }

        public void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            Preprocess(user, shbangs, reader, linenumber);
        }

        void WriteIndent(TextWriter writer, int indent)
        {
            while(indent-- > 0)
            {
                writer.Write("    ");
            }
        }

        void WriteIndented(TextWriter writer, string s, ref int oldIndent)
        {
            if (s == "[")
            {
                writer.WriteLine("\n");
                ++oldIndent;
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                WriteIndent(writer, oldIndent);
            }
            else if (s == "{")
            {
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                ++oldIndent;
                WriteIndent(writer, oldIndent);
            }
            else if (s == "]")
            {
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                --oldIndent;
                WriteIndent(writer, oldIndent);
            }
            else if ( s == "}")
            {
                --oldIndent;
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                WriteIndent(writer, oldIndent);
            }
            else if(s == "\n")
            {
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
            }
            else if(s == ";")
            {
                writer.WriteLine(";");
                WriteIndent(writer, oldIndent);
            }
            else
            {
                writer.Write(s + " ");
            }
        }

        void WriteIndented(TextWriter writer, List<string> list, ref int oldIndent)
        {
            foreach(string s in list)
            {
                WriteIndented(writer, s, ref oldIndent);
            }
        }

        void WriteIndented(TextWriter writer, string[] strarray, ref int oldIndent)
        {
            foreach(string s in strarray)
            {
                WriteIndented(writer, s, ref oldIndent);
            }
        }

        public void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            CompileState cs = Preprocess(user, shbangs, reader, linenumber);
            /* rewrite script */
            int indent = 0;
            using (TextWriter writer = new StreamWriter(s, new UTF8Encoding(false)))
            {
                #region Write Variables
                foreach (KeyValuePair<string, Type> kvp in cs.m_VariableDeclarations)
                {
                    LineInfo li;
                    WriteIndented(writer, MapType(kvp.Value), ref indent);
                    WriteIndented(writer, kvp.Key, ref indent);
                    if (cs.m_VariableInitValues.TryGetValue(kvp.Key, out li))
                    {
                        WriteIndented(writer, "=", ref indent);
                        WriteIndented(writer, li.Line, ref indent);
                    }
                    WriteIndented(writer, ";", ref indent);
                }
                WriteIndented(writer, "\n", ref indent);
                #endregion

                #region Write functions
                foreach(KeyValuePair<string, List<LineInfo>> kvp in cs.m_Functions)
                {
                    foreach(LineInfo li in kvp.Value)
                    {
                        WriteIndented(writer, li.Line, ref indent);
                        if (li.Line[li.Line.Count - 1] != "{" && li.Line[li.Line.Count - 1] != ";" && li.Line[li.Line.Count - 1] != "}")
                        {
                            ++indent;
                            WriteIndented(writer, "\n", ref indent);
                            --indent;
                        }
                    }
                }
                #endregion

                #region Write states
                foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> kvp in cs.m_States)
                {
                    if (kvp.Key != "default")
                    {
                        WriteIndented(writer, "state", ref indent);
                    }
                    WriteIndented(writer, kvp.Key, ref indent);
                    WriteIndented(writer, "{", ref indent);

                    foreach (KeyValuePair<string, List<LineInfo>> eventfn in kvp.Value)
                    {
                        int tempindent = 0;
                        foreach (LineInfo li in eventfn.Value)
                        {
                            WriteIndented(writer, li.Line, ref indent);
                            if (li.Line[li.Line.Count - 1] != "{" && li.Line[li.Line.Count - 1] != ";" && li.Line[li.Line.Count - 1] != "}")
                            {
                                ++tempindent;
                                indent += tempindent;
                                WriteIndented(writer, "\n", ref indent);
                                indent -= tempindent;
                            }
                            else
                            {
                                tempindent = 0;
                            }
                        }
                    }
                    WriteIndented(writer, "}", ref indent);
                }
                #endregion
                writer.Flush();
            }
        }

        public IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int lineNumber = 1)
        {
            CompileState compileState = Preprocess(user, shbangs, reader, lineNumber);
            return PostProcess(compileState, appDom, assetID, (compileState.AcceptedFlags & APIFlags.ASSL) == 0);
        }
    }
}
