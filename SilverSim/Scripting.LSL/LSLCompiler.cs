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
        class CompileState
        {
            public bool EmitDebugSymbols = false;
            public APIFlags AcceptedFlags;
            public Dictionary<string, MethodBuilder> m_FunctionInfo = new Dictionary<string, MethodBuilder>();
            //public ModuleBuilder Module;
            public Dictionary<string, Type> m_VariableDeclarations = new Dictionary<string, Type>();
            public Dictionary<string, FieldBuilder> m_VariableFieldInfo = new Dictionary<string, FieldBuilder>();
            public Dictionary<string, LineInfo> m_VariableInitValues = new Dictionary<string, LineInfo>();
            public List<List<string>> m_LocalVariables = new List<List<string>>();
            public Dictionary<string, List<LineInfo>> m_Functions = new Dictionary<string, List<LineInfo>>();
            public Dictionary<string, Dictionary<string, List<LineInfo>>> m_States = new Dictionary<string, Dictionary<string, List<LineInfo>>>();
            public FieldBuilder InstanceField;
            public Dictionary<string, FieldBuilder> m_ApiFieldInfo = new Dictionary<string, FieldBuilder>();

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

        public void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            Preprocess(user, shbangs, reader, linenumber);
        }

        public IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int lineNumber = 1)
        {
            CompileState compileState = Preprocess(user, shbangs, reader, lineNumber);
            return PostProcess(compileState, appDom, assetID, (compileState.AcceptedFlags & APIFlags.ASSL) == 0);
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
