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
using SilverSim.Scripting.Common.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        CompilerException compilerException(LineInfo p, string message)
        {
            return new CompilerException(p.LineNumber, message);
        }

        class ILParameterInfo
        {
            public int Position;
            public Type ParameterType;

            public ILParameterInfo(Type type, int position)
            {
                ParameterType = type;
                Position = position;
            }
        }

        void ProcessExpression(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            Type expectedType,
            int startAt,
            int endAt,
            LineInfo functionLine,
            Dictionary<string, object> localVars)
        {
            List<string> actFunctionLine = new List<string>();
            if(startAt > endAt)
            {
                throw new NotImplementedException();
            }

            Tree expressionTree;
            try
            {
                expressionTree = new Tree(functionLine.Line.GetRange(startAt, endAt - startAt + 1), m_OpChars, m_SingleOps, m_NumericChars);
                solveTree(compileState, expressionTree);
            }
            catch(Exception e)
            {
                throw compilerException(functionLine, e.Message);
            }
            ProcessExpression(
                compileState, 
                scriptTypeBuilder, 
                stateTypeBuilder, 
                ilgen, 
                expectedType, 
                expressionTree,
                functionLine.LineNumber,
                localVars);
        }

        string MapType(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "integer";
            if (t == typeof(double)) return "float";
            if (t == typeof(LSLKey)) return "key";
            if (t == typeof(Quaternion)) return "rotation";
            if (t == typeof(Vector3)) return "vector";
            if (t == typeof(AnArray)) return "list";
            return "???";
        }

        void ProcessExpression(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            Type expectedType,
            Tree functionTree,
            int lineNumber,
            Dictionary<string, object> localVars)
        {
            Type retType = ProcessExpressionPart(
                compileState,
                scriptTypeBuilder,
                stateTypeBuilder,
                ilgen,
                functionTree,
                lineNumber,
                localVars);
            if(retType == typeof(string) && expectedType == typeof(LSLKey))
            {

            }
            else if(retType == typeof(LSLKey) && expectedType == typeof(string))
            {

            }
            else if(retType == typeof(int) && expectedType == typeof(double))
            {

            }
            else if(retType == typeof(bool))
            {

            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("Unsupported implicit typecast from {0} to {1}", MapType(retType), MapType(expectedType)));
            }
            ProcessCasts(
                ilgen,
                expectedType,
                retType,
                lineNumber);
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
            switch(functionTree.Type)
            {
                case Tree.EntryType.ExpressionTree:
                    return ProcessExpressionPart(
                        compileState, 
                        scriptTypeBuilder, 
                        stateTypeBuilder, 
                        ilgen,
                        functionTree.SubTree[0], 
                        lineNumber,
                        localVars);

                case Tree.EntryType.Function:
                    {
                        MethodBuilder mb = compileState.m_FunctionInfo[functionTree.Entry];

                        ParameterInfo[] pi = mb.GetParameters();

                        if (mb.DeclaringType == stateTypeBuilder)
                        {
                            ilgen.Emit(OpCodes.Ldarg_0);
                        }
                        else if(mb.DeclaringType == scriptTypeBuilder)
                        {
                            ilgen.Emit(OpCodes.Ldfld, stateTypeBuilder.GetField("Instance"));
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

                        for (int i = 0; i < functionTree.SubTree.Count; ++i)
                        {
                            Type t = ProcessExpressionPart(
                                compileState, 
                                scriptTypeBuilder,
                                stateTypeBuilder, 
                                ilgen, 
                                functionTree, 
                                lineNumber,
                                localVars);
                            if(pi[i].ParameterType == t)
                            {
                                /* fully matching */
                            }
                            else if(pi[i].ParameterType == typeof(string) && t == typeof(LSLKey))
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[] { t }));
                            }
                            else if(pi[i].ParameterType == typeof(LSLKey) && t == typeof(string))
                            {
                                ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { t }));
                            }
                            else if(pi[i].ParameterType == typeof(double) && t == typeof(int))
                            {
                                ilgen.Emit(OpCodes.Conv_R8);
                            }
                            else if(pi[i].ParameterType == typeof(AnArray))
                            {
                                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                if(t == typeof(int) || t == typeof(string) || t == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { t }));
                                }
                                else
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                                }
                            }
                            else
                            {
                                throw new ArgumentException();
                            }
                        }

                        ilgen.Emit(OpCodes.Callvirt, mb);
                        return mb.ReturnType;
                    }

                case Tree.EntryType.StringValue:
                    /* string value */
                    {
                        Tree.ConstantValueString val = (Tree.ConstantValueString)functionTree.Value;
                        ilgen.Emit(OpCodes.Ldstr, val.Value);
                        return typeof(string);
                    }

                case Tree.EntryType.OperatorBinary:
                    /* right first */
                    /* left then */
                    {
                        Type retRight = ProcessExpressionPart(
                            compileState, 
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            ilgen, 
                            functionTree.SubTree[1],
                            lineNumber,
                            localVars);

                        Type retLeft;
                        object varInfo = null;
                        if (functionTree.Entry == "=")
                        {
                            varInfo = localVars[functionTree.SubTree[0].Entry];
                            retLeft = GetVarType(scriptTypeBuilder, stateTypeBuilder, varInfo);
                        }
                        else if(functionTree.Entry == "+=" || functionTree.Entry == "-=" || functionTree.Entry == "*=" || functionTree.Entry == "/=" || functionTree.Entry == "%=")
                        {
                            if (functionTree.SubTree[0].Type != Tree.EntryType.Variable && functionTree.SubTree[0].Type != Tree.EntryType.Unknown)
                            {
                                throw new NotSupportedException();
                            }
                            else
                            {
                                varInfo = localVars[functionTree.SubTree[0].Entry];
                                retLeft = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                            }
                        }
                        else
                        {
                            retLeft = ProcessExpressionPart(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                functionTree.SubTree[0],
                                lineNumber,
                                localVars);
                        }

                        if(functionTree.Entry == "=")
                        {
                            /* skip conversion here */
                        }
                        else if(retLeft == typeof(AnArray) && retRight == typeof(AnArray))
                        {
                            /* LSL compares list length */
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("Length").GetGetMethod());
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("Length").GetGetMethod());
                            ilgen.EndScope();
                            retLeft = typeof(int);
                            retRight = typeof(int);
                        }
                        else if(retLeft == retRight)
                        {

                        }
                        else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.EndScope();
                            retRight = typeof(string);
                        }
                        else if(retLeft == typeof(LSLKey) && retRight == typeof(string))
                        {
                            ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                            retLeft = typeof(string);
                        }
                        else if(retLeft == typeof(double) && retRight == typeof(int))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Conv_R8);
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.EndScope();
                            retRight = typeof(double);
                        }
                        else if(retLeft == typeof(int) && retRight == typeof(double))
                        {
                            ilgen.Emit(OpCodes.Conv_R8);
                            retLeft = typeof(double);
                        }
                        else if(retRight == typeof(AnArray))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            if (retLeft == typeof(LSLKey) || retLeft == typeof(Quaternion) || retLeft == typeof(Vector3))
                            {
                                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                ilgen.Emit(OpCodes.Dup);
                                ilgen.Emit(OpCodes.Ldloc, lb);
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            else
                            {
                                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                ilgen.Emit(OpCodes.Dup);
                                ilgen.Emit(OpCodes.Ldloc, lb);
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { retLeft }));
                            }
                            ilgen.EndScope();
                        }
                        else if(retLeft == typeof(AnArray))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                            ilgen.Emit(OpCodes.Dup);
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            if (retRight == typeof(LSLKey) || retRight == typeof(Quaternion) || retRight == typeof(Vector3))
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            else
                            {
                                ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { retRight }));
                            }
                            ilgen.EndScope();
                        }
                        else
                        {
                            throw new CompilerException(lineNumber, string.Format("implicit typecast not supported for {0} and {1} with '{2}'", MapType(retLeft), MapType(retRight), functionTree.Entry));
                        }

                        {
                            ilgen.BeginScope();
                            LocalBuilder lbLeft = ilgen.DeclareLocal(retLeft);
                            LocalBuilder lbRight = ilgen.DeclareLocal(retRight);
                            ilgen.Emit(OpCodes.Stloc, lbRight);
                            ilgen.Emit(OpCodes.Stloc, lbLeft);
                            ilgen.Emit(OpCodes.Ldloc, lbLeft);
                            ilgen.Emit(OpCodes.Ldloc, lbRight);
                            ilgen.EndScope();
                        }

                        switch(functionTree.Entry)
                        {
                            case "+":
                                if(retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Add);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Addition", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '+' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "-":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Sub);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Subtraction", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '-' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "*":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Mul);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Multiplication", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '*' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "/":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Div);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Division", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '/' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "%":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Rem);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Remainder", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '%' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "<<":
                                if (retLeft == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Shl);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '<<' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case ">>":
                                if (retLeft == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Shr);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '>>' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "==":
                                if (retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else if(retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetMethod("Equals", new Type[] { retLeft }));
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Equality", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '==' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "!=":
                                if (retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Ceq);
                                    ilgen.Emit(OpCodes.Ldc_I4_0);
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else if (retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetMethod("Equals", new Type[] { retLeft }));
                                    ilgen.Emit(OpCodes.Ldc_I4_0);
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Inequality", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '!=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "<=":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Cgt);
                                    ilgen.Emit(OpCodes.Ldc_I4_0);
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_LessThanOrEqual", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '<=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "<":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Clt);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_LessThan", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '<' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case ">":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Cgt);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_GreaterThan", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '>' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case ">=":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Clt);
                                    ilgen.Emit(OpCodes.Ldc_I4_0);
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_GreaterThanOrEqual", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '>=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "&&":
                                if (typeof(int) != retLeft)
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ProcessCasts(ilgen, typeof(bool), retLeft, lineNumber);
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                if(typeof(int) != retRight)
                                {
                                    ProcessCasts(ilgen, typeof(bool), retRight, lineNumber);
                                }
                                ilgen.Emit(OpCodes.And);
                                return retLeft;

                            case "&":
                                if(typeof(int) == retLeft)
                                {
                                    ilgen.Emit(OpCodes.And);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '&' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "|":
                                if(typeof(int) == retLeft)
                                {
                                    ilgen.Emit(OpCodes.Or);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '|' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "^":
                                if(typeof(int) == retLeft)
                                {
                                    ilgen.Emit(OpCodes.Xor);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '^' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "||":
                                if (typeof(int) != retLeft)
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ProcessCasts(ilgen, typeof(bool), retLeft, lineNumber);
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                if (typeof(int) != retRight)
                                {
                                    ProcessCasts(ilgen, typeof(bool), retRight, lineNumber);
                                }
                                ilgen.Emit(OpCodes.Or);
                                return retLeft;

                            case "=":
                                if(retLeft == retRight)
                                {
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                                }
                                else if(retLeft == typeof(double) && retRight == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                                }
                                else if (retLeft == typeof(LSLKey) && retRight == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { typeof(string) }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    if(typeof(int) == retRight || typeof(double) == retRight || typeof(string) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { retRight }));
                                    }
                                    else if(typeof(LSLKey) == retRight || typeof(Vector3) == retRight || typeof(Quaternion) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { typeof(IValue) }));
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                ilgen.Emit(OpCodes.Dup);
                                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);

                                return retLeft;

                            case "+=":
                                if(retLeft == retRight)
                                {
                                }
                                else if(retLeft == typeof(double) && retRight == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                                }
                                else if (retLeft == typeof(LSLKey) && retRight == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { typeof(string) }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    if(typeof(int) == retRight || typeof(double) == retRight || typeof(string) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { retRight }));
                                    }
                                    else if(typeof(LSLKey) == retRight || typeof(Vector3) == retRight || typeof(Quaternion) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { typeof(IValue) }));
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '+=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '+=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                if(retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Add);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Addition", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '+=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                ilgen.Emit(OpCodes.Dup);
                                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);

                                return retLeft;

                            case "-=":
                                if(retLeft == retRight)
                                {
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                                }
                                else if(retLeft == typeof(double) && retRight == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                                }
                                else if (retLeft == typeof(LSLKey) && retRight == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { typeof(string) }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    if(typeof(int) == retRight || typeof(double) == retRight || typeof(string) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { retRight }));
                                    }
                                    else if(typeof(LSLKey) == retRight || typeof(Vector3) == retRight || typeof(Quaternion) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { typeof(IValue) }));
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '-=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                                if(retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Sub);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Subtraction", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '-=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                ilgen.Emit(OpCodes.Dup);
                                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);

                                return retLeft;

                            case "*=":
                                if(retLeft == retRight)
                                {
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                                }
                                else if(retLeft == typeof(double) && retRight == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                                }
                                else if (retLeft == typeof(LSLKey) && retRight == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { typeof(string) }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    if(typeof(int) == retRight || typeof(double) == retRight || typeof(string) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { retRight }));
                                    }
                                    else if(typeof(LSLKey) == retRight || typeof(Vector3) == retRight || typeof(Quaternion) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { typeof(IValue) }));
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '*=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '*=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                if(retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Mul);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Multiplication", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '*=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                ilgen.Emit(OpCodes.Dup);
                                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);

                                return retLeft;

                            case "/=":
                                if(retLeft == retRight)
                                {
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                                }
                                else if(retLeft == typeof(double) && retRight == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                                }
                                else if (retLeft == typeof(LSLKey) && retRight == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { typeof(string) }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    if(typeof(int) == retRight || typeof(double) == retRight || typeof(string) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { retRight }));
                                    }
                                    else if(typeof(LSLKey) == retRight || typeof(Vector3) == retRight || typeof(Quaternion) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { typeof(IValue) }));
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '/=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                                if(retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Div);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Division", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '/=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                ilgen.Emit(OpCodes.Dup);
                                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);

                                return retLeft;

                            case "%=":
                                if(retLeft == retRight)
                                {
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);
                                }
                                else if(retLeft == typeof(double) && retRight == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                                }
                                else if (retLeft == typeof(LSLKey) && retRight == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { typeof(string) }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    if(typeof(int) == retRight || typeof(double) == retRight || typeof(string) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { retRight }));
                                    }
                                    else if(typeof(LSLKey) == retRight || typeof(Vector3) == retRight || typeof(Quaternion) == retRight)
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[1] { typeof(IValue) }));
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '%=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '%=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));

                                }
                                if(retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Rem);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Remainder", new Type[] { retLeft, retRight }));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '%=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                ilgen.Emit(OpCodes.Dup);
                                SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo);

                                return retLeft;

                            default:
                                throw new CompilerException(lineNumber, string.Format("operator '{0}' not supported", functionTree.Entry));
                        }
                    }

                case Tree.EntryType.OperatorLeftUnary:
                    {
                        Type ret;

                        switch (functionTree.Entry)
                        {
                            case "!":
                                ret = ProcessExpressionPart(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree.SubTree[0],
                                    lineNumber,
                                    localVars);
                                if (ret == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Ldc_I4_0);
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '!' not supported for {0}", MapType(ret)));
                                }
                                return ret;

                            case "~":
                                ret = ProcessExpressionPart(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree.SubTree[0],
                                    lineNumber,
                                    localVars);
                                if (ret == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Neg);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '~' not supported for {0}", MapType(ret)));
                                }
                                return ret;

                            case "++":
                                if(functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                {
                                    object v = localVars[functionTree.SubTree[0].Entry];
                                    ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    if(ret == typeof(int))
                                    {
                                        ilgen.Emit(OpCodes.Ldc_I4_1);
                                        ilgen.Emit(OpCodes.Add);
                                        ilgen.Emit(OpCodes.Dup);
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '++' not supported for {0}", MapType(ret)));
                                    }
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '++' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                }
                                return ret;

                            case "--":
                                if(functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                {
                                    object v = localVars[functionTree.SubTree[0].Entry];
                                    ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    if(ret == typeof(int))
                                    {
                                        ilgen.Emit(OpCodes.Ldc_I4_1);
                                        ilgen.Emit(OpCodes.Sub);
                                        ilgen.Emit(OpCodes.Dup);
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '--' not supported for {0}", MapType(ret)));
                                    }
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '--' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                }
                                return ret;

                            default:
                                throw new CompilerException(lineNumber, string.Format("operator '{0}' not supported", functionTree.Entry));
                        }
                    }

                case Tree.EntryType.OperatorRightUnary:
                    {
                        Type ret;
                        switch (functionTree.Entry)
                        {
                            case "++":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                {
                                    object v = localVars[functionTree.SubTree[0].Entry];
                                    ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    if (ret == typeof(int))
                                    {
                                        ilgen.Emit(OpCodes.Dup);
                                        ilgen.Emit(OpCodes.Ldc_I4_1);
                                        ilgen.Emit(OpCodes.Add);
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '++' not supported for {0}", MapType(ret)));
                                    }
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '++' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                }
                                return ret;

                            case "--":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                {
                                    object v = localVars[functionTree.SubTree[0].Entry];
                                    ret = GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    if (ret == typeof(int))
                                    {
                                        ilgen.Emit(OpCodes.Dup);
                                        ilgen.Emit(OpCodes.Ldc_I4_1);
                                        ilgen.Emit(OpCodes.Sub);
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '--' not supported for {0}", MapType(ret)));
                                    }
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '--' not supported for '{0}'", functionTree.SubTree[0].Entry));
                                }
                                return ret;

                            default:
                                throw new CompilerException(lineNumber, string.Format("operator '{0}' not supported", functionTree.Entry));
                        }
                    }

                case Tree.EntryType.ReservedWord:
                    throw new CompilerException(lineNumber, string.Format("'{0}' is a reserved word", functionTree.Entry));

                case Tree.EntryType.Rotation:
                    /* rotation */
                    {
                        if(null != functionTree.Value)
                        {
                            /* constants */
                            ConstantValueRotation val = (ConstantValueRotation)functionTree.Value;
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.W);
                            ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                        }
                        else
                        {
                            Type ret;

                            for (int i = 0; i < 4; ++i)
                            {
                                ret = ProcessExpressionPart(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree.SubTree[i],
                                    lineNumber,
                                    localVars);
                                if (ret == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if (ret != typeof(double))
                                {
                                    throw new CompilerException(lineNumber, string.Format("implicit typecast from {0} to double not supported", MapType(ret)));
                                }
                            }

                            ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                        }
                    }
                    return typeof(Quaternion);

                case Tree.EntryType.Value:
                    /* value */
                    if(functionTree.Value is ConstantValueRotation)
                    {
                        ConstantValueRotation v = (ConstantValueRotation)functionTree.Value;
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.X);
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.Y);
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.Z);
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.W);
                        ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                        return typeof(Quaternion);

                    }
                    else if(functionTree.Value is ConstantValueVector)
                    {
                        ConstantValueVector v = (ConstantValueVector)functionTree.Value;
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.X);
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.Y);
                        ilgen.Emit(OpCodes.Ldc_R8, v.Value.Z);
                        ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                        return typeof(Vector3);
                    }
                    else if(functionTree.Value is Tree.ConstantValueFloat)
                    {
                        ilgen.Emit(OpCodes.Ldc_R8, ((Tree.ConstantValueFloat)functionTree.Value).Value);
                        return typeof(double);
                    }
                    else if(functionTree.Value is Tree.ConstantValueInt)
                    {
                        ilgen.Emit(OpCodes.Ldc_I4, ((Tree.ConstantValueInt)functionTree.Value).Value);
                        return typeof(int);
                    }
                    else
                    {
                        throw new CompilerException(lineNumber, string.Format("invalid value"));
                    }

                case Tree.EntryType.Variable:
                    /* variable */
                    {
                        object v = localVars[functionTree.SubTree[0].Entry];
                        return GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    }

                case Tree.EntryType.Vector:
                    /* three components */
                    {
                        if(null != functionTree.Value)
                        {
                            /* constants */
                            ConstantValueVector val = (ConstantValueVector)functionTree.Value;
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                            ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                            ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                        }
                        else
                        {
                            Type ret;
                            for (int i = 0; i < 3; ++i)
                            {
                                ret = ProcessExpressionPart(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree.SubTree[i],
                                    lineNumber,
                                    localVars);
                                if (ret == typeof(int))
                                {
                                    ilgen.Emit(OpCodes.Conv_R8);
                                }
                                else if (ret != typeof(double))
                                {
                                    throw new CompilerException(lineNumber, string.Format("implicit typecast from {0} to double not supported", MapType(ret)));
                                }
                            }

                            ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                        }
                    }
                    return typeof(Vector3);

                default:
                    throw new CompilerException(lineNumber, string.Format("unexpected '{0}'", functionTree.Entry));
            }
        }

        void ProcessCasts(ILGenerator ilgen, Type toType, Type fromType, int lineNumber)
        {
            /* value is on stack before */
            if(toType == fromType)
            {
            }
            else if(toType == typeof(void))
            {
                ilgen.Emit(OpCodes.Pop);
            }
            else if(fromType == typeof(void))
            {
                throw new CompilerException(lineNumber, string.Format("function does not return anything"));
            }
            else if(toType == typeof(string))
            {
                if(fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetMethod("ToString", new Type[0]));
                }
                else if(fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(double).GetMethod("ToString", new Type[0]));
                }
                else if(fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(Vector3).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(Quaternion).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(AnArray))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("ToString", new Type[0]));
                }
                else if(fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if(toType == typeof(int))
            {
                if(fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod());
                }
                else if(fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Conv_I4);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if(toType == typeof(bool))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Ldc_R8, 0f);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(AnArray))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("Count").GetGetMethod());
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(Vector3).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Ldc_R8, 0f);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if(toType == typeof(double))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Conv_R8);
                }
                else if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Conv_R8);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if(toType == typeof(Vector3))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Vector3).GetMethod("Parse", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if(toType == typeof(Quaternion))
            {
                if(fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("Parse", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if(toType == typeof(AnArray))
            {
                if(fromType == typeof(string) || fromType == typeof(int) || fromType == typeof(double))
                {
                    ilgen.BeginScope();
                    LocalBuilder lb = ilgen.DeclareLocal(fromType);
                    ilgen.Emit(OpCodes.Stloc, lb);
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { fromType }));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.EndScope();
                }
                else if(fromType == typeof(Vector3) || fromType == typeof(Quaternion) || fromType == typeof(LSLKey))
                {
                    ilgen.BeginScope();
                    LocalBuilder lb = ilgen.DeclareLocal(fromType);
                    ilgen.Emit(OpCodes.Stloc, lb);
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.EndScope();
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
            }
        }

        Type GetVarType(
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            object v)
        {
            if (v is ILParameterInfo)
            {
                return ((ILParameterInfo)v).ParameterType;
            }
            else if (v is LocalBuilder)
            {
                return ((LocalBuilder)v).LocalType;
            }
            else if (v is FieldBuilder)
            {
                return ((FieldBuilder)v).FieldType;
            }
            else if (v is FieldInfo)
            {
                return ((FieldInfo)v).FieldType;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        Type GetVarToStack(
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen, 
            object v)
        {
            Type retType;
            if (v is ILParameterInfo)
            {
                ilgen.Emit(OpCodes.Ldarg, ((ILParameterInfo)v).Position);
                retType = ((ILParameterInfo)v).ParameterType;
            }
            else if(v is LocalBuilder)
            {
                ilgen.Emit(OpCodes.Ldloc, (LocalBuilder)v);
                retType = ((LocalBuilder)v).LocalType;
            }
            else if(v is FieldBuilder)
            {
                ilgen.Emit(OpCodes.Ldfld, ((FieldBuilder)v));
                retType = ((FieldBuilder)v).FieldType;
            }
            else if (v is FieldInfo)
            {
                ilgen.Emit(OpCodes.Ldfld, ((FieldInfo)v));
                retType = ((FieldInfo)v).FieldType;
            }
            else
            {
                throw new NotImplementedException();
            }
            if(retType == typeof(AnArray))
            {
                /* list has deep copying */
                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { retType }));
            }
            return retType;
        }

        void SetVarFromStack(
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            object v)
        {
            if (v is ILParameterInfo)
            {
                ilgen.Emit(OpCodes.Starg, ((ILParameterInfo)v).Position);
            }
            else if (v is LocalBuilder)
            {
                ilgen.Emit(OpCodes.Stloc, (LocalBuilder)v);
            }
            else if (v is FieldBuilder)
            {
                ilgen.Emit(OpCodes.Stfld, ((FieldBuilder)v));
            }
            else if (v is FieldInfo)
            {
                ilgen.Emit(OpCodes.Stfld, ((FieldInfo)v));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void ProcessStatement(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            int startAt,
            int endAt,
            LineInfo functionLine,
            Dictionary<string, object> localVars)
        {
            if (functionLine.Line[startAt + 1] == "=")
            {
                /* variable assignment */
                string varName = functionLine.Line[startAt + 0];
                if (localVars[varName] is LocalBuilder)
                {
                    LocalBuilder lb = (LocalBuilder)localVars[varName];
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        lb.GetType(),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    ilgen.Emit(OpCodes.Stloc, lb);
                }
                else if (localVars[varName] is FieldInfo)
                {
                    FieldInfo fi = (FieldInfo)localVars[varName];
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        fi.GetType(),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    ilgen.Emit(OpCodes.Stfld, fi);
                }
                else if (localVars[varName] is FieldBuilder)
                {
                    FieldBuilder fi = (FieldBuilder)localVars[varName];
                    ProcessExpression(
                        compileState,
                        scriptTypeBuilder,
                        stateTypeBuilder,
                        ilgen,
                        fi.GetType(),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    ilgen.Emit(OpCodes.Stfld, fi);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
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

        void ProcessBlock(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            Type returnType,
            ILGenerator ilgen,
            List<LineInfo> functionBody,
            Dictionary<string, object> localVars,
            Dictionary<string, Label> labels,
            ref int lineIndex)
        {
            Dictionary<string, Label> outerLabels = labels;
            List<string> markedLabels = new List<string>();
            /* we need a copy here */
            localVars = new Dictionary<string, object>(localVars);
            if (null != labels)
            {
                labels = new Dictionary<string, Label>(labels);
            }
            else
            {
                labels = new Dictionary<string, Label>();
            }

            for (; lineIndex < functionBody.Count; ++lineIndex)
            {
                LineInfo functionLine = functionBody[lineIndex];
                LocalBuilder lb;
                switch (functionLine.Line[0])
                {
                    /* type named things are variable declaration */
                    case "integer":
                        lb = ilgen.DeclareLocal(typeof(int));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(int),
                                3, 
                                functionLine.Line.Count - 2, 
                                functionLine, 
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "vector":
                        lb = ilgen.DeclareLocal(typeof(Vector3));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder,
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(Vector3), 
                                3, 
                                functionLine.Line.Count - 2, 
                                functionLine, 
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "list":
                        lb = ilgen.DeclareLocal(typeof(AnArray));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen, 
                                typeof(AnArray), 
                                3,
                                functionLine.Line.Count - 2,
                                functionLine, 
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "float":
                        lb = ilgen.DeclareLocal(typeof(double));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(double), 
                                3,
                                functionLine.Line.Count - 2,
                                functionLine, localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Ldc_R8, 0f);
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "string":
                        lb = ilgen.DeclareLocal(typeof(string));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                typeof(string),
                                3,
                                functionLine.Line.Count - 2, 
                                functionLine, 
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Ldstr, "");
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "key":
                        lb = ilgen.DeclareLocal(typeof(LSLKey));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                typeof(LSLKey),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[0]));
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "rotation":
                        lb = ilgen.DeclareLocal(typeof(Quaternion));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(Quaternion),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine, 
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[0]));
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "for":
                        {
                            int semicolon1, semicolon2;
                            int endoffor;
                            int countparens = 0;
                            for (endoffor = 0; endoffor <= functionLine.Line.Count; ++endoffor)
                            {
                                if (functionLine.Line[endoffor] == ")")
                                {
                                    if(--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endoffor] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endoffor >= functionLine.Line.Count)
                            {
                                throw new Exception();
                            }

                            semicolon1 = functionLine.Line.IndexOf(";");
                            semicolon2 = functionLine.Line.IndexOf(";", semicolon1 + 1);
                            if (2 != semicolon1)
                            {
                                ProcessStatement(
                                    compileState, 
                                    scriptTypeBuilder,
                                    stateTypeBuilder, 
                                    ilgen,
                                    2, 
                                    semicolon1 - 1,
                                    functionLine, 
                                    localVars);
                            }
                            Label beginlabel = ilgen.DefineLabel();

                            if (semicolon1 + 1 != semicolon2)
                            {
                                Label endlabel = ilgen.DefineLabel();
                                ilgen.Emit(OpCodes.Br, endlabel);

                                ilgen.MarkLabel(beginlabel);
                                if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                                {
                                    /* block */
                                    ilgen.BeginScope();
                                    ++lineIndex;
                                    ProcessBlock(
                                        compileState,
                                        scriptTypeBuilder, 
                                        stateTypeBuilder,
                                        returnType,
                                        ilgen,
                                        functionBody,
                                        localVars, 
                                        labels,
                                        ref lineIndex);
                                }
                                else if (endoffor + 1 != functionLine.Line.Count - 1)
                                {
                                    /* single statement */
                                    ProcessStatement(
                                        compileState,
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        ilgen, 
                                        endoffor + 1,
                                        functionLine.Line.Count - 2,
                                        functionLine, 
                                        localVars);
                                }

                                if (semicolon2 + 1 != endoffor)
                                {
                                    ProcessStatement(
                                        compileState,
                                        scriptTypeBuilder, 
                                        stateTypeBuilder, 
                                        ilgen, 
                                        semicolon2 + 1,
                                        endoffor - 1, 
                                        functionLine, 
                                        localVars);
                                }

                                ilgen.MarkLabel(endlabel);
                                ProcessExpression(
                                    compileState, 
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    typeof(int),
                                    semicolon1 + 1, 
                                    semicolon2 - 1, 
                                    functionLine,
                                    localVars);
                                ilgen.Emit(OpCodes.Ldc_I4_0);
                                ilgen.Emit(OpCodes.Ceq);
                                ilgen.Emit(OpCodes.Brfalse, beginlabel);
                            }
                            else
                            {
                                ilgen.MarkLabel(beginlabel);
                                if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                                {
                                    /* block */
                                    ilgen.BeginScope();
                                    ++lineIndex;
                                    ProcessBlock(
                                        compileState, 
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        returnType,
                                        ilgen,
                                        functionBody, 
                                        localVars,
                                        labels,
                                        ref lineIndex);
                                }
                                else
                                {
                                    /* single statement */
                                    ProcessStatement(
                                        compileState,
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        ilgen, 
                                        endoffor + 1,
                                        functionLine.Line.Count - 2,
                                        functionLine,
                                        localVars);
                                }
                                ilgen.Emit(OpCodes.Br, beginlabel);
                            }
                        }
                        break;

                    case "while":
                        {
                            int endofwhile;
                            int countparens = 0;
                            for (endofwhile = 0; endofwhile <= functionLine.Line.Count; ++endofwhile)
                            {
                                if (functionLine.Line[endofwhile] == ")")
                                {
                                    if(--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofwhile] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endofwhile >= functionLine.Line.Count)
                            {
                                throw new Exception();
                            }

                            Label beginlabel = ilgen.DefineLabel();
                            Label endlabel = ilgen.DefineLabel();
                            ilgen.Emit(OpCodes.Br, endlabel);

                            ilgen.MarkLabel(beginlabel);
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                /* block */
                                ilgen.BeginScope();
                                ++lineIndex;
                                ProcessBlock(
                                    compileState, 
                                    scriptTypeBuilder, 
                                    stateTypeBuilder,
                                    returnType,
                                    ilgen,
                                    functionBody,
                                    localVars, 
                                    labels, 
                                    ref lineIndex);
                            }
                            else if (endofwhile + 1 != functionLine.Line.Count - 1)
                            {
                                /* single statement */
                                ProcessStatement(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder, 
                                    ilgen, 
                                    endofwhile + 1,
                                    functionLine.Line.Count - 2,
                                    functionLine, 
                                    localVars);
                            }

                            ilgen.MarkLabel(endlabel);
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen,
                                typeof(bool),
                                2, 
                                endofwhile - 1, 
                                functionLine,
                                localVars);
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            ilgen.Emit(OpCodes.Brfalse, beginlabel);
                        }
                        break;

                    case "do":
                        {
                            int endofdo;
                            int beginofwhile = 0;
                            int countparens = 0;

                            #region Find end of do
                            for (endofdo = 0; endofdo <= functionLine.Line.Count; ++endofdo)
                            {
                                if (functionLine.Line[endofdo] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofdo] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endofdo >= functionLine.Line.Count)
                            {
                                throw new Exception();
                            }
                            #endregion

                            #region Find while
                            if (functionLine.Line[functionLine.Line.Count - 1] != "{")
                            {
                                for (beginofwhile = functionLine.Line.Count - 1; beginofwhile >= 0; --beginofwhile)
                                {
                                    if (functionLine.Line[beginofwhile] == "(")
                                    {
                                        if (--countparens == 0)
                                        {
                                            break;
                                        }
                                    }
                                    else if (functionLine.Line[beginofwhile] == ")")
                                    {
                                        ++countparens;
                                    }
                                }
                                if (beginofwhile < 0 || beginofwhile < endofdo + 1 || functionLine.Line[beginofwhile - 1] != "while")
                                {
                                    throw new Exception();
                                }
                            }
                            #endregion

                            Label beginlabel = ilgen.DefineLabel();

                            ilgen.MarkLabel(beginlabel);
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                /* block */
                                ilgen.BeginScope();
                                ++lineIndex;
                                ProcessBlock(
                                    compileState, 
                                    scriptTypeBuilder, 
                                    stateTypeBuilder,
                                    returnType,
                                    ilgen, 
                                    functionBody, 
                                    localVars, 
                                    labels,
                                    ref lineIndex);
                                beginofwhile = 0;
                                if(++lineIndex >= functionBody.Count)
                                {
                                    throw new Exception();
                                }
                                functionLine = functionBody[lineIndex];
                                if (functionLine.Line[0] != "while")
                                {
                                    throw new Exception();
                                }
                            }
                            else if (endofdo + 1 != beginofwhile - 1)
                            {
                                /* single statement */
                                ProcessStatement(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder, 
                                    ilgen, 
                                    endofdo + 1, 
                                    beginofwhile - 2,
                                    functionLine,
                                    localVars);
                            }

                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(bool), 
                                beginofwhile + 1,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            ilgen.Emit(OpCodes.Brfalse, beginlabel);
                        }
                        break;

                    case "jump":
                        if (!labels.ContainsKey(functionLine.Line[1]))
                        {
                            Label label = ilgen.DefineLabel();
                            labels[functionLine.Line[1]] = label;
                        }
                        ilgen.Emit(OpCodes.Br, labels[functionLine.Line[1]]);
                        break;

                    case "return":
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
                        break;

                    case "state":
                        /* when same state, the state instruction compiles to nop according to wiki */
                        if(stateTypeBuilder == scriptTypeBuilder)
                        {
                            throw compilerException(functionLine, "Global functions cannot change state");
                        }
                        ilgen.Emit(OpCodes.Ldstr, functionLine.Line[1]);
                        ilgen.Emit(OpCodes.Newobj, typeof(ChangeStateException).GetConstructor(new Type[1] { typeof(string) }));
                        ilgen.Emit(OpCodes.Throw);
                        break;

                    case "{": /* new unconditional block */
                        ilgen.BeginScope();
                        ++lineIndex;
                        ProcessBlock(
                            compileState, 
                            scriptTypeBuilder, 
                            stateTypeBuilder,
                            returnType,
                            ilgen, 
                            functionBody,
                            localVars,
                            labels, 
                            ref lineIndex);
                        break;

                    case "}": /* end unconditional block */
                        if (outerLabels != null)
                        {
                            ilgen.EndScope();
                        }
                        /* no increment here, is done outside */
                        return;

                    default:
                        ProcessStatement(
                            compileState,
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            ilgen, 
                            0,
                            functionLine.Line.Count - 2, 
                            functionLine, 
                            localVars);
                        break;
                }
            }
        }

        void ProcessFunction(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            MethodBuilder mb,
            ILGenerator ilgen,
            List<LineInfo> functionBody,
            Dictionary<string, object> localVars)
        {
            Type returnType = typeof(void);
            List<string> functionDeclaration = functionBody[0].Line;
            string functionName = functionDeclaration[1];
            int functionStart = 2;

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
                    returnType = typeof(Quaternion);
                    break;

                default:
                    functionName = functionDeclaration[0];
                    functionStart = 1;
                    break;
            }

            int paramidx = 0;
            while (functionDeclaration[++functionStart] != ")")
            {
                if(functionDeclaration[functionStart] == ",")
                {
                    ++functionStart;
                }
                Type t;
                switch (functionDeclaration[functionStart++])
                {
                    case "integer":
                        t = typeof(int);
                        break;

                    case "vector":
                        t = typeof(Vector3);
                        break;

                    case "list":
                        t = typeof(AnArray);
                        break;

                    case "float":
                        t = typeof(double);
                        break;

                    case "string":
                        t = typeof(string);
                        break;

                    case "key":
                        t = typeof(LSLKey);
                        break;

                    case "rotation":
                        t = typeof(Quaternion);
                        break;

                    default:
                        throw compilerException(functionBody[0], "Internal Error");
                }
                /* parameter name and type in order */
                localVars[functionDeclaration[functionStart]] = new ILParameterInfo(t, paramidx + 1);
            }

            int lineIndex = 1;
            ProcessBlock(
                compileState, 
                scriptTypeBuilder,
                stateTypeBuilder,
                mb.ReturnType, 
                ilgen,
                functionBody,
                localVars, 
                null, 
                ref lineIndex);

            /* we have no missing return value check right now, so we simply emit default values in that case */
            if(returnType == typeof(int))
            {
                ilgen.Emit(OpCodes.Ldc_I4_0);
            }
            else if(returnType == typeof(double))
            {
                ilgen.Emit(OpCodes.Ldc_R8, 0f);
            }
            else if(returnType == typeof(string))
            {
                ilgen.Emit(OpCodes.Ldstr);
            }
            else if(returnType == typeof(AnArray))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
            }
            else if (returnType == typeof(Vector3))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[0]));
            }
            else if (returnType == typeof(Quaternion))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[0]));
            }
            else if (returnType == typeof(LSLKey))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[0]));
            }
            ilgen.Emit(OpCodes.Ret);

        }

        Dictionary<string, object> AddConstants(CompileState compileState, TypeBuilder typeBuilder, ILGenerator ilgen)
        {
            Dictionary<string, object> localVars = new Dictionary<string, object>();
            FieldBuilder fb;
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
                                fb = typeBuilder.DefineField(f.Name, f.FieldType, f.Attributes);
                                if ((f.Attributes & FieldAttributes.Literal) != 0)
                                {
                                    fb.SetConstant(f.GetValue(null));
                                }
                                else
                                {
                                    ilgen.Emit(OpCodes.Ldfld, f);
                                    ilgen.Emit(OpCodes.Stfld, fb);
                                }
                                localVars[f.Name] = fb;
                            }
                        }
                        else
                        {
                            m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                        }
                    }
                }
            }
            return localVars;
        }

        bool AreAllVarReferencesSatisfied(CompileState cs, List<string> initedVars, Tree expressionTree)
        {
            foreach(Tree st in expressionTree.SubTree)
            {
                if(!AreAllVarReferencesSatisfied(cs, initedVars, st))
                {
                    return false;
                }
                else if(st.Type == Tree.EntryType.Variable || st.Type == Tree.EntryType.Unknown)
                {
                    if(cs.m_VariableDeclarations.ContainsKey(st.Entry) &&
                        !initedVars.Contains(st.Entry))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        IScriptAssembly PostProcess(CompileState compileState, AppDomain appDom, UUID assetID, bool forcedSleepDefault)
        {
            string assetAssemblyName = "Script." + assetID.ToString().Replace("-", "_");
            AssemblyName aName = new AssemblyName(assetAssemblyName);
            AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            #region Create Script Container
            TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
            Dictionary<string, object> typeLocals = new Dictionary<string, object>();
            foreach (IScriptApi api in m_Apis)
            {
                ScriptApiName apiAttr = (ScriptApiName)api.GetType().GetCustomAttributes(typeof(ScriptApiName), false)[0];
                FieldBuilder fb = scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
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
            typeLocals = AddConstants(compileState, scriptTypeBuilder, script_ilgen);
            #endregion

            Dictionary<string, Type> stateTypes = new Dictionary<string, Type>();

            #region Globals generation
            foreach(KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
            {
                FieldBuilder fb = scriptTypeBuilder.DefineField("var_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public);
                compileState.m_VariableFieldInfo[variableKvp.Key] = fb;
                typeLocals[variableKvp.Key] = fb;
            }

            List<string> varIsInited = new List<string>();
            List<string> varsToInit = new List<string>(compileState.m_VariableInitValues.Keys);

            while(varsToInit.Count != 0)
            {
                string varName = varsToInit[0];
                varsToInit.RemoveAt(0);

                FieldBuilder fb = compileState.m_VariableFieldInfo[varName];
                LineInfo initargs;

                if(compileState.m_VariableInitValues.TryGetValue(varName, out initargs))
                {
                    Tree expressionTree;
                    try
                    {
                        expressionTree = new Tree(initargs.Line, m_OpChars, m_SingleOps, m_NumericChars);
                        solveTree(compileState, expressionTree);
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
                    }
                    else
                    {
                        /* push back that var. We got it too early. */
                        varsToInit.Add(varName);
                    }
                }
                else if(fb.FieldType == typeof(int))
                {
                    script_ilgen.Emit(OpCodes.Ldc_I4_0);
                }
                else if(fb.FieldType == typeof(double))
                {
                    script_ilgen.Emit(OpCodes.Ldc_R8, 0f);
                }
                else if (fb.FieldType == typeof(string))
                {
                    script_ilgen.Emit(OpCodes.Ldstr, "");
                }
                else if (fb.FieldType == typeof(Vector3))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[0]));
                }
                else if (fb.FieldType == typeof(Quaternion))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[0]));
                }
                else if (fb.FieldType == typeof(AnArray))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                }
                script_ilgen.Emit(OpCodes.Stfld, fb);
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
                int functionStart = 2;

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
                        returnType = typeof(Quaternion);
                        break;

                    default:
                        functionName = functionDeclaration[0];
                        functionStart = 1;
                        break;
                }
                List<Type> paramTypes = new List<Type>();
                List<string> paramName = new List<string>();
                while (functionDeclaration[++functionStart] != ")")
                {
                    switch (functionDeclaration[++functionStart])
                    {
                        case "integer":
                            paramTypes.Add(typeof(int));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        case "vector":
                            paramTypes.Add(typeof(Vector3));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        case "list":
                            paramTypes.Add(typeof(AnArray));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        case "float":
                            paramTypes.Add(typeof(double));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        case "string":
                            paramTypes.Add(typeof(string));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        case "key":
                            paramTypes.Add(typeof(LSLKey));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        case "rotation":
                            paramTypes.Add(typeof(Quaternion));
                            paramName.Add(functionDeclaration[++functionStart]);
                            break;

                        default:
                            throw compilerException(functionKvp.Value[0], "Internal Error");
                    }
                }

                method = scriptTypeBuilder.DefineMethod("fn_" + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
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
                    event_ilgen.Emit(OpCodes.Ret);
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
                ScriptApiName apiAttr = (ScriptApiName)api.GetType().GetCustomAttributes(typeof(ScriptApiName), false)[0];
                FieldInfo info = t.GetField(apiAttr.Name, BindingFlags.Static | BindingFlags.Public);
                info.SetValue(null, api);
            }
            #endregion

            return new LSLScriptAssembly(ab, t, stateTypes, forcedSleepDefault);
        }

        class LSLScriptAssembly : IScriptAssembly
        {
            Assembly m_Assembly;
            Type m_Script;
            Dictionary<string, Type> m_StateTypes;
            bool m_ForcedSleep;

            public LSLScriptAssembly(Assembly assembly, Type script, Dictionary<string, Type> stateTypes, bool forcedSleep)
            {
                m_Assembly = assembly;
                m_Script = script;
                m_StateTypes = stateTypes;
                m_ForcedSleep = forcedSleep;
            }

            public ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item)
            {
                Script m_Script = new Script(objpart, item, m_ForcedSleep);
                foreach (KeyValuePair<string, Type> t in m_StateTypes)
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
