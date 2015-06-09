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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        void ProcessStatement(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            Type returnType,
            ILGenerator ilgen,
            int startAt,
            int endAt,
            LineInfo functionLine,
            Dictionary<string, object> localVars,
            Dictionary<string, ILLabelInfo> labels)
        {
            if (functionLine.Line[startAt] == "@")
            {
                throw compilerException(functionLine, "Invalid label declaration");
            }
            #region Jump to label
            else if (functionLine.Line[startAt] == "jump")
            {
                if (functionLine.Line.Count <= startAt + 2)
                {
                    throw compilerException(functionLine, "Invalid jump statement");
                }
                if (!labels.ContainsKey(functionLine.Line[1]))
                {
                    Label label = ilgen.DefineLabel();
                    labels[functionLine.Line[1]] = new ILLabelInfo(label, false);
                }
                labels[functionLine.Line[1]].UsedInLines.Add(functionLine.LineNumber);

                ilgen.Emit(OpCodes.Br, labels[functionLine.Line[1]].Label);
                compileState.PopControlFlowImplicit(ilgen, functionLine.LineNumber);
                return;
            }
            #endregion
            #region Return from function
            else if (functionLine.Line[startAt] == "return")
            {
                if (returnType == typeof(void))
                {
                    if (functionLine.Line[1] != ";")
                    {
                        ProcessExpression(
                            compileState,
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            ilgen,
                            typeof(void),
                            1,
                            functionLine.Line.Count - 2,
                            functionLine,
                            localVars);
                    }
                }
                else if (returnType == typeof(int))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(int),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(string))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(string),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(double))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(double),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(AnArray))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(AnArray),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(Vector3))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(Vector3),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(Quaternion))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(Quaternion),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(LSLKey))
                {
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(LSLKey),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                ilgen.Emit(OpCodes.Ret);
                compileState.PopControlFlowImplicit(ilgen, functionLine.LineNumber);
                return;
            }
            #endregion
            #region State Change
            else if (functionLine.Line[startAt] == "state")
            {
                /* when same state, the state instruction compiles to nop according to wiki */
                if (stateTypeBuilder == scriptTypeBuilder)
                {
                    throw compilerException(functionLine, "Global functions cannot change state");
                }
                ilgen.Emit(OpCodes.Ldstr, functionLine.Line[1]);
                ilgen.Emit(OpCodes.Newobj, typeof(ChangeStateException).GetConstructor(new Type[1] { typeof(string) }));
                ilgen.Emit(OpCodes.Throw);
                compileState.PopControlFlowImplicit(ilgen, functionLine.LineNumber);
                return;
            }
            #endregion
            #region Assignment =
            else if (functionLine.Line[startAt + 1] == "=")
            {
                string varName = functionLine.Line[startAt];
                /* variable assignment */
                object v = localVars[varName];
                ProcessExpression(
                    compileState,
                    scriptTypeBuilder,
                    stateTypeBuilder,
                    ilgen,
                    GetVarType(scriptTypeBuilder, stateTypeBuilder, v),
                    startAt + 2,
                    endAt,
                    functionLine,
                    localVars);
                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, functionLine.LineNumber);
            }
            #endregion
            #region Component Access
            else if (functionLine.Line[startAt + 1] == ".")
            {
                /* component access */
                if (startAt != 0)
                {
                    throw compilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object o = localVars[varName];
                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, o);
                    ilgen.BeginScope();
                    LocalBuilder lb_struct = ilgen.DeclareLocal(varType);
                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, o);
                    ilgen.Emit(OpCodes.Stloc, lb_struct);
                    string fieldName;
                    if (varType == typeof(Vector3))
                    {
                        switch (functionLine.Line[startAt + 2])
                        {
                            case "x":
                                fieldName = "X";
                                break;

                            case "y":
                                fieldName = "Y";
                                break;

                            case "z":
                                fieldName = "Z";
                                break;

                            default:
                                throw compilerException(functionLine, "vector does not have member " + functionLine.Line[startAt + 2]);
                        }

                    }
                    else if (varType == typeof(Quaternion))
                    {
                        switch (functionLine.Line[startAt + 2])
                        {
                            case "x":
                                fieldName = "X";
                                break;

                            case "y":
                                fieldName = "Y";
                                break;

                            case "z":
                                fieldName = "Z";
                                break;

                            case "s":
                                fieldName = "W";
                                break;

                            default:
                                throw compilerException(functionLine, "quaternion does not have member " + functionLine.Line[startAt + 2]);
                        }
                    }
                    else
                    {
                        throw compilerException(functionLine, "Type " + MapType(varType) + " does not have accessible components");
                    }

                    ilgen.Emit(OpCodes.Ldloca, lb_struct);
                    if (functionLine.Line[startAt + 3] != "=")
                    {
                        ilgen.Emit(OpCodes.Dup, lb_struct);
                        ilgen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                    }
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        typeof(double),
                        startAt + 4,
                        endAt,
                        functionLine,
                        localVars);

                    switch (functionLine.Line[startAt + 3])
                    {
                        case "=":
                            ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "+=":
                            ilgen.Emit(OpCodes.Add);
                            ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "-=":
                            ilgen.Emit(OpCodes.Sub);
                            ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "*=":
                            ilgen.Emit(OpCodes.Mul);
                            ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "/=":
                            ilgen.Emit(OpCodes.Div);
                            ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "%=":
                            ilgen.Emit(OpCodes.Rem);
                            ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        default:
                            throw compilerException(functionLine, string.Format("invalid assignment operator '{0}'", functionLine.Line[startAt + 3]));
                    }
                    ilgen.Emit(OpCodes.Ldloc, lb_struct);
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, o, functionLine.LineNumber);
                    ilgen.EndScope();
                }
            }
            #endregion
            #region Assignment Operators += -= *= /= %=
            else if (functionLine.Line[startAt + 1] == "+=")
            {
                if (startAt != 0)
                {
                    throw compilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        GetVarType(scriptTypeBuilder, stateTypeBuilder, v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double) || ret == typeof(string))
                    {
                        ilgen.Emit(OpCodes.Add);
                    }
                    else if (ret == typeof(LSLKey) || ret == typeof(AnArray) || ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        ilgen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Addition", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw compilerException(functionLine, string.Format("operator '+=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "-=")
            {
                if (startAt != 0)
                {
                    throw compilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        GetVarType(scriptTypeBuilder, stateTypeBuilder, v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        ilgen.Emit(OpCodes.Sub);
                    }
                    else if (ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        ilgen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Subtraction", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw compilerException(functionLine, string.Format("operator '-=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "*=")
            {
                if (startAt != 0)
                {
                    throw compilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        GetVarType(scriptTypeBuilder, stateTypeBuilder, v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        ilgen.Emit(OpCodes.Mul);
                    }
                    else if (ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        ilgen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Multiply", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw compilerException(functionLine, string.Format("operator '*=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "/=")
            {
                if (startAt != 0)
                {
                    throw compilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        GetVarType(scriptTypeBuilder, stateTypeBuilder, v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        ilgen.Emit(OpCodes.Div);
                    }
                    else if (ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        ilgen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Division", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw compilerException(functionLine, string.Format("operator '/=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "%=")
            {
                if (startAt != 0)
                {
                    throw compilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        GetVarType(scriptTypeBuilder, stateTypeBuilder, v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        ilgen.Emit(OpCodes.Rem);
                    }
                    else
                    {
                        throw compilerException(functionLine, string.Format("operator '%=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, functionLine.LineNumber);
                }
            }
            #endregion
            else
            {
                /* function call no return */
                ProcessExpression(
                    compileState,
                    scriptTypeBuilder,
                    stateTypeBuilder,
                    ilgen,
                    typeof(void),
                    startAt,
                    endAt,
                    functionLine,
                    localVars);
            }
        }
    }
}
