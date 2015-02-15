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
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using SilverSim.Scripting.Common;
using SilverSim.Scripting.Common.Expression;
using System.Text;

namespace SilverSim.Scripting.LSL
{
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
        Resolver m_Resolver;

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
        }

        public void AddPlugins(ConfigurationLoader loader)
        {
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
                        loader.AddPlugin(((ScriptApiName)scriptApiAttr).Name, factory);
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
                foreach (FieldInfo f in api.GetType().GetFields())
                {
                    APILevel attr = (APILevel) System.Attribute.GetCustomAttribute(f, typeof(APILevel));
                    if(attr != null)
                    {
                        if ((f.Attributes & FieldAttributes.Static) != 0)
                        {
                            if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
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
                                m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                            }
                        }
                    }
                }

                foreach(Type t in api.GetType().GetNestedTypes(BindingFlags.Public).Where(t => t.BaseType == typeof(MulticastDelegate)))
                {
                    System.Attribute attr = System.Attribute.GetCustomAttribute(t, typeof(APILevel));
                    if(attr != null)
                    {
                        m_EventDelegates.Add(t.Name, t.GetMethod("Invoke"));
                    }
                }

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
                                m_Methods.Add(new KeyValuePair<IScriptApi,MethodInfo>(api, m));
                                if(!m_MethodNames.Contains(m.Name))
                                {
                                    m_MethodNames.Add(m.Name);
                                }
                            }
                        }
                    }

                    attr = System.Attribute.GetCustomAttribute(m, typeof(ExecutedOnStateChange));
                    if (attr != null && (m.Attributes & MethodAttributes.Static) != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if(pi.Length == 1)
                        {
                            if(pi[0].ParameterType.Equals(typeof(ScriptInstance)))
                            {
                                Delegate d = Delegate.CreateDelegate(typeof(Script.StateChangeEventDelegate), null, m);
                                m_StateChangeDelegates.Add((Script.StateChangeEventDelegate)d);
                            }
                        }
                    }

                    attr = System.Attribute.GetCustomAttribute(m, typeof(ExecutedOnScriptReset));
                    if (attr != null && (m.Attributes & MethodAttributes.Static) != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if (pi.Length == 1)
                        {
                            if (pi[0].ParameterType.Equals(typeof(ScriptInstance)))
                            {
                                Delegate d = Delegate.CreateDelegate(typeof(Script.ScriptResetEventDelegate), null, m);
                                m_ScriptResetDelegates.Add((Script.ScriptResetEventDelegate)d);
                            }
                        }
                    }
                }
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

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("=", Resolver.OperatorType.Binary);
            plist.Add("+=", Resolver.OperatorType.Binary);
            plist.Add("-=", Resolver.OperatorType.Binary);
            plist.Add("*=", Resolver.OperatorType.Binary);
            plist.Add("/=", Resolver.OperatorType.Binary);
            plist.Add("%=", Resolver.OperatorType.Binary);
            plist.Add("&=", Resolver.OperatorType.Binary);
            plist.Add("^=", Resolver.OperatorType.Binary);
            plist.Add("|=", Resolver.OperatorType.Binary);
            plist.Add("<<=", Resolver.OperatorType.Binary);
            plist.Add(">>=", Resolver.OperatorType.Binary);

            m_Resolver = new Resolver(m_ReservedWords, operators, blockOps);

#if TEST_CODE
            #region Test Code
            string test = "test(vector a, float b, string c, integer d, list e, key f) {" +
                    "{llSay(PUBLIC_CHANNEL, \"Hello\");}" +
                    "integer f;\n" +
                    "}\n" +
                    "default {\n" +
                    "state_entry()\n{\n" +
                    "test(\n);\n" +
                    "}}";
            using (Stream s = new MemoryStream(Encoding.UTF8.GetBytes(test)))
            {
                List<string> shbangs = new List<string>();
                shbangs.Add("//#!Mode: ASSL");
                shbangs.Add("//#!Enable: Admin");
                IScriptAssembly t = Compile(AppDomain.CurrentDomain, UUI.Unknown, shbangs, UUID.Zero, new StreamReader(s));
            }
            #endregion
