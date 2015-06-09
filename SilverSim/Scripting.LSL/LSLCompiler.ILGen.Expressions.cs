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
        class ReturnTypeException : Exception
        {
            public Type ReturnType;
            public ReturnTypeException(Type t, int lineNumber)
            {
                ReturnType = t;
                if(t == null)
                {
                    throw new CompilerException(lineNumber, "Internal Error! returnType is not set");
                }
                else if (!IsValidType(t))
                {
                    throw new CompilerException(lineNumber, string.Format("Internal Error! '{0}' is not a LSL compatible type", t.FullName));
                }
            }
        }

        interface IExpressionStackElement
        {
            Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Dictionary<string, object> localVars,
                Type innerExpressionReturn);
        }

        Type ProcessExpressionPart(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            Tree functionTree,
            int lineNumber,
            Dictionary<string, object> localVars)
        {
            List<IExpressionStackElement> expressionStack = new List<IExpressionStackElement>();
            Type innerExpressionReturn = null;
            bool first = true;

            for (; ;)
            {
                if (expressionStack.Count != 0)
                {
                    try
                    {
                        functionTree = expressionStack[0].ProcessNextStep(
                            this,
                            compileState,
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            ilgen,
                            localVars,
                            innerExpressionReturn);
                    }
                    catch (ReturnTypeException e)
                    {
                        innerExpressionReturn = e.ReturnType;
                        expressionStack.RemoveAt(0);
                        if (expressionStack.Count == 0)
                        {
                            if (!IsValidType(innerExpressionReturn))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Return type is not set to LSL compatible type");
                            }
                            return innerExpressionReturn;
                        }
                        continue;
                    }
                }
                else if(!first)
                {
                    if(!IsValidType(innerExpressionReturn))
                    {
                        throw new CompilerException(lineNumber, "Internal Error! Return type is not set to LSL compatible type");
                    }
                    return innerExpressionReturn;
                }
                first = false;

                /* dive into */
                while (functionTree.Type == Tree.EntryType.FunctionArgument ||
                    functionTree.Type == Tree.EntryType.ExpressionTree ||
                    (functionTree.Type == Tree.EntryType.Level && functionTree.Entry != "["))
                {
                    functionTree = functionTree.SubTree[0];
                }

                if (functionTree.Value != null)
                {
                    if (functionTree.Value is Tree.ConstantValueFloat)
                    {
                        ilgen.Emit(OpCodes.Ldc_R8, ((Tree.ConstantValueFloat)functionTree.Value).Value);
                        innerExpressionReturn = typeof(double);
                    }
                    else if (functionTree.Value is Tree.ConstantValueInt)
                    {
                        ilgen.Emit(OpCodes.Ldc_I4, ((Tree.ConstantValueInt)functionTree.Value).Value);
                        innerExpressionReturn = typeof(int);
                    }
                    else if (functionTree.Value is Tree.ConstantValueString)
                    {
                        ilgen.Emit(OpCodes.Ldstr, ((Tree.ConstantValueString)functionTree.Value).Value);
                        innerExpressionReturn = typeof(string);
                    }
                    else if (functionTree.Value is ConstantValueRotation)
                    {
                        ConstantValueRotation val = (ConstantValueRotation)functionTree.Value;
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.W);
                        ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                        innerExpressionReturn = typeof(Quaternion);
                    }
                    else if (functionTree.Value is ConstantValueVector)
                    {
                        ConstantValueVector val = (ConstantValueVector)functionTree.Value;
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                        ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                        ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                        innerExpressionReturn = typeof(Vector3);
                    }
                    else
                    {
                        throw new CompilerException(lineNumber, "Internal Error");
                    }
                }
                else
                {
                    switch (functionTree.Type)
                    {
                        case Tree.EntryType.Function:
                            expressionStack.Insert(0, new FunctionExpression(
                                this,
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                functionTree,
                                lineNumber,
                                localVars));
                            innerExpressionReturn = null;
                            break;

                        case Tree.EntryType.OperatorBinary:
                            expressionStack.Insert(0, new BinaryOperatorExpression(
                                this,
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                functionTree,
                                lineNumber,
                                localVars));
                            innerExpressionReturn = null;
                            break;

                        case Tree.EntryType.OperatorLeftUnary:
                            switch (functionTree.Entry)
                            {
                                case "++":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            ilgen.Emit(OpCodes.Ldc_I4_1);
                                            ilgen.Emit(OpCodes.Add);
                                            ilgen.Emit(OpCodes.Dup);
                                            SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '++' not supported for {0}", MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '++' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                case "--":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            ilgen.Emit(OpCodes.Ldc_I4_1);
                                            ilgen.Emit(OpCodes.Sub);
                                            ilgen.Emit(OpCodes.Dup);
                                            SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '--' not supported for {0}", MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '--' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                case "(string)":
                                case "(integer)":
                                case "(float)":
                                case "(vector)":
                                case "(list)":
                                case "(key)":
                                case "(quaternion)":
                                case "(rotation)":
                                    expressionStack.Insert(0, new TypecastExpression(
                                        this,
                                        compileState,
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        ilgen,
                                        functionTree,
                                        lineNumber,
                                        localVars));
                                    innerExpressionReturn = null;
                                    break;

                                default:
                                    expressionStack.Insert(0, new LeftUnaryOperators(
                                        this,
                                        compileState,
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        ilgen,
                                        functionTree,
                                        lineNumber,
                                        localVars));
                                    innerExpressionReturn = null;
                                    break;
                            }
                            break;

                        case Tree.EntryType.OperatorRightUnary:
                            switch (functionTree.Entry)
                            {
                                case "++":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            ilgen.Emit(OpCodes.Dup);
                                            ilgen.Emit(OpCodes.Ldc_I4_1);
                                            ilgen.Emit(OpCodes.Add);
                                            SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '++' not supported for {0}", MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '++' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                case "--":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            ilgen.Emit(OpCodes.Dup);
                                            ilgen.Emit(OpCodes.Ldc_I4_1);
                                            ilgen.Emit(OpCodes.Sub);
                                            SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '--' not supported for {0}", MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '--' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                default:
                                    throw new CompilerException(lineNumber, string.Format("right unary operator '{0}' not supported", functionTree.Entry));
                            }
                            break;

                        case Tree.EntryType.ReservedWord:
                            throw new CompilerException(lineNumber, string.Format("'{0}' is a reserved word", functionTree.Entry));

                        #region Constants and complex types
                        case Tree.EntryType.StringValue:
                            /* string value */
                            {
                                Tree.ConstantValueString val = (Tree.ConstantValueString)functionTree.Value;
                                ilgen.Emit(OpCodes.Ldstr, val.Value);
                                innerExpressionReturn = typeof(string);
                            }
                            break;

                        case Tree.EntryType.Rotation:
                            /* rotation */
                            if (null != functionTree.Value)
                            {
                                /* constants */
                                ConstantValueRotation val = (ConstantValueRotation)functionTree.Value;
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.W);
                                ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Quaternion);
                            }
                            else
                            {
                                expressionStack.Insert(0, new RotationExpression(
                                    this,
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree,
                                    lineNumber,
                                    localVars));
                                innerExpressionReturn = null;
                            }
                            break;

                        case Tree.EntryType.Value:
                            /* value */
                            if (functionTree.Value is ConstantValueRotation)
                            {
                                ConstantValueRotation v = (ConstantValueRotation)functionTree.Value;
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.X);
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.Y);
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.Z);
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.W);
                                ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Quaternion);
                            }
                            else if (functionTree.Value is ConstantValueVector)
                            {
                                ConstantValueVector v = (ConstantValueVector)functionTree.Value;
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.X);
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.Y);
                                ilgen.Emit(OpCodes.Ldc_R8, v.Value.Z);
                                ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Vector3);
                            }
                            else if (functionTree.Value is Tree.ConstantValueFloat)
                            {
                                ilgen.Emit(OpCodes.Ldc_R8, ((Tree.ConstantValueFloat)functionTree.Value).Value);
                                innerExpressionReturn = typeof(double);
                            }
                            else if (functionTree.Value is Tree.ConstantValueInt)
                            {
                                ilgen.Emit(OpCodes.Ldc_I4, ((Tree.ConstantValueInt)functionTree.Value).Value);
                                innerExpressionReturn = typeof(int);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format("invalid value"));
                            }
                            break;

                        case Tree.EntryType.Vector:
                            /* three components */
                            if (null != functionTree.Value)
                            {
                                /* constants */
                                ConstantValueVector val = (ConstantValueVector)functionTree.Value;
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                                ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                                ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Vector3);
                            }
                            else
                            {
                                expressionStack.Insert(0, new VectorExpression(
                                    this,
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree,
                                    lineNumber,
                                    localVars));
                                innerExpressionReturn = null;
                            }
                            break;
                        #endregion

                        case Tree.EntryType.Variable:
                            /* variable */
                            try
                            {
                                object v = localVars[functionTree.Entry];
                                innerExpressionReturn = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                            }
                            catch (Exception e)
                            {
                                throw new CompilerException(lineNumber, string.Format("Variable '{0}' not defined", functionTree.Entry));
                            }
                            break;

                        case Tree.EntryType.Level:
                            switch (functionTree.Entry)
                            {
                                case "[":
                                    /* we got a list */
                                    expressionStack.Insert(0, new ListExpression(
                                        this,
                                        compileState,
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        ilgen,
                                        functionTree,
                                        lineNumber,
                                        localVars));
                                    innerExpressionReturn = null;
                                    break;

                                default:
                                    throw new CompilerException(lineNumber, string.Format("unexpected level entry '{0}'", functionTree.Entry));
                            }
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format("unknown '{0}'", functionTree.Entry));
                    }
                }
            }
        }
    }
}
