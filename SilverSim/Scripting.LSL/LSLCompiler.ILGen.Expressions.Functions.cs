// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.LSL.Expression;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        class FunctionExpression : IExpressionStackElement
        {
            class FunctionParameterInfo
            {
                public readonly string ParameterName;
                public readonly Type ParameterType;
                public readonly Tree FunctionArgument;
                public readonly int Position;
                public FunctionParameterInfo(string name, Type t, Tree functionarg, int position)
                {
                    ParameterName = name;
                    ParameterType = t;
                    FunctionArgument = functionarg;
                    Position = position;
                }
            }
            List<FunctionParameterInfo> m_Parameters = new List<FunctionParameterInfo>();
            
            string m_FunctionName;
            Type m_FunctionReturnType;
            int m_LineNumber;

            public FunctionExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                MethodBuilder mb;
                m_LineNumber = lineNumber;
                if (compileState.m_FunctionInfo.TryGetValue(functionTree.Entry, out mb))
                {
                    KeyValuePair<Type, KeyValuePair<string, Type>[]> signatureInfo = compileState.m_FunctionSignature[functionTree.Entry];
                    KeyValuePair<string, Type>[] pi = signatureInfo.Value;

                    if (null == stateTypeBuilder)
                    {
                        ilgen.Emit(OpCodes.Ldarg_0);
                    }
                    else
                    {
                        ilgen.Emit(OpCodes.Ldarg_0);
                        ilgen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    }

                    m_FunctionName = functionTree.Entry;

                    for (int i = 0; i < functionTree.SubTree.Count; ++i)
                    {
                        m_Parameters.Add(new FunctionParameterInfo(pi[i].Key, pi[i].Value, functionTree.SubTree[i], i));
                    }

                    m_FunctionReturnType = signatureInfo.Key;
                }
                else if (lslCompiler.m_MethodNames.Contains(functionTree.Entry))
                {
                    foreach (KeyValuePair<IScriptApi, MethodInfo> kvp in lslCompiler.m_Methods)
                    {
                        if (kvp.Value.Name == functionTree.Entry)
                        {
                            ParameterInfo[] pi = kvp.Value.GetParameters();
                            if (pi.Length - 1 == functionTree.SubTree.Count)
                            {
                                ScriptApiName apiAttr = (ScriptApiName)System.Attribute.GetCustomAttribute(kvp.Key.GetType(), typeof(ScriptApiName));

                                if (!IsValidType(kvp.Value.ReturnType))
                                {
                                    throw new CompilerException(lineNumber, string.Format("Internal Error! Return Value (type {1}) of function {0} is not LSL compatible", kvp.Value.Name, kvp.Value.ReturnType.Name));
                                }
                                if (null == stateTypeBuilder)
                                {
                                    ilgen.Emit(OpCodes.Ldarg_0);
                                }
                                else
                                {
                                    ilgen.Emit(OpCodes.Ldarg_0);
                                    ilgen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                                }

                                ilgen.Emit(OpCodes.Ldsfld, compileState.m_ApiFieldInfo[apiAttr.Name]);

                                for (int i = 0; i < functionTree.SubTree.Count; ++i)
                                {
                                    if (!IsValidType(pi[i + 1].ParameterType))
                                    {
                                        throw new CompilerException(lineNumber, string.Format("Internal Error! Parameter {0} (type {1}) of function {2} is not LSL compatible",
                                            pi[i + 1].Name, pi[i + 1].ParameterType.FullName, functionTree.Entry));
                                    }

                                    m_Parameters.Add(new FunctionParameterInfo(pi[i + 1].Name, pi[i + 1].ParameterType, functionTree.SubTree[i], i));
                                }

                                m_FunctionReturnType = kvp.Value.ReturnType;
                                return;
                            }
                        }
                    }
                    throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}", functionTree.Entry));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("No function {0} defined", functionTree.Entry));
                }
            }

            public Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                TypeBuilder scriptTypeBuilder, 
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen, 
                Dictionary<string, object> localVars,
                Type innerExpressionReturn)
            {
                if (null != innerExpressionReturn)
                {
                    try
                    {
                        ProcessImplicitCasts(ilgen, m_Parameters[0].ParameterType, innerExpressionReturn, m_LineNumber);
                    }
                    catch
                    {
                        throw new CompilerException(m_LineNumber,
                            string.Format("No implicit cast from {0} to {1} possible for parameter '{2}' of function '{3}'",
                                MapType(innerExpressionReturn),
                                MapType(m_Parameters[0].ParameterType),
                                m_Parameters[0].ParameterName,
                                m_FunctionName));
                    }

                    m_Parameters.RemoveAt(0);
                }

                if(m_Parameters.Count == 0)
                {
                    throw new ReturnTypeException(m_FunctionReturnType, m_LineNumber);
                }
                else
                {
                    return m_Parameters[0].FunctionArgument;
                }
            }
        }
    }
}