#endif
            SilverSim.Scripting.Common.CompilerRegistry.ScriptCompilers["lsl"] = this;
            SilverSim.Scripting.Common.CompilerRegistry.ScriptCompilers["XEngine"] = this; /* we won't be supporting anything beyond LSL compatibility */
        }

        class CompileState
        {
            public APIFlags AcceptedFlags;
            //public ModuleBuilder Module;
            public Dictionary<string, string> m_VariableDeclarations = new Dictionary<string,string>();
            public Dictionary<string, string> m_VariableInitValues = new Dictionary<string,string>();
            public List<List<string>> m_LocalVariables = new List<List<string>>();
            public Dictionary<string, List<List<string>>> m_Functions = new Dictionary<string, List<List<string>>>();
            public Dictionary<string, Dictionary<string, List<List<string>>>> m_States = new Dictionary<string, Dictionary<string, List<List<string>>>>();

            public CompileState()
            {

            }
        }

        private void throwParserException(Parser p, string message)
        {
            string fname;
            int lineno;
            p.getfileinfo(out fname, out lineno);
            throw new CompilerException(lineno, message);
        }

        private void checkValidName(Parser p, string type, string name)
        {
            if(name.Length == 0)
            {
                throwParserException(p, string.Format("{1} name '{0}' is not valid.", name, type));
            }
            if (name[0] != '_' && !(name[0] >= 'a' && name[0] <= 'z') && !(name[0] >= 'A' && name[0] <= 'Z') && name[0] != '_' && name[0] != '$')
            {
                throwParserException(p, string.Format("{1} name '{0}' is not valid.", name, type));
            }
            foreach(char c in name.Substring(1))
            {
                if (!(name[0] >= 'a' && name[0] <= 'z') && !(name[0] >= 'A' && name[0] <= 'Z') && name[0] != '_' && name[0] != '$')
                {
                    throwParserException(p, string.Format("{1} name '{0}' is not valid.", name, type));
                }
            }
        }

        private void checkUsedName(CompileState cs, Parser p, string type, string name)
        {
            checkValidName(p, type, name);
            if(m_ReservedWords.Contains(name))
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is a reserved word.", name, type));
            }
            else if(m_MethodNames.Contains(name))
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined function name.", name, type));
            }
            else if(m_Constants.ContainsKey(name) && (m_Constants[name] & cs.AcceptedFlags) != 0)
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined constant.", name, type));
            }
            else if (m_EventDelegates.ContainsKey(name))
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined constant.", name, type));
            }
            else if(cs.m_VariableDeclarations.ContainsKey(name))
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined as user variable.", name, type));
            }
            else if (cs.m_Functions.ContainsKey(name))
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined as user function.", name, type));
            }
            if(cs.m_LocalVariables[cs.m_LocalVariables.Count - 1].Contains(name))
            {
                throwParserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined as local variable in the same block.", name, type));
            }
        }

        struct FuncParamInfo
        {
            public Type Type;
            public string Name;
        }

        List<FuncParamInfo> checkFunctionParameters(CompileState cs, Parser p, List<string> arguments)
        {
            List<FuncParamInfo> funcParams = new List<FuncParamInfo>();
            if(cs.m_LocalVariables.Count != 0)
            {
                throwParserException(p, "Internal parser error");
            }
            cs.m_LocalVariables.Add(new List<string>());
            if(arguments.Count == 1 && arguments[0] == ")")
            {
                return funcParams;
            }
            for(int i = 0; i < arguments.Count; i += 3)
            {
                FuncParamInfo fp = new FuncParamInfo();
                switch (arguments[i])
                {
                    case "integer":
                        fp.Type = typeof(int);
                        break;

                    case "vector":
                        fp.Type = typeof(Vector3);
                        break;

                    case "list":
                        fp.Type = typeof(AnArray);
                        break;

                    case "float":
                        fp.Type = typeof(double);
                        break;

                    case "string":
                        fp.Type = typeof(string);
                        break;

                    case "key":
                        fp.Type = typeof(UUID);
                        break;

                    case "rotation":
                        fp.Type = typeof(Quaternion);
                        break;

                    default:
                        throwParserException(p, string.Format("Invalid type for parameter {0}", i / 3));
                        break;
                }

                checkUsedName(cs, p, "Parameter", arguments[i + 1]);
                cs.m_LocalVariables[0].Add(arguments[i + 1]);
                fp.Name = arguments[i + 1];
                funcParams.Add(fp);

                if (arguments[i + 2] == ",")
                {
                }
                else if(arguments[i + 2] == ")")
                {
                    if(i + 3 != arguments.Count)
                    {
                        throwParserException(p, string.Format("Missing ')' at the end of function declaration"));
                    }
                    return funcParams;
                }
            }
            throwParserException(p, string.Format("Missing ')' at the end of function declaration"));
            return null;
        }

        void parseBlock(CompileState compileState, Parser p, List<List<string>> block, bool addNewLocals = false)
        {
            if(addNewLocals)
            {
                compileState.m_LocalVariables.Add(new List<string>());
            }
            for (; ; )
            {
                List<string> args = new List<string>();
                try
                {
                    p.read(args);
                }
                catch (ParserBase.EndOfStringException)
                {
                    throwParserException(p, "Missing '\"' at the end of string");
                }
                catch (ParserBase.EndOfFileException)
                {
                    throwParserException(p, "Premature end of script");
                }
                if (args.Count == 0)
                {
                    /* should not happen but better be safe here */
                }
                else if (args[args.Count - 1] == ";")
                {
                    switch (args[0])
                    {
                        case "integer":
                        case "vector":
                        case "list":
                        case "float":
                        case "string":
                        case "key":
                        case "rotation":
                            checkUsedName(compileState, p, "Local Variable", args[1]);
                            compileState.m_LocalVariables[compileState.m_LocalVariables.Count - 1].Add(args[1]);
                            if(args[2] != ";" && args[2] != "=")
                            {
                                throwParserException(p, string.Format("Expecting '=' or ';' after variable name {0}", args[1]));
                            }
                            break;

                        default:
                            break;
                    }
                    block.Add(args);
                }
                else if (args[args.Count - 1] == "{")
                {
                    block.Add(args);
                    parseBlock(compileState, p, block, true);
                }
                else if (args[0] == "}")
                {
                    compileState.m_LocalVariables.RemoveAt(compileState.m_LocalVariables.Count - 1);
                    return;
                }
            }
        }

        void parseState(CompileState compileState, Parser p, string stateName)
        {
            compileState.m_States.Add(stateName, new Dictionary<string, List<List<string>>>());
            for (; ; )
            {
                List<string> args = new List<string>();
                try
                {
                    p.read(args);
                }
                catch (ParserBase.EndOfStringException)
                {
                    throwParserException(p, "Missing '\"' at the end of string");
                }
                catch (ParserBase.EndOfFileException)
                {
                    throwParserException(p, "Missing '}' at end of script");
                }
                if (args.Count == 0)
                {
                    /* should not happen but better be safe here */
                }
                else if (args[args.Count - 1] == ";")
                {
                    throwParserException(p, string.Format("Neither variable declarations nor statements allowed outside of event functions. Offending state {0}.", stateName));
                }
                else if (args[args.Count - 1] == "{")
                {
                    if(!m_EventDelegates.ContainsKey(args[0]))
                    {
                        throwParserException(p, string.Format("'{0}' is not a valid event.", args[0]));
                    }
                    List<FuncParamInfo> fp = checkFunctionParameters(compileState, p, args.GetRange(2, args.Count - 3));
                    MethodInfo m = m_EventDelegates[args[0]];
                    ParameterInfo[] pi = m.GetParameters();
                    if(fp.Count != pi.Length)
                    {
                        throwParserException(p, string.Format("'{0}' does not have the correct parameters.", args[0]));
                    }
                    int i;
                    for(i = 0; i < fp.Count; ++i)
                    {
                        if(!fp[i].Type.Equals(pi[i].ParameterType))
                        {
                            throwParserException(p, string.Format("'{0}' does not match in parameter types", args[0]));
                        }
                    }
                    if(compileState.m_States[stateName].ContainsKey(args[0]))
                    {
                        throwParserException(p, string.Format("Event '{0}' already defined", args[0]));
                    }
                    List<List<string>> stateList = new List<List<string>>();
                    compileState.m_States[stateName].Add(args[0], stateList);
                    stateList.Add(args);
                    parseBlock(compileState, p, stateList);
                }
                else if (args[0] == "}")
                {
                    return;
                }
            }
        }

        void solveDeclarations(Tree tree)
        {
            foreach(Tree st in tree.SubTree)
            {
                solveDeclarations(st);

                if(st.Type == Tree.EntryType.Declaration)
                {
                    if(st.SubTree.Count == 3)
                    {
                        st.Type = Tree.EntryType.Vector;
                    }
                    else if(st.SubTree.Count == 4)
                    {
                        st.Type = Tree.EntryType.Rotation;
                    }
                    else
                    {
                        throw new Resolver.ResolverException("argument list for <> has neither 3 nor 4 arguments");
                    }
                }
            }
        }

        class ConstantValueVector : Tree.ConstantValue
        {
            public Vector3 Value;

            public ConstantValueVector(Vector3 v)
            {
                Value = v;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override Tree.ValueBase Negate()
            {
                return new ConstantValueVector(-Value);
            }
        }

        class ConstantValueRotation : Tree.ConstantValue
        {
            public Quaternion Value;

            public ConstantValueRotation(Quaternion v)
            {
                Value = v;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override Tree.ValueBase Negate()
            {
                return new ConstantValueRotation(-Value);
            }
        }

        void solveTypecasts(Tree tree)
        {
            int pos;
            for (pos = 0; pos < tree.SubTree.Count - 1; ++pos )
            {
                Tree st = tree.SubTree[pos];
                if (st.SubTree.Count == 1 && st.SubTree[0].Type == Tree.EntryType.ReservedWord && st.Type == Tree.EntryType.Level)
                {
                    st.Entry = st.SubTree[0].Entry;
                    st.Type = Tree.EntryType.Typecast;
                    st.SubTree.Add(tree.SubTree[pos + 1]);
                    tree.SubTree.RemoveAt(pos + 1);
                }
                else
                {
                    ++pos;
                }
                solveTypecasts(st);
            }
        }

        void solveVariables(CompileState cs, Tree tree)
        {
            foreach(Tree st in tree.SubTree)
            {
                solveVariables(cs, tree);

                if(st.Type == Tree.EntryType.Unknown)
                {
                    if(cs.m_VariableDeclarations.ContainsKey(st.Entry))
                    {
                        st.Type = Tree.EntryType.Variable;
                    }
                    foreach(List<string> vars in cs.m_LocalVariables)
                    {
                        if (vars.Contains(st.Entry))
                        {
                            st.Type = Tree.EntryType.Variable;
                        }
                    }
                }
            }
        }

        void solveConstantOperations(Tree tree)
        {
            foreach(Tree st in tree.SubTree)
            {
                solveConstantOperations(st);

                if(st.Entry != "<")
                {

                }
                else if(st.Type == Tree.EntryType.Vector)
                {
                    if(st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null)
                    {
                        double[] v = new double[3];
                        for(int idx = 0; idx < 3; ++idx)
                        {
                            if(st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueFloat)
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if(st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueInt)
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new Resolver.ResolverException("constant vector cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueVector(new Vector3(v[0], v[1], v[2]));
                    }
                }
                else if(st.Type == Tree.EntryType.Rotation)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null &&
                        st.SubTree[3].SubTree[0].Value != null)
                    {
                        double[] v = new double[4];
                        for (int idx = 0; idx < 4; ++idx)
                        {
                            if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueFloat)
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueInt)
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new Resolver.ResolverException("constant rotation cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueRotation(new Quaternion(v[0], v[1], v[2], v[3]));

                    }
                }

                if(st.Type == Tree.EntryType.Typecast && st.SubTree[0].Value != null)
                {
                    /* solve a typecast */
                    switch(st.Entry)
                    {
                        case "string":
                            st.Value = new Tree.ConstantValueString(st.SubTree[0].Value.ToString());
                            break;

                        case "integer":
                            break;

                        case "float":
                            break;

                        case "vector":
                            break;

                        case "rotation":
                            break;

                        case "key":
                            break;

                        default:
                            throw new Resolver.ResolverException(string.Format("Invalid typecasting with {0}", st.Entry));
                    }
                }

                if(st.Type == Tree.EntryType.OperatorBinary && st.SubTree[0].Value != null && st.SubTree[1].Value != null)
                {
                    switch(st.Entry)
                    {
                        case "+":
                            if(st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if(st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueString && st.SubTree[1].Value is Tree.ConstantValueString)
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "-":
                            if(st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if(st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value -
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "*":
                            if(st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if(st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value.Cross(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value));
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value *
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "/":
                            if(st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if(st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value /
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "%":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "^":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ^
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<<":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <<
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">>":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >>
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "!=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "==":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ".":
                            throw new Resolver.ResolverException("'.' should not be used with constants");
                    }
                }
                else if(st.Type == Tree.EntryType.OperatorLeftUnary && st.SubTree[0].Value != null)
                {
                    if(st.Entry == "+")
                    {
                        st.Value = st.SubTree[0].Value;
                    }
                    else if(st.Entry == "-")
                    {
                        st.Value = st.SubTree[0].Value.Negate();
                    }
                    else if(st.Entry == "~")
                    {
                        if(st.Value is Tree.ConstantValueFloat)
                        {
                            throw new Resolver.ResolverException("float cannot be binary-negated");
                        }
                        else if(st.Value is Tree.ConstantValueInt)
                        {
                            st.Value = new Tree.ConstantValueInt(~((Tree.ConstantValueInt)(st.Value)).Value);
                        }
                        else if(st.Value is Tree.ConstantValueString)
                        {
                            throw new Resolver.ResolverException("string cannot be binary negated");
                        }
                    }
                    else if (st.Entry == "!")
                    {
                        if (st.Value is Tree.ConstantValueFloat)
                        {
                            throw new Resolver.ResolverException("float cannot be logically negated");
                        }
                        else if (st.Value is Tree.ConstantValueInt)
                        {
                            st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueInt)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (st.Value is Tree.ConstantValueString)
                        {
                            throw new Resolver.ResolverException("string cannot be logically negated");
                        }
                    }
                }
            }
        }

        void solveTree(CompileState cs, Tree resolvetree)
        {
            m_Resolver.Process(resolvetree);
            solveDeclarations(resolvetree);
            solveTypecasts(resolvetree);
            solveVariables(cs, resolvetree);
            solveConstantOperations(resolvetree);
        }
        
        CompileState Preprocess(UUI user, Dictionary<int, string> shbangs, TextReader reader, int lineNumber = 1)
        {
            CompileState compileState = new CompileState();
            compileState.AcceptedFlags = APIFlags.OSSL | APIFlags.LSL | APIFlags.LightShare;
            APIFlags extraflags = APIFlags.None;
            foreach(KeyValuePair<int, string> shbang in shbangs)
            { 
                if (shbang.Value.StartsWith("//#!Mode:"))
                {
                    /* we got a sh-bang here, it is a lot safer than what OpenSimulator uses */
                    string mode = shbang.Value.Substring(9).Trim().ToUpper();
                    if (mode == "LSL")
                    {
                        compileState.AcceptedFlags = APIFlags.LSL;
                    }
                    else if (mode == "ASSL")
                    {
                        compileState.AcceptedFlags = APIFlags.ASSL | APIFlags.OSSL | APIFlags.LightShare | APIFlags.LSL;
                    }
                    else if(mode == "AURORA" || mode == "WHITECORE")
                    {
                        compileState.AcceptedFlags = APIFlags.OSSL | APIFlags.WindLight_Aurora | APIFlags.LSL;
                    }
                }
                else if (shbang.Value.StartsWith("//#!Enable:"))
                {
                    string api = shbang.Value.Substring(11).Trim().ToLower();
                    if (api == "admin")
                    {
                        extraflags |= APIFlags.ASSL_Admin;
                    }
                }
                compileState.AcceptedFlags |= extraflags;
            }

            Parser p = new Parser();
            p.push(reader, "", lineNumber);
            
            for (; ;)
            {
                List<string> args = new List<string>();
                try
                {
                    p.read(args);
                }
                catch(ParserBase.EndOfStringException)
                {
                    throwParserException(p, "Missing '\"' at the end of string");
                }
                catch(ParserBase.EndOfFileException)
                {
                    break;
                }
                if(args.Count == 0)
                {
                    /* should not happen but better be safe here */
                }
                else if(args[args.Count - 1] == ";")
                {
                    /* variable definition */
                    if(args[2] != "=" && args[2] != ";")
                    {
                        throwParserException(p, "Invalid variable definition. Either ';' or an expression preceeded by '='");
                    }
                    switch (args[0])
                    {
                        case "integer":
                        case "vector":
                        case "list":
                        case "float":
                        case "string":
                        case "key":
                        case "rotation":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            break;

                        default:
                            throwParserException(p, string.Format("Invalid variable definition. Wrong type {0}.", args[0]));
                            break;
                    }
                }
                else if (args[args.Count - 1] == "{")
                {
                    if(args[0] == "default")
                    {
                        /* default state begin */
                        if(args[1] != "{")
                        {
                            throwParserException(p, "Invalid default state declaration");
                        }
                        parseState(compileState, p, "default");
                    }
                    else if(args[0] == "state")
                    {
                        /* state begin */
                        if(args[1] == "default")
                        {
                            throwParserException(p, "default state cannot be declared with state");
                        }
                        checkValidName(p, "State", args[1]);
                        if(compileState.m_States.ContainsKey(args[1]))
                        {
                            throwParserException(p, "state definition cannot be declared twice");
                        }

                        if(args[2] != "{")
                        {
                            throwParserException(p, "Invalid state declaration");
                        }
                        parseState(compileState, p, args[1]);
                    }
                    else
                    {
                        List<FuncParamInfo> fp;
                        List<List<string>> funcList = new List<List<string>>();
                        /* either type or function name */
                        switch (args[0])
                        {
                            case "integer":
                            case "vector":
                            case "list":
                            case "float":
                            case "string":
                            case "key":
                            case "rotation":
                                checkUsedName(compileState, p, "Function", args[1]);
                                fp = checkFunctionParameters(compileState, p, args.GetRange(3, args.Count - 3));
                                funcList.Add(args);
                                parseBlock(compileState, p, funcList);
                                break;

                            default:
                                fp = checkFunctionParameters(compileState, p, args.GetRange(2, args.Count - 3));
                                args.Insert(0, "void");
                                funcList.Add(args);
                                parseBlock(compileState, p, funcList);
                                break;
                        }
                    }
                }
                else if (args[0] == "}")
                {
                    throwParserException(p, "'}' found without matching '{'");
                }
            }
            return compileState;
        }

        public void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            Preprocess(user, shbangs, reader, linenumber);
        }

        public IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int lineNumber = 1)
        {
            CompileState compileState = Preprocess(user, shbangs, reader, lineNumber);
            return PostProcess(compileState, appDom, assetID);
        }

        void ProcessFunction(CompileState compileState, TypeBuilder scriptTypeBuilder, TypeBuilder stateTypeBuilder, MethodBuilder mb, ILGenerator ilgen, List<List<string>> functionBody)
        {

        }

        IScriptAssembly PostProcess(CompileState compileState, AppDomain appDom, UUID assetID)
        {
            string assetAssemblyName = "Script." + assetID.ToString().Replace("-", "_");
            AssemblyName aName = new AssemblyName(assetAssemblyName);
            AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            #region Create Script Container
            TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
            foreach (IScriptApi api in m_Apis)
            {
                ScriptApiName apiAttr = (ScriptApiName)api.GetType().GetCustomAttributes(typeof(ScriptApiName), false)[0];
                scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
            }

            Type[] script_cb_params = new Type[2] { typeof(ObjectPart), typeof(ObjectPartInventoryItem) };
            ConstructorBuilder script_cb = scriptTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, script_cb_params);
            ILGenerator script_ilgen = script_cb.GetILGenerator();
            {
                ConstructorInfo typeConstructor = typeof(Script).GetConstructor(script_cb_params);
                script_ilgen.Emit(OpCodes.Ldarg_1);
                script_ilgen.Emit(OpCodes.Ldarg_1);
                script_ilgen.Emit(OpCodes.Ldarg_2);
                script_ilgen.Emit(OpCodes.Call, typeConstructor);
            }
            #endregion

            Dictionary<string, Type> stateTypes = new Dictionary<string, Type>();

            foreach(KeyValuePair<string, List<List<string>>> functionKvp in compileState.m_Functions)
            {
                MethodBuilder method;
                Type returnType = typeof(void);
                List<string> functionDeclaration = functionKvp.Value[0];
                string functionName = functionDeclaration[1];
                int functionStart = 2;

                switch(functionDeclaration[0])
                {
                    case "integer":
                        returnType = typeof(int);
                        break;

                    case "vector":
                        returnType = typeof(Vector3);
                        break;

                    case "list":
                        returnType = typeof(AnArray);
                        break;

                    case "float":
                        returnType = typeof(double);
                        break;

                    case "string":
                        returnType = typeof(string);
                        break;

                    case "key":
                        returnType = typeof(string);
                        break;

                    case "rotation":
                        returnType = typeof(Quaternion);
                        break;

                    default:
                        functionName = functionDeclaration[0];
                        functionStart = 1;
                        break;
                }
                List<Type> paramTypes = new List<Type>();
                while(functionDeclaration[++functionStart] != ")")
                {
                    switch(functionDeclaration[++functionStart])
                    {
                        case "integer":
                            paramTypes.Add(typeof(int));
                            break;

                        case "vector":
                            paramTypes.Add(typeof(Vector3));
                            break;

                        case "list":
                            paramTypes.Add(typeof(AnArray));
                            break;

                        case "float":
                            paramTypes.Add(typeof(double));
                            break;

                        case "string":
                            paramTypes.Add(typeof(string));
                            break;

                        case "key":
                            paramTypes.Add(typeof(string));
                            break;

                        case "rotation":
                            paramTypes.Add(typeof(Quaternion));
                            break;

                        default:
                            throw new CompilerException(0, "Internal Error");
                    }
                    ++functionStart; /* skip parameter */
                }

                method = scriptTypeBuilder.DefineMethod("fn_" + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
                ILGenerator method_ilgen = method.GetILGenerator();
                ProcessFunction(compileState, scriptTypeBuilder, null, method, method_ilgen, functionKvp.Value);
                method_ilgen.Emit(OpCodes.Ret);
            }

            foreach (List<string> localVar in compileState.m_LocalVariables)
            {
                FieldBuilder fb;
                switch(localVar[0])
                {
                    case "key":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(string), FieldAttributes.Public);
                        break;

                    case "string":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(string), FieldAttributes.Public);
                        break;

                    case "integer":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(int), FieldAttributes.Public);
                        break;

                    case "float":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(double), FieldAttributes.Public);
                        break;

                    case "vector":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(Vector3), FieldAttributes.Public);
                        break;

                    case "rotation":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(Quaternion), FieldAttributes.Public);
                        break;

                    case "list":
                        fb = scriptTypeBuilder.DefineField("var_" + localVar[1], typeof(AnArray), FieldAttributes.Public);
                        break;

                    default:
                        break;
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, List<List<string>>>> stateKvp in compileState.m_States)
            {
                FieldBuilder fb;
                TypeBuilder state = mb.DefineType(aName.Name + ".State." + stateKvp.Key, TypeAttributes.Public, typeof(object));
                state.AddInterfaceImplementation(typeof(LSLState));
                fb = state.DefineField("Instance", scriptTypeBuilder, FieldAttributes.Private | FieldAttributes.InitOnly);

                ConstructorBuilder state_cb = state.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[1] { scriptTypeBuilder });
                ILGenerator state_ilgen = state_cb.GetILGenerator();
                ConstructorInfo typeConstructor = typeof(Script).GetConstructor(new Type[0]);
                state_ilgen.Emit(OpCodes.Ldarg_0);
                state_ilgen.Emit(OpCodes.Call, typeConstructor);
                state_ilgen.Emit(OpCodes.Ldarg_1);
                state_ilgen.Emit(OpCodes.Stfld, fb);

                #region Fill constants and objects
                foreach (IScriptApi api in m_Apis)
                {
                    foreach (FieldInfo f in api.GetType().GetFields())
                    {
                        System.Attribute attr = System.Attribute.GetCustomAttribute(f, typeof(APILevel));

                        if (attr != null && (f.Attributes & FieldAttributes.Static) != 0)
                        {
                            if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                            {
                                APILevel apilevel = (APILevel)attr;
                                if ((apilevel.Flags & compileState.AcceptedFlags) != 0)
                                {
                                    fb = state.DefineField(f.Name, f.FieldType, f.Attributes);
                                    if ((f.Attributes & FieldAttributes.Literal) != 0)
                                    {
                                        fb.SetConstant(f.GetValue(null));
                                    }
                                    else
                                    {
                                        state_ilgen.Emit(OpCodes.Ldfld, f);
                                        state_ilgen.Emit(OpCodes.Stfld, fb);
                                    }
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

                state_ilgen.Emit(OpCodes.Ret);

                /* add the type initializers */
                state_cb = state.DefineTypeInitializer();

                state_ilgen = state_cb.GetILGenerator();

                typeConstructor = typeof(Object).GetConstructor(new Type[0]);

                state_ilgen.Emit(OpCodes.Ldarg_0);
                state_ilgen.Emit(OpCodes.Call, typeConstructor);
                state_ilgen.Emit(OpCodes.Ret);

                foreach (KeyValuePair<string, List<List<string>>> eventKvp in stateKvp.Value)
                {
                    MethodInfo d = m_EventDelegates[eventKvp.Key];
                    ParameterInfo[] pinfo = d.GetParameters();
                    Type[] paramtypes = new Type[pinfo.Length];
                    for(int pi = 0; pi < pinfo.Length; ++pi)
                    {
                        paramtypes[pi] = pinfo[pi].ParameterType;
                    }
                    MethodBuilder eventbuilder = state.DefineMethod(eventKvp.Key, MethodAttributes.Public, typeof(void), paramtypes);
                    ILGenerator event_ilgen = eventbuilder.GetILGenerator();
                    ProcessFunction(compileState, scriptTypeBuilder, state, eventbuilder, event_ilgen, eventKvp.Value);
                    event_ilgen.Emit(OpCodes.Ret);
                }

                stateTypes.Add(stateKvp.Key, state.CreateType());
            }

            script_ilgen.Emit(OpCodes.Ret);

            #region Call type initializer
            {
                script_cb = scriptTypeBuilder.DefineTypeInitializer();
                script_ilgen = script_cb.GetILGenerator();
                ConstructorInfo typeConstructor = typeof(Object).GetConstructor(new Type[0]);

                script_ilgen.Emit(OpCodes.Ldarg_0);
                script_ilgen.Emit(OpCodes.Call, typeConstructor);
                script_ilgen.Emit(OpCodes.Ret);
            }
            #endregion

            #region Initialize static fields
            Type t = scriptTypeBuilder.CreateType();
            foreach (IScriptApi api in m_Apis)
            {
                ScriptApiName apiAttr = (ScriptApiName)api.GetType().GetCustomAttributes(typeof(ScriptApiName), false)[0];
                FieldInfo info = t.GetField(apiAttr.Name);
                info.SetValue(t, api);
            }
            #endregion

            mb.CreateGlobalFunctions();

            return new LSLScriptAssembly(ab, t, stateTypes);
        }

        class LSLScriptAssembly : IScriptAssembly
        {
            Assembly m_Assembly;
            Type m_Script;
            Dictionary<string, Type> m_StateTypes;

            public LSLScriptAssembly(Assembly assembly, Type script, Dictionary<string, Type> stateTypes)
            {
                m_Assembly = assembly;
                m_Script = script;
                m_StateTypes = stateTypes;
            }

            public ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item)
            {
                Script m_Script = new Script(objpart, item);
                foreach(KeyValuePair<string, Type> t in m_StateTypes)
                {
                    ConstructorInfo info = t.Value.GetConstructor(new Type[1] { typeof(Script) });
                    object[] param = new object[1];
                    param[0] = m_Script;
                    m_Script.AddState(t.Key, (LSLState)info.Invoke(param));
                }

                return m_Script;
            }
        }
    }
}


#if EXAMPLARY_CODE_FOR_CREATING_DYNAMIC_ASSEMBLIES            
            AssemblyName aName = new AssemblyName("SilverSim.Scripting.LSL.Script");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            TypeBuilder lsl = mb.DefineType(aName.Name + ".LSL", TypeAttributes.Public, typeof(ScriptBase));
            TypeBuilder ossl = mb.DefineType(aName.Name + ".OSSL", TypeAttributes.Public, typeof(ScriptBase));
            TypeBuilder assl = mb.DefineType(aName.Name + ".ASSL", TypeAttributes.Public, typeof(ScriptBase));

            Type[] constructorParams = new Type[2];
            constructorParams[0] = typeof(ObjectPart);
            constructorParams[1] = typeof(ObjectPartInventoryItem);
            constructorParams[2] = typeof(Int32);
            ConstructorBuilder lslcb = lsl.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorParams);
            ConstructorBuilder osslcb = ossl.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorParams);
            ConstructorBuilder asslcb = assl.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorParams);
            ConstructorInfo scriptbaseconstructor = typeof(ScriptBase).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, constructorParams, null);

            ILGenerator lslcb_il = lslcb.GetILGenerator();
            ILGenerator osslcb_il = osslcb.GetILGenerator();
            ILGenerator asslcb_il = asslcb.GetILGenerator();

            /* Add the calls to the base class constructor here */
            lslcb_il.Emit(OpCodes.Ldarg_0);
            lslcb_il.Emit(OpCodes.Ldarg_1);
            lslcb_il.Emit(OpCodes.Ldarg_2);
            lslcb_il.Emit(OpCodes.Ldc_I4, 0);
            lslcb_il.Emit(OpCodes.Call, scriptbaseconstructor);

            osslcb_il.Emit(OpCodes.Ldarg_0);
            osslcb_il.Emit(OpCodes.Ldarg_1);
            osslcb_il.Emit(OpCodes.Ldarg_2);
            osslcb_il.Emit(OpCodes.Ldc_I4, 0);
            osslcb_il.Emit(OpCodes.Call, scriptbaseconstructor);

            asslcb_il.Emit(OpCodes.Ldarg_0);
            asslcb_il.Emit(OpCodes.Ldarg_1);
            asslcb_il.Emit(OpCodes.Ldarg_2);
            asslcb_il.Emit(OpCodes.Ldc_I4, 1);
            asslcb_il.Emit(OpCodes.Call, scriptbaseconstructor);

            lslcb_il.Emit(OpCodes.Ret);
            osslcb_il.Emit(OpCodes.Ret);
            asslcb_il.Emit(OpCodes.Ret);

            /* add the type initializers */
            lslcb = lsl.DefineTypeInitializer();
            osslcb = ossl.DefineTypeInitializer();
            asslcb = assl.DefineTypeInitializer();

            lslcb_il = lslcb.GetILGenerator();
            osslcb_il = osslcb.GetILGenerator();
            asslcb_il = asslcb.GetILGenerator();

            ConstructorInfo typeConstructor = typeof(Object).GetConstructor(new Type[0]);

            lslcb_il.Emit(OpCodes.Ldarg_0);
            osslcb_il.Emit(OpCodes.Ldarg_0);
            asslcb_il.Emit(OpCodes.Ldarg_0);

            lslcb_il.Emit(OpCodes.Call, typeConstructor);
            osslcb_il.Emit(OpCodes.Call, typeConstructor);
            asslcb_il.Emit(OpCodes.Call, typeConstructor);

            FieldBuilder fb;

            foreach (ScriptApiFactory api in apis)
            {
                foreach (FieldInfo f in api.ApiType.GetFields())
                {
                    foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(f))
                    {
                        if (attr is APILevel && (f.Attributes & FieldAttributes.Static) != 0)
                        {
                            if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                            {
                                APILevel apilevel = (APILevel)attr;
                                if ((apilevel.Flags & APIFlags.LSL) != 0)
                                {
                                    fb = lsl.DefineField(f.Name, f.FieldType, f.Attributes);
                                    if ((f.Attributes & FieldAttributes.Literal) != 0)
                                    {
                                        fb.SetConstant(f.GetValue(null));
                                    }
                                    else
                                    {
                                        lslcb_il.Emit(OpCodes.Ldfld, f);
                                        lslcb_il.Emit(OpCodes.Stfld, fb);
                                    }
                                }
                                if ((apilevel.Flags & APIFlags.OSSL) != 0)
                                {
                                    fb = ossl.DefineField(f.Name, f.FieldType, f.Attributes);
                                    if ((f.Attributes & FieldAttributes.Literal) != 0)
                                    {
                                        fb.SetConstant(f.GetValue(null));
                                    }
                                    else
                                    {
                                        osslcb_il.Emit(OpCodes.Ldfld, f);
                                        osslcb_il.Emit(OpCodes.Stfld, fb);
                                    }
                                }
                                if ((apilevel.Flags & APIFlags.ASSL) != 0)
                                {
                                    fb = assl.DefineField(f.Name, f.FieldType, f.Attributes);
                                    if ((f.Attributes & FieldAttributes.Literal) != 0)
                                    {
                                        fb.SetConstant(f.GetValue(null));
                                    }
                                    else
                                    {
                                        asslcb_il.Emit(OpCodes.Ldfld, f);
                                        asslcb_il.Emit(OpCodes.Stfld, fb);
                                    }
                                }
                            }
                            else
                            {
                                m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                            }
                        }
                    }
                }
            }

            lslcb_il.Emit(OpCodes.Ret);
            osslcb_il.Emit(OpCodes.Ret);
            asslcb_il.Emit(OpCodes.Ret);

            LSLScript = lsl.CreateType();
            OSSLScript = ossl.CreateType();
            ASSLScript = assl.CreateType();

            mb.CreateGlobalFunctions();
#endif
