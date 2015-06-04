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
        void ProcessExpression(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            Type expectedType,
            int startAt,
            int endAt,
            List<string> functionLine,
            Dictionary<string, object> localVars)
        {
            List<string> actFunctionLine = new List<string>();
            if(startAt > endAt)
            {
                throw new NotImplementedException();
            }
            
            Tree expressionTree = new Tree(functionLine.GetRange(startAt, endAt - startAt + 1), m_OpChars, m_SingleOps, m_NumericChars);
            solveTree(compileState, expressionTree);
            ProcessExpression(compileState, scriptTypeBuilder, stateTypeBuilder, ilgen, expectedType, expressionTree, localVars);
        }

        void ProcessExpression(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            Type expectedType,
            Tree functionTree,
            Dictionary<string, object> localVars)
        {
            Type actualReturnType = typeof(void);
            ProcessCasts(
                ilgen, 
                expectedType, 
                actualReturnType);
        }

        void ProcessCasts(ILGenerator ilgen, Type toType, Type fromType)
        {
            /* value is on stack before */
            if(toType == fromType)
            {
            }
            else if(toType == typeof(void))
            {
                ilgen.Emit(OpCodes.Pop);
            }
            else if(toType == typeof(string))
            {
                if(fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Call, typeof(string).GetMethod("ToString", new Type[0]));
                }
                else if(fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Call, typeof(double).GetMethod("ToString", new Type[0]));
                }
                else if(fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Vector3).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(AnArray))
                {
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("ToString", new Type[0]));
                }
                else if(fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if(toType == typeof(int))
            {
                if(fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(string).GetProperty("Length").GetGetMethod());
                }
                else if(fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Conv_I4);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if(toType == typeof(bool))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(string).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Clt);
                }
                else if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Clt_Un);
                }
                else if (fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Ldc_R8, 0f);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_1);
                    ilgen.Emit(OpCodes.Xor);
                }
                else if (fromType == typeof(AnArray))
                {
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetProperty("Count").GetGetMethod());
                }
                    /*
                else if (fromType == typeof(Quaternion))
                {
                }
                     */
                else if (fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Vector3).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_1);
                    ilgen.Emit(OpCodes.Xor);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if(toType == typeof(double))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(string).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Conv_R8);
                }
                else if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Conv_R8);
                }
                else
                {
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                }
            }
            else if(toType == typeof(AnArray))
            {
                if(fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(string) }));
                }
                else if(fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(int) }));
                }
                else if(fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(Vector3) }));
                }
                else if(fromType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(Quaternion) }));
                }
                else if (fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(double) }));
                }
                else if(fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else
                {
                    throw new NotImplementedException();
                }
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
            if(v is ParameterInfo)
            {
                ilgen.Emit(OpCodes.Ldarg, ((ParameterInfo)v).Position);
                return ((ParameterInfo)v).ParameterType;
            }
            else if(v is LocalBuilder)
            {
                ilgen.Emit(OpCodes.Ldloc, (LocalBuilder)v);
                return ((LocalBuilder)v).LocalType;
            }
            else if(v is FieldBuilder)
            {
                ilgen.Emit(OpCodes.Ldfld, ((FieldBuilder)v));
                return ((FieldBuilder)v).FieldType;
            }
            else if (v is FieldInfo)
            {
                ilgen.Emit(OpCodes.Ldfld, ((FieldInfo)v));
                return ((FieldInfo)v).FieldType;
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
            List<string> functionLine,
            Dictionary<string, object> localVars)
        {
            if (functionLine[startAt + 1] == "=")
            {
                /* variable assignment */
                string varName = functionLine[startAt + 0];
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
            List<List<string>> functionBody,
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
                List<string> functionLine = new List<string>();
                LocalBuilder lb;
                switch (functionLine[0])
                {
                    /* type named things are variable declaration */
                    case "integer":
                        lb = ilgen.DeclareLocal(typeof(int));
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(int),
                                3, 
                                functionLine.Count - 2, 
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
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder,
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(Vector3), 
                                3, 
                                functionLine.Count - 2, 
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
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen, 
                                typeof(AnArray), 
                                3,
                                functionLine.Count - 2,
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
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(double), 
                                3,
                                functionLine.Count - 2,
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
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                typeof(string),
                                3,
                                functionLine.Count - 2, 
                                functionLine, 
                                localVars);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Ldstr, "");
                            /* TODO: more to do */
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "key":
                        lb = ilgen.DeclareLocal(typeof(LSLKey));
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                typeof(LSLKey),
                                3,
                                functionLine.Count - 2,
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
                        lb.SetLocalSymInfo(functionLine[1]);
                        localVars[functionLine[1]] = lb;
                        if (functionLine[2] != ";")
                        {
                            ProcessExpression(
                                compileState, 
                                scriptTypeBuilder, 
                                stateTypeBuilder, 
                                ilgen, 
                                typeof(Quaternion),
                                3, 
                                functionLine.Count - 2,
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
                            for(endoffor = 0; endoffor <= functionLine.Count; ++endoffor)
                            {
                                if(functionLine[endoffor] == ")")
                                {
                                    if(--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if(functionLine[endoffor] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if(endoffor >= functionLine.Count)
                            {
                                throw new Exception();
                            }

                            semicolon1 = functionLine.IndexOf(";");
                            semicolon2 = functionLine.IndexOf(";", semicolon1 + 1);
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
                                if (functionLine[functionLine.Count - 1] == "{")
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
                                else if(endoffor + 1 != functionLine.Count - 1)
                                {
                                    /* single statement */
                                    ProcessStatement(
                                        compileState,
                                        scriptTypeBuilder,
                                        stateTypeBuilder,
                                        ilgen, 
                                        endoffor + 1,
                                        functionLine.Count - 2,
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
                                if (functionLine[functionLine.Count - 1] == "{")
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
                                        functionLine.Count - 2,
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
                            for (endofwhile = 0; endofwhile <= functionLine.Count; ++endofwhile)
                            {
                                if (functionLine[endofwhile] == ")")
                                {
                                    if(--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine[endofwhile] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endofwhile >= functionLine.Count)
                            {
                                throw new Exception();
                            }

                            Label beginlabel = ilgen.DefineLabel();
                            Label endlabel = ilgen.DefineLabel();
                            ilgen.Emit(OpCodes.Br, endlabel);

                            ilgen.MarkLabel(beginlabel);
                            if (functionLine[functionLine.Count - 1] == "{")
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
                            else if(endofwhile + 1 != functionLine.Count - 1)
                            {
                                /* single statement */
                                ProcessStatement(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder, 
                                    ilgen, 
                                    endofwhile + 1,
                                    functionLine.Count - 2,
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
                            for (endofdo = 0; endofdo <= functionLine.Count; ++endofdo)
                            {
                                if (functionLine[endofdo] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine[endofdo] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endofdo >= functionLine.Count)
                            {
                                throw new Exception();
                            }
                            #endregion

                            #region Find while
                            if (functionLine[functionLine.Count - 1] != "{")
                            {
                                for (beginofwhile = functionLine.Count - 1; beginofwhile >= 0; --beginofwhile)
                                {
                                    if (functionLine[beginofwhile] == "(")
                                    {
                                        if (--countparens == 0)
                                        {
                                            break;
                                        }
                                    }
                                    else if (functionLine[beginofwhile] == ")")
                                    {
                                        ++countparens;
                                    }
                                }
                                if (beginofwhile < 0 || beginofwhile < endofdo + 1 || functionLine[beginofwhile - 1] != "while")
                                {
                                    throw new Exception();
                                }
                            }
                            #endregion

                            Label beginlabel = ilgen.DefineLabel();

                            ilgen.MarkLabel(beginlabel);
                            if (functionLine[functionLine.Count - 1] == "{")
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
                                if(functionLine[0] != "while")
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
                                functionLine.Count - 2,
                                functionLine,
                                localVars);
                            ilgen.Emit(OpCodes.Ldc_I4_0);
                            ilgen.Emit(OpCodes.Ceq);
                            ilgen.Emit(OpCodes.Brfalse, beginlabel);
                        }
                        break;

                    case "jump":
                        if (!labels.ContainsKey(functionLine[1]))
                        {
                            Label label = ilgen.DefineLabel();
                            labels[functionLine[1]] = label;
                        }
                        ilgen.Emit(OpCodes.Br, labels[functionLine[1]]);
                        break;

                    case "return":
                        if (returnType == typeof(void))
                        {
                            if (functionLine[1] != ";")
                            {
                                ProcessExpression(
                                    compileState,
                                    scriptTypeBuilder, 
                                    stateTypeBuilder,
                                    ilgen,
                                    typeof(void), 
                                    1, 
                                    functionLine.Count - 2, 
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
                                functionLine.Count - 2, 
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
                                functionLine.Count - 2, 
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
                                functionLine.Count - 2,
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
                                functionLine.Count - 2,
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
                                functionLine.Count - 2,
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
                                functionLine.Count - 2,
                                functionLine, 
                                localVars);
                        }
                        ilgen.Emit(OpCodes.Ret);
                        break;

                    case "state":
                        throw new NotImplementedException();
                        //break;

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
                            functionLine.Count - 2, 
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
            List<List<string>> functionBody,
            Dictionary<string, object> localVars)
        {
            Type returnType = typeof(void);
            List<string> functionDeclaration = functionBody[0];
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
                Type t;
                switch (functionDeclaration[++functionStart])
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
                        throw new CompilerException(0, "Internal Error");
                }
                /* parameter name and type in order */
                localVars[functionDeclaration[functionStart++]] = mb.GetParameters()[paramidx];
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
            AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            #region Create Script Container
            TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
            Dictionary<string, object> typeLocals = new Dictionary<string, object>();
            foreach (IScriptApi api in m_Apis)
            {
                ScriptApiName apiAttr = (ScriptApiName)api.GetType().GetCustomAttributes(typeof(ScriptApiName), false)[0];
                FieldBuilder fb = scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
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
                List<string> initargs;

                if(compileState.m_VariableInitValues.TryGetValue(varName, out initargs))
                {
                    Tree expressionTree = new Tree(initargs, m_OpChars, m_SingleOps, m_NumericChars);
                    solveTree(compileState, expressionTree);
                    if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree))
                    {
                        ProcessExpression(
                            compileState,
                            scriptTypeBuilder,
                            scriptTypeBuilder,
                            script_ilgen,
                            fb.FieldType,
                            expressionTree,
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
                    /* TODO: more to do */
                }
                else if (fb.FieldType == typeof(Vector3))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[0]));
                    /* TODO: more to do */
                }
                else if (fb.FieldType == typeof(Quaternion))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[0]));
                    /* TODO: more to do */
                }
                else if (fb.FieldType == typeof(AnArray))
                {
                    script_ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    /* TODO: more to do */
                }
                script_ilgen.Emit(OpCodes.Stfld, fb);
            }
            #endregion

            #region Function compilation
            /* we have to process the function definition first */
            foreach (KeyValuePair<string, List<List<string>>> functionKvp in compileState.m_Functions)
            {
                MethodBuilder method;
                Type returnType = typeof(void);
                List<string> functionDeclaration = functionKvp.Value[0];
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
                            throw new CompilerException(0, "Internal Error");
                    }
                }

                method = scriptTypeBuilder.DefineMethod("fn_" + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
                compileState.m_FunctionInfo[functionName] = method;
            }

            foreach (KeyValuePair<string, List<List<string>>> functionKvp in compileState.m_Functions)
            {
                List<string> functionDeclaration = functionKvp.Value[0];
                string functionName = functionDeclaration[1];
                MethodBuilder method = compileState.m_FunctionInfo[functionName];
                
                ILGenerator method_ilgen = method.GetILGenerator();
                ProcessFunction(compileState, scriptTypeBuilder, null, method, method_ilgen, functionKvp.Value, typeLocals);
                method_ilgen.Emit(OpCodes.Ret);
            }
            #endregion

            #region State compilation
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
                typeLocals = AddConstants(compileState, state, state_ilgen);
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
                    for (int pi = 0; pi < pinfo.Length; ++pi)
                    {
                        paramtypes[pi] = pinfo[pi].ParameterType;
                    }
                    MethodBuilder eventbuilder = state.DefineMethod(eventKvp.Key, MethodAttributes.Public, typeof(void), paramtypes);
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
