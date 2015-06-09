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
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        class BinaryOperatorExpression : IExpressionStackElement
        {
            string m_Operator;
            LocalBuilder m_LeftHandLocal;
            LocalBuilder m_RightHandLocal;
            Tree m_LeftHand;
            Type m_LeftHandType = null;
            Tree m_RightHand;
            Type m_RightHandType = null;
            int m_LineNumber;
            enum State
            {
                LeftHand,
                RightHand
            }

            List<State> m_ProcessOrder;
            bool m_HaveBeginScope = false;

            static readonly Dictionary<string, State[]> m_ProcessOrders = new Dictionary<string, State[]>();

            static BinaryOperatorExpression()
            {
                m_ProcessOrders.Add("+", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("-", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("*", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("/", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("%", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("<<", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(">>", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("&", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("|", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("&&", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("||", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("^", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("<", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("<=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("==", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("!=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(">", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(">=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(".", new State[] { State.LeftHand });

                m_ProcessOrders.Add("=", new State[] { State.RightHand });
                m_ProcessOrders.Add("+=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("-=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("*=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("/=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("%=", new State[] { State.RightHand, State.LeftHand });
            }

            void BeginScope(ILGenerator ilgen)
            {
                if(m_HaveBeginScope)
                {
                    throw new CompilerException(m_LineNumber, "Internal Error! Binary operator evaluation scope error");
                }
                m_HaveBeginScope = true;
                ilgen.BeginScope();
            }

            LocalBuilder DeclareLocal(ILGenerator ilgen, Type localType)
            {
                if(!m_HaveBeginScope)
                {
                    ilgen.BeginScope();
                }
                m_HaveBeginScope = true;
                return ilgen.DeclareLocal(localType);
            }

            ReturnTypeException Return(ILGenerator ilgen, Type t)
            {
                if(m_HaveBeginScope)
                {
                    ilgen.EndScope();
                }
                return new ReturnTypeException(t, m_LineNumber);
            }

            public BinaryOperatorExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_LineNumber = lineNumber;
                m_LeftHand = functionTree.SubTree[0];
                m_RightHand = functionTree.SubTree[1];
                m_Operator = functionTree.Entry;
                m_ProcessOrder = new List<State>(m_ProcessOrders[m_Operator]);
                if(m_Operator == "=")
                {
                    if(m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                    {
                        if(m_LeftHand.SubTree[0].Type != Tree.EntryType.Variable)
                        {
                            throw new CompilerException(m_LineNumber, "l-value of operator '=' is not a variable");
                        }
                    }
                    else if(m_LeftHand.Type != Tree.EntryType.Variable)
                    {
                        throw new CompilerException(m_LineNumber, "l-value of operator '=' is not a variable");
                    }
                    object varInfo = localVars[m_LeftHand.Entry];
                    m_LeftHandType = GetVarType(scriptTypeBuilder, stateTypeBuilder, varInfo);
                }
                else if(m_Operator != "=" && m_Operator != ".")
                {
                    /* evaluation is reversed, so we have to sort them */
                    BeginScope(ilgen);
                    switch(m_Operator)
                    {
                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                            if(m_LeftHand.Type != Tree.EntryType.Variable)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("l-value of operator '{0}' is not a variable", m_Operator));
                            }
                            break;

                        default:
                            break;
                    }
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
                if(null != innerExpressionReturn)
                {
                    switch(m_ProcessOrder[0])
                    {
                        case State.RightHand:
                            if (m_HaveBeginScope)
                            {
                                m_RightHandLocal = DeclareLocal(ilgen, innerExpressionReturn);
                                ilgen.Emit(OpCodes.Stloc, m_RightHandLocal);
                            }
                            m_RightHandType = innerExpressionReturn;
                            break;

                        case State.LeftHand:
                            if (m_HaveBeginScope)
                            {
                                m_LeftHandLocal = DeclareLocal(ilgen, innerExpressionReturn);
                                ilgen.Emit(OpCodes.Stloc, m_LeftHandLocal);
                            }
                            m_LeftHandType = innerExpressionReturn;
                            break;

                    }
                    m_ProcessOrder.RemoveAt(0);
                }
                
                if(m_ProcessOrder.Count != 0)
                {
                    switch(m_ProcessOrder[0])
                    {
                        case State.RightHand:
                            return m_RightHand;

                        case State.LeftHand:
                            return m_LeftHand;

                        default:
                            throw new CompilerException(m_LineNumber, "Internal Error");
                    }
                }
                else
                {
                    switch(m_Operator)
                    {
                        case ".": 
                            ProcessOperator_Member(
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                localVars);
                            break;

                        case "=":
                            ProcessOperator_Assignment(
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                localVars);
                            break;

                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                            ProcessOperator_ModifyAssignment(
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                localVars);
                            break;

                        case "+":
                        case "-":
                        case "*":
                        case "/":
                        case "%":
                        case "^":
                        case "&":
                        case "&&":
                        case "|":
                        case "||":
                        case "!=":
                        case "==":
                        case "<=":
                        case ">=":
                        case ">":
                        case "<":
                        case "<<":
                        case ">>":
                            ProcessOperator_Return(
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                localVars);
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected operator '{0}'", m_Operator));
                    }
                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected return from operator '{0}' code generator", m_Operator));
                }
            }

            public void ProcessOperator_Member(
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder, 
                ILGenerator ilgen,
                Dictionary<string, object> localVars)
            {
                if (m_RightHand.Type != Tree.EntryType.Unknown &&
                    m_RightHand.Type != Tree.EntryType.Variable)
                {
                    throw new CompilerException(m_LineNumber, string.Format("'{0}' is not a member of type {1}", m_RightHand.Entry, MapType(m_LeftHandType)));
                }
                if (m_LeftHandType == typeof(Vector3))
                {
                    LocalBuilder lb = DeclareLocal(ilgen, m_LeftHandType);
                    ilgen.Emit(OpCodes.Stloc, lb);
                    ilgen.Emit(OpCodes.Ldloca, lb);
                    switch (m_RightHand.Entry)
                    {
                        case "x":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("X"));
                            break;

                        case "y":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("Y"));
                            break;

                        case "z":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("Z"));
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("'{0}' is not a member of type vector", m_RightHand.Entry));
                    }
                    throw Return(ilgen, typeof(double));
                }
                else if (m_LeftHandType == typeof(Quaternion))
                {
                    LocalBuilder lb = DeclareLocal(ilgen, m_LeftHandType);
                    ilgen.Emit(OpCodes.Stloc, lb);
                    ilgen.Emit(OpCodes.Ldloca, lb);
                    switch (m_RightHand.Entry)
                    {
                        case "x":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("X"));
                            break;

                        case "y":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("Y"));
                            break;

                        case "z":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("Z"));
                            break;

                        case "s":
                            ilgen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("W"));
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("'{0}' is not a member of type rotation", m_RightHand.Entry));
                    }
                    throw Return(ilgen, typeof(double));
                }
                else
                {
                    throw new CompilerException(m_LineNumber, "operator '.' can only be used on type vector or rotation");
                }
            }

            public void ProcessOperator_Assignment(
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Dictionary<string, object> localVars)
            {
                object varInfo = localVars[m_LeftHand.Entry];
                m_LeftHandType = GetVarType(scriptTypeBuilder, stateTypeBuilder, varInfo);
                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                ilgen.Emit(OpCodes.Dup);
                SetVarFromStack(
                    scriptTypeBuilder, 
                    stateTypeBuilder, 
                    ilgen, 
                    varInfo,
                    m_LineNumber);
                throw Return(ilgen, m_LeftHandType);
            }

            public void ProcessOperator_ModifyAssignment(
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Dictionary<string, object> localVars)
            {
                LocalBuilder componentLocal = null;
                object varInfo = localVars[m_LeftHand.SubTree[0].Entry];
                bool isComponentAccess = false;
                if(m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                {
                    m_LeftHandType = typeof(double);
                    Type varType = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                    componentLocal = DeclareLocal(ilgen, varType);
                    ilgen.Emit(OpCodes.Stloc, componentLocal);
                    isComponentAccess = true;
                }
                else
                {
                    varInfo = localVars[m_LeftHand.SubTree[0].Entry];
                }

                ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);

                if(m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(double))
                {

                }
                else if(m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(double))
                {

                }
                else if(m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(int))
                {
                    ProcessImplicitCasts(ilgen, typeof(double), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(double);
                }
                else if(m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(int))
                {
                    ProcessImplicitCasts(ilgen, typeof(double), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(double);
                }
                else
                {
                    ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                }

                MethodInfo mi;
                switch(m_Operator)
                {
                    case "+=":
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Sub);
                        }
                        else if(null != (mi = m_LeftHandType.GetMethod("op_Addition", new Type[]{m_LeftHandType, m_RightHandType})))
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '+=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            ilgen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '+=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "-=":
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Sub);
                        }
                        else if(null != (mi = m_LeftHandType.GetMethod("op_Subtraction", new Type[]{m_LeftHandType, m_RightHandType})))
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '-=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            ilgen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '-=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "*=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerMultiply", new Type[] { m_LeftHandType, m_RightHandType}));
                        }
                        else if(typeof(double) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Mul);
                        }
                        else if(null != (mi = m_LeftHandType.GetMethod("op_Multiply", new Type[]{m_LeftHandType, m_RightHandType})))
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '*=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            ilgen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '*=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "/=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerDivision", new Type[] { m_LeftHandType, m_RightHandType}));
                        }
                        else if(typeof(double) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Mul);
                        }
                        else if(null != (mi = m_LeftHandType.GetMethod("op_Division", new Type[]{m_LeftHandType, m_RightHandType})))
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '/=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            ilgen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '*=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "%=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerModulus", new Type[] { m_LeftHandType, m_RightHandType}));
                        }
                        else if(typeof(double) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Div);
                        }
                        else if(null != (mi = m_LeftHandType.GetMethod("op_Modulus", new Type[]{m_LeftHandType, m_RightHandType})))
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '%=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            ilgen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '%=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;
                }

                ilgen.Emit(OpCodes.Dup);
                if(isComponentAccess)
                {
                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, varInfo);
                    LocalBuilder resultLocal = DeclareLocal(ilgen, m_LeftHandType);
                    ilgen.Emit(OpCodes.Stloc, resultLocal);
                    ilgen.Emit(OpCodes.Ldloca, componentLocal);
                    ilgen.Emit(OpCodes.Ldloc, resultLocal);
                    string fieldName;
                    switch(m_LeftHand.SubTree[1].Entry)
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
                            if(typeof(Quaternion) != varType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("'vector' does not have a '{0}' member", m_LeftHand.SubTree[1].Entry));
                            }
                            fieldName = "W";
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("'{0}' does not have a '{1}' member", MapType(varType), m_LeftHand.SubTree[1].Entry));
                    }

                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                    ilgen.Emit(OpCodes.Ldloc, componentLocal);
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, m_LineNumber);
                    ilgen.Emit(OpCodes.Ldloc, resultLocal);
                    throw Return(ilgen, typeof(double));
                }
                else
                {
                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, m_LineNumber);
                    throw Return(ilgen, m_LeftHandType);
                }
            }

            public void ProcessOperator_Return(
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Dictionary<string, object> localVars)
            {
                MethodInfo mi;
                switch(m_Operator)
                {
                    case "+":
                        if(m_LeftHandType == typeof(AnArray) && m_RightHandType == typeof(AnArray))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                            ilgen.Emit(OpCodes.Dup);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("AddRange", new Type[] { typeof(AnArray) }));
                            throw Return(ilgen, typeof(AnArray));
                        }
                        if(m_LeftHandType == typeof(AnArray))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                            ilgen.Emit(OpCodes.Dup);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            if (typeof(int) == m_RightHandType || typeof(double) == m_RightHandType || typeof(string) == m_RightHandType)
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { m_RightHandType }));
                            }
                            else
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            throw Return(ilgen, typeof(AnArray));
                        }
                        else if(m_RightHandType == typeof(AnArray))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessCasts(ilgen, typeof(AnArray), m_LeftHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Dup);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            if (typeof(int) == m_RightHandType || typeof(double) == m_RightHandType || typeof(string) == m_RightHandType)
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { m_RightHandType }));
                            }
                            else
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            throw Return(ilgen, typeof(AnArray));
                        }
                        else if(m_LeftHandType == typeof(int) || m_RightHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Add);
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if(m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("Concat", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(string));
                        }
                        else if(m_LeftHandType == typeof(LSLKey))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, typeof(string), m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            throw Return(ilgen, typeof(string));
                        }
                        else if(typeof(string) == m_LeftHandType)
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("Concat", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if(typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType &&
                            null != (mi = m_LeftHandType.GetMethod("op_Addition", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        else if (typeof(double) != m_RightHandType && typeof(int) != m_RightHandType && typeof(string) != m_RightHandType && 
                            null != (mi = m_RightHandType.GetMethod("op_Addition", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '+' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "-":
                        if(m_LeftHandType == typeof(int) || m_RightHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Sub);
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if(m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("ob_Subtraction", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(string));
                        }
                        else if(typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType &&
                            null != (mi = m_LeftHandType.GetMethod("ob_Subtraction", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        else if (typeof(double) != m_RightHandType && typeof(int) != m_RightHandType && typeof(string) != m_RightHandType && 
                            null != (mi = m_RightHandType.GetMethod("ob_Subtraction", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '-' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        
                    case "*":
                        if (m_LeftHandType == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerMultiply", new Type[] { typeof(int), typeof(int) }));
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Mul);
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if(m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(string));
                        }
                        else if(typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType &&
                            null != (mi = m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        else if (null != (mi = m_RightHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '*' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "/":
                        if (m_LeftHandType == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerDivision", new Type[] { typeof(int), typeof(int) }));
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Div);
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Division", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(string));
                        }
                        else if (typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType &&
                            null != (mi = m_LeftHandType.GetMethod("op_Division", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        else if (null != (mi = m_RightHandType.GetMethod("op_Division", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '/' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "%":
                        if (m_LeftHandType == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerModulus", new Type[] { typeof(int), typeof(int) }));
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Rem);
                            throw Return(ilgen, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Modulus", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(string));
                        }
                        else if (typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType &&
                            null != (mi = m_LeftHandType.GetMethod("op_Modulus", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        else if (null != (mi = m_RightHandType.GetMethod("op_Modulus", new Type[] { m_LeftHandType, m_RightHandType })))
                        {
                            ilgen.Emit(OpCodes.Call, mi);
                            if(!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(ilgen, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '%' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "<<":
                        if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Shl);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '<<' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        throw Return(ilgen, m_LeftHandType);

                    case ">>":
                        if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Shr);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '>>' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        throw Return(ilgen, m_LeftHandType);

                    case "==":
                        if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_RightHandType == typeof(double))
                            {
                                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                                m_LeftHandType = m_RightHandType;
                            }
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Ceq);
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(LSLKey))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Callvirt, m_LeftHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_RightHandType == typeof(LSLKey))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, m_RightHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion) || m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Equality", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(AnArray) && m_RightHandType == typeof(AnArray))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, m_LeftHandType.GetProperty("Count").GetGetMethod());
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, m_LeftHandType.GetProperty("Count").GetGetMethod());
                            ilgen.Emit(OpCodes.Ceq);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '==' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "!=":
                        if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_RightHandType == typeof(double))
                            {
                                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                                m_LeftHandType = m_RightHandType;
                            }
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Ceq);
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(LSLKey))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Callvirt, m_LeftHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_RightHandType == typeof(LSLKey))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, m_RightHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion) || m_LeftHandType == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);
                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Inequality", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(AnArray) && m_RightHandType == typeof(AnArray))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, m_LeftHandType.GetProperty("Count").GetGetMethod());
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Callvirt, m_RightHandType.GetProperty("Count").GetGetMethod());
                            /* LSL is really about subtraction with that operator */
                            ilgen.Emit(OpCodes.Sub);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '!=' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "<=":
                        if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_RightHandType == typeof(double))
                            {
                                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                                m_LeftHandType = m_RightHandType;
                            }
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Cgt);
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_LessThanOrEqual", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(ilgen, typeof(int));
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '<=' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }

                    case "<":
                        if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_RightHandType == typeof(double))
                            {
                                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                                m_LeftHandType = m_RightHandType;
                            }
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Clt);

                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_LessThan", new Type[] { m_LeftHandType, m_LeftHandType }));

                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '<' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_LeftHandType)));

                    case ">":
                        if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_RightHandType == typeof(double))
                            {
                                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                                m_LeftHandType = m_RightHandType;
                            }
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Cgt);

                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_GreaterThan", new Type[] { m_LeftHandType, m_LeftHandType }));

                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '>' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_LeftHandType)));

                    case ">=":
                        if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_RightHandType == typeof(double))
                            {
                                ProcessImplicitCasts(ilgen, m_RightHandType, m_LeftHandType, m_LineNumber);
                                m_LeftHandType = m_RightHandType;
                            }
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Clt);
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);

                            throw Return(ilgen, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(ilgen, m_LeftHandType, m_RightHandType, m_LineNumber);

                            ilgen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_GreaterThanOrEqual", new Type[] { m_LeftHandType, m_LeftHandType }));

                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '>=' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "&&":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.And);
                            ilgen.Emit(OpCodes.Ldc_I4_1);
                            ilgen.Emit(OpCodes.And);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '&&' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "&":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.And);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '&' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "|":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Or);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '|' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "^":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Xor);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '^' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "||":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            ilgen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ilgen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ilgen.Emit(OpCodes.Or);
                            ilgen.Emit(OpCodes.Ldc_I4_1);
                            ilgen.Emit(OpCodes.And);
                            throw Return(ilgen, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '||' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    default:
                        throw new CompilerException(m_LineNumber, string.Format("unknown operator '{0}' for {1} and {2}", m_Operator, MapType(m_LeftHandType), MapType(m_RightHandType)));
                }
            }
        }
    }
}
