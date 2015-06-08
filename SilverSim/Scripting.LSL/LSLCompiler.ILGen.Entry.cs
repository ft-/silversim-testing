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
using SilverSim.Scripting.LSL.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {

        IScriptAssembly PostProcess(CompileState compileState, AppDomain appDom, UUID assetID, bool forcedSleepDefault)
        {
            string assetAssemblyName = "Script." + assetID.ToString().Replace("-", "_");
            AssemblyName aName = new AssemblyName(assetAssemblyName);
            AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, compileState.EmitDebugSymbols);

            #region Create Script Container
            TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
            Dictionary<string, object> typeLocals = new Dictionary<string, object>();
            foreach (IScriptApi api in m_Apis)
            {
                ScriptApiName apiAttr = (ScriptApiName)System.Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiName));
                FieldBuilder fb = scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
                compileState.m_ApiFieldInfo[apiAttr.Name] = fb;
            }


            Type[] script_cb_params = new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) };
            ConstructorBuilder script_cb = scriptTypeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                script_cb_params);
            ILGenerator script_ilgen = script_cb.GetILGenerator();
            {
                ConstructorInfo typeConstructor = typeof(Script).GetConstructor(script_cb_params);
                script_ilgen.Emit(OpCodes.Ldarg_0);
                script_ilgen.Emit(OpCodes.Ldarg_1);
                script_ilgen.Emit(OpCodes.Ldarg_2);
                if (forcedSleepDefault)
                {
                    script_ilgen.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    script_ilgen.Emit(OpCodes.Ldc_I4_0);
                }
                script_ilgen.Emit(OpCodes.Call, typeConstructor);
            }
            #endregion

            Dictionary<string, Type> stateTypes = new Dictionary<string, Type>();

            #region Globals generation
            typeLocals = AddConstants(compileState, scriptTypeBuilder, script_ilgen);
            foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
            {
                FieldBuilder fb = scriptTypeBuilder.DefineField("var_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public);
                compileState.m_VariableFieldInfo[variableKvp.Key] = fb;
                typeLocals[variableKvp.Key] = fb;
            }

            List<string> varIsInited = new List<string>();
            List<string> varsToInit = new List<string>(compileState.m_VariableInitValues.Keys);

            while (varsToInit.Count != 0)
            {
                string varName = varsToInit[0];
                varsToInit.RemoveAt(0);

                FieldBuilder fb = compileState.m_VariableFieldInfo[varName];
                LineInfo initargs;

                if (compileState.m_VariableInitValues.TryGetValue(varName, out initargs))
                {
                    Tree expressionTree;
                    try
                    {
                        expressionTree = new Tree(initargs.Line, m_OpChars, m_SingleOps, m_NumericChars);
                        solveTree(compileState, expressionTree, typeLocals.Keys);
                    }
                    catch (Exception e)
                    {
                        throw compilerException(initargs, string.Format("Init value of variable {0} has syntax error. {1}", varName, e.Message));
                    }

                    if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree))
                    {
                        ProcessExpression(
                            compileState,
                            scriptTypeBuilder,
                            scriptTypeBuilder,
                            script_ilgen,
                            fb.FieldType,
                            expressionTree,
                            initargs.LineNumber,
                            typeLocals);
                        script_ilgen.Emit(OpCodes.Stfld, fb);
                        varIsInited.Add(varName);
                    }
                    else
                    {
                        /* push back that var. We got it too early. */
                        varsToInit.Add(varName);
                    }
                }
                else if (fb.FieldType == typeof(int))
                {
                    script_ilgen.Emit(OpCodes.Ldc_I4_0);
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(double))
                {
                    script_ilgen.Emit(OpCodes.Ldc_R8, 0f);
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(string))
                {
                    script_ilgen.Emit(OpCodes.Ldstr, "");
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(Vector3))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[0]));
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(Quaternion))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[0]));
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(AnArray))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(LSLKey))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[0]));
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
            }
            #endregion

            #region Function compilation
            /* we have to process the function definition first */
            foreach (KeyValuePair<string, List<LineInfo>> functionKvp in compileState.m_Functions)
            {
                MethodBuilder method;
                Type returnType = typeof(void);
                List<string> functionDeclaration = functionKvp.Value[0].Line;
                string functionName = functionDeclaration[1];
                int functionStart = 3;

                switch (functionDeclaration[0])
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
                        returnType = typeof(LSLKey);
                        break;

                    case "rotation":
                    case "quaternion":
                        returnType = typeof(Quaternion);
                        break;

                    case "void":
                        returnType = typeof(void);
                        break;

                    default:
                        functionName = functionDeclaration[0];
                        functionStart = 2;
                        break;
                }
                List<Type> paramTypes = new List<Type>();
                List<string> paramName = new List<string>();
                while (functionDeclaration[functionStart] != ")")
                {
                    if (functionDeclaration[functionStart] == ",")
                    {
                        ++functionStart;
                    }
                    switch (functionDeclaration[functionStart++])
                    {
                        case "integer":
                            paramTypes.Add(typeof(int));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        case "vector":
                            paramTypes.Add(typeof(Vector3));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        case "list":
                            paramTypes.Add(typeof(AnArray));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        case "float":
                            paramTypes.Add(typeof(double));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        case "string":
                            paramTypes.Add(typeof(string));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        case "key":
                            paramTypes.Add(typeof(LSLKey));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        case "rotation":
                        case "quaternion":
                            paramTypes.Add(typeof(Quaternion));
                            paramName.Add(functionDeclaration[functionStart++]);
                            break;

                        default:
                            throw compilerException(functionKvp.Value[0], "Internal Error");
                    }
                }

                method = scriptTypeBuilder.DefineMethod("fn_" + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
                KeyValuePair<string, Type>[] paramSignature = new KeyValuePair<string, Type>[paramTypes.Count];
                for (int i = 0; i < paramTypes.Count; ++i)
                {
                    paramSignature[i] = new KeyValuePair<string, Type>(paramName[i], paramTypes[i]);
                }
                compileState.m_FunctionSignature[functionName] = new KeyValuePair<Type, KeyValuePair<string, Type>[]>(returnType, paramSignature);
                compileState.m_FunctionInfo[functionName] = method;
            }

            foreach (KeyValuePair<string, List<LineInfo>> functionKvp in compileState.m_Functions)
            {
                List<string> functionDeclaration = functionKvp.Value[0].Line;
                string functionName = functionDeclaration[1];
                MethodBuilder method = compileState.m_FunctionInfo[functionName];

                ILGenerator method_ilgen = method.GetILGenerator();
                ProcessFunction(compileState, scriptTypeBuilder, null, method, method_ilgen, functionKvp.Value, typeLocals);
                method_ilgen.Emit(OpCodes.Ret);
            }
            #endregion

            #region State compilation
            foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> stateKvp in compileState.m_States)
            {
                FieldBuilder fb;
                TypeBuilder state = mb.DefineType(aName.Name + ".State." + stateKvp.Key, TypeAttributes.Public, typeof(object));
                state.AddInterfaceImplementation(typeof(LSLState));
                fb = state.DefineField("Instance", scriptTypeBuilder, FieldAttributes.Private | FieldAttributes.InitOnly);
                compileState.InstanceField = fb;

                ConstructorBuilder state_cb = state.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard,
                    new Type[1] { scriptTypeBuilder });
                ILGenerator state_ilgen = state_cb.GetILGenerator();
                ConstructorInfo typeConstructor = typeof(object).GetConstructor(new Type[0]);
                state_ilgen.Emit(OpCodes.Ldarg_0);
                state_ilgen.Emit(OpCodes.Call, typeConstructor);
                state_ilgen.Emit(OpCodes.Ldarg_1);
                state_ilgen.Emit(OpCodes.Stfld, fb);
                typeLocals = AddConstants(compileState, state, state_ilgen);
                foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
                {
                    FieldBuilder cfb = scriptTypeBuilder.DefineField("var_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public);
                    compileState.m_VariableFieldInfo[variableKvp.Key] = cfb;
                    typeLocals[variableKvp.Key] = cfb;
                }

                state_ilgen.Emit(OpCodes.Ret);

                /* add the type initializers */
                state_cb = state.DefineTypeInitializer();

                state_ilgen = state_cb.GetILGenerator();
                state_ilgen.Emit(OpCodes.Ret);

                foreach (KeyValuePair<string, List<LineInfo>> eventKvp in stateKvp.Value)
                {
                    MethodInfo d = m_EventDelegates[eventKvp.Key];
                    ParameterInfo[] pinfo = d.GetParameters();
                    Type[] paramtypes = new Type[pinfo.Length];
                    for (int pi = 0; pi < pinfo.Length; ++pi)
                    {
                        paramtypes[pi] = pinfo[pi].ParameterType;
                    }
                    MethodBuilder eventbuilder = state.DefineMethod(
                        eventKvp.Key,
                        MethodAttributes.Public,
                        typeof(void),
                        paramtypes);
                    ILGenerator event_ilgen = eventbuilder.GetILGenerator();
                    ProcessFunction(compileState, scriptTypeBuilder, state, eventbuilder, event_ilgen, eventKvp.Value, typeLocals);
                }

                stateTypes.Add(stateKvp.Key, state.CreateType());
            }
            #endregion

            script_ilgen.Emit(OpCodes.Ret);

            #region Call type initializer
            {
                script_cb = scriptTypeBuilder.DefineTypeInitializer();
                script_ilgen = script_cb.GetILGenerator();
                script_ilgen.Emit(OpCodes.Ret);
            }
            #endregion

            mb.CreateGlobalFunctions();

            #region Initialize static fields
            Type t = scriptTypeBuilder.CreateType();

            foreach (IScriptApi api in m_Apis)
            {
                ScriptApiName apiAttr = (ScriptApiName)System.Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiName));
                FieldInfo info = t.GetField(apiAttr.Name, BindingFlags.Static | BindingFlags.Public);
                info.SetValue(null, api);
            }
            #endregion

            return new LSLScriptAssembly(ab, t, stateTypes, forcedSleepDefault);
        }

        bool AreAllVarReferencesSatisfied(CompileState cs, List<string> initedVars, Tree expressionTree)
        {
            foreach (Tree st in expressionTree.SubTree)
            {
                if (!AreAllVarReferencesSatisfied(cs, initedVars, st))
                {
                    return false;
                }
                else if (st.Type == Tree.EntryType.Variable || st.Type == Tree.EntryType.Unknown)
                {
                    if (cs.m_VariableDeclarations.ContainsKey(st.Entry) &&
                        !initedVars.Contains(st.Entry))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
