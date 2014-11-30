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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public class LSLCompiler : IScriptCompiler, IPlugin, IPluginSubFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LSL COMPILER");
        List<IScriptApi> m_Apis = new List<IScriptApi>();
        Dictionary<string, FieldInfo> m_Constants = new Dictionary<string, FieldInfo>();
        Dictionary<string, MethodInfo> m_Methods = new Dictionary<string, MethodInfo>();

        public LSLCompiler()
        {

        }

        public void AddPlugins(ConfigurationLoader loader)
        {
            Type[] types = GetType().Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (typeof(IScriptApi).IsAssignableFrom(type))
                {
                    foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(type))
                    {
                        if (attr is ScriptApiName)
                        {
                            IPlugin factory = (IPlugin)Activator.CreateInstance(type);
                            loader.AddPlugin(((ScriptApiName)attr).Name, factory);
                        }
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            List<IScriptApi> apis = loader.GetServicesByValue<IScriptApi>();
            foreach (IScriptApi api in apis)
            {
                foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(api.GetType()))
                {
                    if (attr is LSLImplementation && !m_Apis.Contains(api))
                    {
                        m_Apis.Add(api);
                    }
                }
            }

            foreach (IScriptApi api in apis)
            {
                foreach (FieldInfo f in api.GetType().GetFields())
                {
                    foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(f))
                    {
                        if (attr is APILevel && (f.Attributes & FieldAttributes.Static) != 0)
                        {
                            if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                            {
                                m_Constants.Add(f.Name, f);
                            }
                            else
                            {
                                m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                            }
                        }
                    }
                }
                foreach (MethodInfo m in api.GetType().GetMethods())
                {
                    foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(m))
                    {
                        if (attr is APILevel && (m.Attributes & MethodAttributes.Static) != 0)
                        {
                            m_Methods.Add(m.Name, m);
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
        }

        public IScriptAssembly Compile(UUI user, AssetData asset)
        {
            using (TextReader reader = new StreamReader(asset.InputStream))
            {
                return Compile(user, reader);
            }
        }

        public IScriptAssembly Compile(UUI user, TextReader reader, int lineNumber = 1)
        {
            APIFlags acceptedflags = APIFlags.OSSL | APIFlags.LSL | APIFlags.LightShare;
            APIFlags extraflags = APIFlags.None;
            string input = reader.ReadLine();
            while(input.StartsWith("//#!"))
            {
                if (input.StartsWith("//#!Mode:"))
                {
                    /* we got a sh-bang here, it is a lot safer than what OpenSimulator uses */
                    string mode = input.Substring(9).Trim();
                    if (mode == "LSL")
                    {
                        acceptedflags = APIFlags.LSL;
                    }
                    else if (mode == "ASSL")
                    {
                        acceptedflags = APIFlags.ASSL;
                    }
                }
                else if(input.StartsWith("//#!Enable:"))
                {
                    string api = input.Substring(11).Trim();
                    if(api == "Admin")
                    {
                        extraflags |= APIFlags.ASSL_Admin;
                    }
                }

                ++lineNumber;
                input = reader.ReadLine();
            }
            acceptedflags |= extraflags;

            while (null != input)
            {

                
                ++lineNumber;
                input = reader.ReadLine();
            }
            throw new NotImplementedException();
        }
    }
}
