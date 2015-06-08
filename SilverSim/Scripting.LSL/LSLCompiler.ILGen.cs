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

/*
 * Operator Overloads
== op_Equality
!= op_Inequality
>  op_GreaterThan
<  op_LessThan
>= op_GreaterThanOrEqual
<= op_LessThanOrEqual
&  op_BitwiseAnd
|  op_BitwiseOr
+  op_Addition
-  op_Subtraction
/  op_Division
%  op_Modulus
*  op_Multiply
<< op_LeftShift
>> op_RightShift
^  op_ExclusiveOr
-  op_UnaryNegation
+  op_UnaryPlus
!  op_LogicalNot
~  op_OnesComplement
   op_False
   op_True
++ op_Increment
-- op_Decrement
 */

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        CompilerException compilerException(LineInfo p, string message)
        {
            return new CompilerException(p.LineNumber, message);
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
                List<string> expressionLine = functionLine.Line.GetRange(startAt, endAt - startAt + 1);
                CollapseStringConstants(expressionLine);
                expressionTree = new Tree(expressionLine, m_OpChars, m_SingleOps, m_NumericChars);
                solveTree(compileState, expressionTree, localVars.Keys);
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
            ProcessImplicitCasts(
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
            if (functionTree.Value != null)
            {
                if(functionTree.Value is Tree.ConstantValueFloat)
                {
                    ilgen.Emit(OpCodes.Ldc_R8, ((Tree.ConstantValueFloat)functionTree.Value).Value);
                    return typeof(double);
                }
                else if(functionTree.Value is Tree.ConstantValueInt)
                {
                    ilgen.Emit(OpCodes.Ldc_I4, ((Tree.ConstantValueInt)functionTree.Value).Value);
                    return typeof(int);
                }
                else if(functionTree.Value is Tree.ConstantValueString)
                {
                    ilgen.Emit(OpCodes.Ldstr, ((Tree.ConstantValueString)functionTree.Value).Value);
                    return typeof(string);
                }
                else if(functionTree.Value is ConstantValueRotation)
                {
                    ConstantValueRotation val = (ConstantValueRotation)functionTree.Value;
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.W);
                    ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                    return typeof(Quaternion);
                }
                else if(functionTree.Value is ConstantValueVector)
                {
                    ConstantValueVector val = (ConstantValueVector)functionTree.Value;
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.X);
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                    ilgen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                    ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                    return typeof(Vector3);
                }
                else
                {
                    throw new CompilerException(lineNumber, "Internal Error");
                }
            }

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

                #region Function Processing
                case Tree.EntryType.FunctionArgument:
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
                        MethodBuilder mb;
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

                            for (int i = 0; i < functionTree.SubTree.Count; ++i)
                            {
                                Type t = ProcessExpressionPart(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree.SubTree[i],
                                    lineNumber,
                                    localVars);
                                try
                                {
                                    ProcessImplicitCasts(ilgen, pi[i].Value, t, lineNumber);
                                }
                                catch
                                {
                                    throw new CompilerException(lineNumber, 
                                        string.Format("No implicit cast from {0} to {1} possible for parameter '{2}' of function '{3}'", 
                                            MapType(t),
                                            MapType(pi[i].Value),
                                            pi[i].Key,
                                            functionTree.Entry));
                                }
                            }

                            ilgen.Emit(OpCodes.Callvirt, mb);
                            return signatureInfo.Key;
                        }
                        else if(m_MethodNames.Contains(functionTree.Entry))
                        {
                            foreach(KeyValuePair<IScriptApi, MethodInfo> kvp in m_Methods)
                            {
                                if(kvp.Value.Name == functionTree.Entry)
                                {
                                    ParameterInfo[] pi = kvp.Value.GetParameters();
                                    if(pi.Length - 1 == functionTree.SubTree.Count)
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
                                            Type t = ProcessExpressionPart(
                                                compileState,
                                                scriptTypeBuilder,
                                                stateTypeBuilder,
                                                ilgen,
                                                functionTree.SubTree[i],
                                                lineNumber,
                                                localVars);
                                            ProcessImplicitCasts(ilgen, pi[i + 1].ParameterType, t, lineNumber);
                                        }
                                        ilgen.Emit(OpCodes.Callvirt, kvp.Value);
                                        return kvp.Value.ReturnType;
                                    }
                                }
                            }
                            throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}", functionTree.Entry));
                        }
                        throw new CompilerException(lineNumber, string.Format("No function {0} defined", functionTree.Entry));
                    }
                #endregion

                #region Binary operators
                case Tree.EntryType.OperatorBinary:
                    /* right first */
                    /* left then */
                    if(functionTree.Entry == "!=" || functionTree.Entry == "==")
                    {
                        bool allLeftHandElementsConstant = false;
                        bool allRightHandElementsConstant = false;
                        bool leftIsKnownList = false;
                        bool rightIsKnownList = false;
                        /* optimize list compares for constant parameters */
                        if (functionTree.SubTree[0].Type == Tree.EntryType.Level &&
                            functionTree.SubTree[0].Entry == "[")
                        {
                            leftIsKnownList = true;
                            allLeftHandElementsConstant = true;
                            foreach(Tree lt in functionTree.SubTree[0].SubTree)
                            {
                                if(lt.Entry == "," && lt.Type == Tree.EntryType.Separator)
                                {

                                }
                                else if(lt.Value == null)
                                {
                                    allLeftHandElementsConstant = false;
                                }
                            }
                        }

                        if (functionTree.SubTree[1].Type == Tree.EntryType.Level &&
                            functionTree.SubTree[1].Entry == "[")
                        {
                            rightIsKnownList = true;
                            allRightHandElementsConstant = true;
                            foreach (Tree lt in functionTree.SubTree[1].SubTree)
                            {
                                if (lt.Entry == "," && lt.Type == Tree.EntryType.Separator)
                                {

                                }
                                else if (lt.Value == null)
                                {
                                    allRightHandElementsConstant = false;
                                }
                            }
                        }

                        if(leftIsKnownList && rightIsKnownList && allLeftHandElementsConstant && allRightHandElementsConstant)
                        {
                            if (functionTree.Entry == "==")
                            {
                                /* nothing to do actually besides just push the result of the length difference */
                                ilgen.Emit(OpCodes.Ldc_I4, functionTree.SubTree[0].SubTree.Count == functionTree.SubTree[1].SubTree.Count ? 1 : 0);
                            }
                            else
                            {
                                /* nothing to do actually besides just push the length difference */
                                ilgen.Emit(OpCodes.Ldc_I4, (functionTree.SubTree[0].SubTree.Count + 1) / 2 - (functionTree.SubTree[1].SubTree.Count + 1) / 2);
                            }
                            return typeof(int);
                        }
                        else if(leftIsKnownList && allLeftHandElementsConstant)
                        {
                            /* left hand is constant */
                            ilgen.Emit(OpCodes.Ldc_I4, (functionTree.SubTree[0].SubTree.Count + 1) / 2);
                            Type t = ProcessExpressionPart(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                functionTree.SubTree[1],
                                lineNumber,
                                localVars);
                            ilgen.Emit(OpCodes.Call, typeof(AnArray).GetProperty("Count").GetGetMethod());
                            if (t != typeof(AnArray))
                            {
                                throw new CompilerException(lineNumber, string.Format("operator '{0}' is not defined for {1} and {2}", functionTree.Entry, "list", MapType(t)));
                            }
                            if (functionTree.Entry == "==")
                            {
                                ilgen.Emit(OpCodes.Ceq);
                            }
                            else
                            {
                                ilgen.Emit(OpCodes.Sub);
                            }
                            return typeof(int);
                        }
                        else if(rightIsKnownList && allRightHandElementsConstant)
                        {
                            /* right hand is constant */
                            Type t = ProcessExpressionPart(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                functionTree.SubTree[1],
                                lineNumber,
                                localVars);
                            if (t != typeof(AnArray))
                            {
                                throw new CompilerException(lineNumber, string.Format("operator '{0}' is not defined for {1} and {2}", functionTree.Entry, "list", MapType(t)));
                            }
                            ilgen.Emit(OpCodes.Call, typeof(AnArray).GetProperty("Count").GetGetMethod());
                            ilgen.Emit(OpCodes.Ldc_I4, (functionTree.SubTree[1].SubTree.Count + 1) / 2);
                            if (functionTree.Entry == "==")
                            {
                                ilgen.Emit(OpCodes.Ceq);
                            }
                            else
                            {
                                ilgen.Emit(OpCodes.Sub);
                            }
                            return typeof(int);
                        }
                    }

                    if(functionTree.Entry == ".")
                    {
                        Type retLeft = ProcessExpressionPart(
                            compileState,
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            ilgen,
                            functionTree.SubTree[0],
                            lineNumber,
                            localVars);
                        if(functionTree.SubTree[1].Type != Tree.EntryType.Unknown &&
                            functionTree.SubTree[1].Type != Tree.EntryType.Variable)
                        {
                            throw new CompilerException(lineNumber, string.Format("'{0}' is not a member of type vector", functionTree.SubTree[1].Entry));
                        }
                        else if(typeof(Vector3) == retLeft)
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Ldloca, lb);
                            switch (functionTree.SubTree[1].Entry)
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
                                    throw new CompilerException(lineNumber, string.Format("'{0}' is not a member of type vector", functionTree.SubTree[1].Entry));
                            }
                            ilgen.EndScope();
                            return typeof(double);
                        }
                        else if(typeof(Quaternion) == retLeft)
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Ldloca, lb);
                            switch (functionTree.SubTree[1].Entry)
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
                                    throw new CompilerException(lineNumber, string.Format("'{0}' is not a member of type rotation", functionTree.SubTree[1].Entry));
                            }
                            ilgen.EndScope();
                            return typeof(double);
                        }
                        else
                        {
                            throw new CompilerException(lineNumber, string.Format("Component access with '{0}' not defined", MapType(retLeft)));
                        }
                    }
                    else
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
                        Type lookupType;
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


                        lookupType = retLeft;

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
                            ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("Count").GetGetMethod());
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("Count").GetGetMethod());
                            ilgen.EndScope();
                            retLeft = typeof(int);
                            retRight = typeof(int);
                            lookupType = retLeft;
                        }
                        #region vector * scalar or quaternion * scalar
                        else if(retLeft == typeof(Vector3) && retRight == typeof(double))
                        {

                        }
                        else if (retLeft == typeof(Quaternion) && retRight == typeof(double))
                        {

                        }
                        else if (retLeft == typeof(Vector3) && retRight == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Conv_R8);
                            retRight = typeof(double);
                        }
                        else if (retLeft == typeof(Quaternion) && retRight == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Conv_R8);
                            retRight = typeof(double);
                        }
                        #endregion
                        #region scalar * vector or scalar * quaternion
                        else if (retLeft == typeof(double) && retRight == typeof(Vector3))
                        {
                            lookupType = retRight;
                        }
                        else if (retLeft == typeof(double) && retRight == typeof(Quaternion))
                        {
                            lookupType = retRight;
                        }
                        else if (retLeft == typeof(int) && (retRight == typeof(Vector3) || retRight == typeof(Quaternion)))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retRight);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Conv_R8);
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.EndScope();
                            lookupType = retRight;
                            retLeft = typeof(double);
                        }
                        #endregion
                        #region vector * quaternion or vector / quaternion
                        else if(retLeft == typeof(Vector3) && retRight == typeof(Quaternion))
                        {

                        }
                        #endregion
                        else if (retLeft == retRight)
                        {

                        }
                        else if(retLeft == typeof(string) && retRight == typeof(LSLKey))
                        {
                            ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                            retRight = typeof(string);
                        }
                        else if(retLeft == typeof(LSLKey) && retRight == typeof(string))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.EndScope();
                            retLeft = typeof(string);
                            lookupType = retLeft;
                        }
                        else if(retLeft == typeof(double) && retRight == typeof(int))
                        {
                            ilgen.Emit(OpCodes.Conv_R8);
                            retRight = typeof(double);
                        }
                        else if(retLeft == typeof(int) && retRight == typeof(double))
                        {
                            ilgen.BeginScope();
                            LocalBuilder lb = ilgen.DeclareLocal(retLeft);
                            ilgen.Emit(OpCodes.Stloc, lb);
                            ilgen.Emit(OpCodes.Conv_R8);
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.EndScope();
                            retLeft = typeof(double);
                            lookupType = retLeft;
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
                                if(retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Add);
                                }
                                else if(retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                {
                                    if (retRight == typeof(LSLKey))
                                    {
                                        ilgen.BeginScope();
                                        LocalBuilder lb = ilgen.DeclareLocal(typeof(LSLKey));
                                        ilgen.Emit(OpCodes.Stloc, lb);
                                        ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                                        ilgen.Emit(OpCodes.Dup);
                                        ilgen.Emit(OpCodes.Ldloc, lb);
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                                        ilgen.EndScope();
                                    }
                                    else
                                    {
                                        MethodInfo mi = lookupType.GetMethod("op_Addition", new Type[] { retLeft, retRight });
                                        if (mi == null)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '+' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        retLeft = mi.ReturnType;
                                    }
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
                                    MethodInfo mi = lookupType.GetMethod("op_Subtraction", new Type[] { retLeft, retRight });
                                    if (mi == null)
                                    {
                                        throw new CompilerException(lineNumber, string.Format("internal error. operator '-' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Call, mi);
                                    retLeft = mi.ReturnType;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '-' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "*":
                                if(retLeft == typeof(int) && retRight == typeof(int))
                                {
                                    /* special LSL case */
                                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerMultiply", new Type[] { retLeft, retRight }));
                                }
                                else if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    if (retRight == typeof(Vector3) || retRight == typeof(Quaternion))
                                    {
                                        MethodInfo mi = lookupType.GetMethod("op_Multiply", new Type[] { retLeft, retRight });
                                        if (mi == null)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '*' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        retLeft = mi.ReturnType;
                                    }
                                    else
                                    {
                                        ilgen.Emit(OpCodes.Mul);
                                    }
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    MethodInfo mi = lookupType.GetMethod("op_Multiply", new Type[] { retLeft, retRight });
                                    if (mi == null)
                                    {
                                        throw new CompilerException(lineNumber, string.Format("internal error. operator '*' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Call, mi);
                                    retLeft = mi.ReturnType;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '*' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "/":
                                if(retLeft == typeof(int) && retRight == typeof(int))
                                {
                                    /* special LSL case */
                                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerDivision", new Type[] { retLeft, retRight }));
                                }
                                else if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    if (retRight == typeof(Vector3) || retRight == typeof(Quaternion))
                                    {
                                        MethodInfo mi = lookupType.GetMethod("op_Division", new Type[] { retLeft, retRight });
                                        if (mi == null)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '/' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        retLeft = mi.ReturnType;
                                    }
                                    else
                                    {
                                        ilgen.Emit(OpCodes.Div);
                                    }
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    MethodInfo mi = lookupType.GetMethod("op_Division", new Type[] { retLeft, retRight });
                                    if (mi == null)
                                    {
                                        throw new CompilerException(lineNumber, string.Format("internal error. operator '/' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Call, mi);
                                    retLeft = mi.ReturnType;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '/' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return retLeft;

                            case "%":
                                if(retLeft == typeof(int) && retRight == typeof(int))
                                {
                                    /* special LSL case */
                                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerModulus", new Type[] { retLeft, retRight }));
                                }
                                else if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Rem);
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey))
                                {
                                    MethodInfo mi = lookupType.GetMethod("op_Modulus", new Type[] { retLeft, retRight });
                                    if (mi == null)
                                    {
                                        throw new CompilerException(lineNumber, string.Format("internal error. operator '%' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Call, mi);
                                    retLeft = mi.ReturnType;
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
                                if (retLeft == typeof(int) || retLeft == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else if(retLeft == typeof(LSLKey))
                                {
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetMethod("Equals", new Type[] { retLeft }));
                                }
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Equality", new Type[] { retLeft, retRight }));
                                }
                                else if(retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetProperty("Length").GetGetMethod());
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetProperty("Length").GetGetMethod());
                                    ilgen.EndScope();
                                    ilgen.Emit(OpCodes.Ceq);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '==' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return typeof(int);

                            case "!=":
                                if (retLeft == typeof(int) || retLeft == typeof(double))
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
                                else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(string))
                                {
                                    ilgen.Emit(OpCodes.Call, retLeft.GetMethod("op_Inequality", new Type[] { retLeft, retRight }));
                                }
                                else if (retLeft == typeof(AnArray))
                                {
                                    ilgen.BeginScope();
                                    LocalBuilder lb = ilgen.DeclareLocal(retRight);
                                    ilgen.Emit(OpCodes.Stloc, lb);
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetProperty("Length").GetGetMethod());
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    ilgen.Emit(OpCodes.Callvirt, retLeft.GetProperty("Length").GetGetMethod());
                                    ilgen.EndScope();
                                    /* LSL is really about subtraction with that operator */
                                    ilgen.Emit(OpCodes.Sub);
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '!=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                }
                                return typeof(int);

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
                                return typeof(int);

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
                                return typeof(int);

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
                                return typeof(int);

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
                                return typeof(int);

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
                                return typeof(int);

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
                                return typeof(int);

                            case "=":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                {
                                    if(functionTree.SubTree[0].SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        throw new CompilerException(lineNumber, "Component of left-hand value cannot be assigned");
                                    }
                                    ProcessImplicitCasts(ilgen, typeof(double), retRight, lineNumber);
                                    ilgen.BeginScope();
                                    LocalBuilder lbvar = ilgen.DeclareLocal(typeof(double));
                                    ilgen.Emit(OpCodes.Stloc, lbvar);

                                    string varName = functionTree.SubTree[0].SubTree[0].Entry;
                                    object v = localVars[varName];
                                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, v);
                                    string fieldName;
                                    if(typeof(Vector3) == varType)
                                    {
                                        switch(functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber, 
                                                    string.Format("vector does not have component '{0}'", 
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else if(typeof(Quaternion) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("rotation does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, 
                                            string.Format("Variable '{0}' has no components",
                                                varName));
                                    }
                                    LocalBuilder lbtarget = ilgen.DeclareLocal(varType);
                                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    ilgen.Emit(OpCodes.Stloc, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloc, lbvar);
                                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloc, lbtarget);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    ProcessImplicitCasts(ilgen, retLeft, retRight, lineNumber);
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, lineNumber);
                                }

                                return retLeft;

                            case "+=":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                {
                                    if (functionTree.SubTree[0].SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        throw new CompilerException(lineNumber, "Component of left-hand value cannot be assigned");
                                    }
                                    ProcessImplicitCasts(ilgen, typeof(double), retRight, lineNumber);
                                    ilgen.BeginScope();
                                    LocalBuilder lbvar = ilgen.DeclareLocal(typeof(double));
                                    ilgen.Emit(OpCodes.Stloc, lbvar);
                                    ilgen.Emit(OpCodes.Dup, lbvar);

                                    string varName = functionTree.SubTree[0].SubTree[0].Entry;
                                    object v = localVars[varName];
                                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, v);
                                    string fieldName;
                                    if (typeof(Vector3) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("vector does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else if (typeof(Quaternion) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("rotation does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber,
                                            string.Format("Variable '{0}' has no components",
                                                varName));
                                    }
                                    LocalBuilder lbtarget = ilgen.DeclareLocal(varType);
                                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    ilgen.Emit(OpCodes.Stloc, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloc, lbvar);
                                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloc, lbtarget);
                                    ilgen.Emit(OpCodes.Add);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    ProcessImplicitCasts(ilgen, retLeft, retRight, lineNumber);
                                    if (retLeft == typeof(int) || retLeft == typeof(double))
                                    {
                                        ilgen.Emit(OpCodes.Add);
                                    }
                                    else if(retLeft == typeof(string))
                                    {
                                        ilgen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                                    }
                                    else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                    {
                                        MethodInfo mi = retLeft.GetMethod("op_Addition", new Type[] { retLeft, retRight });
                                        if (null == mi)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '+=' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        if (retLeft != mi.ReturnType)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("'+=' cannot be processed on {0} and {1}", MapType(retLeft), MapType(retRight)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '+=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, lineNumber);
                                }
                                return retLeft;

                            case "-=":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                {
                                    if (functionTree.SubTree[0].SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        throw new CompilerException(lineNumber, "Component of left-hand value cannot be assigned");
                                    }
                                    ProcessImplicitCasts(ilgen, typeof(double), retRight, lineNumber);
                                    ilgen.BeginScope();
                                    LocalBuilder lbvar = ilgen.DeclareLocal(typeof(double));
                                    ilgen.Emit(OpCodes.Stloc, lbvar);
                                    ilgen.Emit(OpCodes.Dup, lbvar);

                                    string varName = functionTree.SubTree[0].SubTree[0].Entry;
                                    object v = localVars[varName];
                                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, v);
                                    string fieldName;
                                    if (typeof(Vector3) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("vector does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else if (typeof(Quaternion) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("rotation does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber,
                                            string.Format("Variable '{0}' has no components",
                                                varName));
                                    }
                                    LocalBuilder lbtarget = ilgen.DeclareLocal(varType);
                                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    ilgen.Emit(OpCodes.Stloc, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloc, lbvar);
                                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloc, lbtarget);
                                    ilgen.Emit(OpCodes.Sub);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    ProcessImplicitCasts(ilgen, retLeft, retRight, lineNumber);
                                    if (retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                    {
                                        ilgen.Emit(OpCodes.Sub);
                                    }
                                    else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                    {
                                        MethodInfo mi = retLeft.GetMethod("op_Subtraction", new Type[] { retLeft, retRight });
                                        if (null == mi)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '-=' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        if (retLeft != mi.ReturnType)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '-=' cannot be processed on {0} and {1}", MapType(retLeft), MapType(retRight)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '-=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, lineNumber);
                                }
                                return retLeft;

                            case "*=":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                {
                                    if (functionTree.SubTree[0].SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        throw new CompilerException(lineNumber, "Component of left-hand value cannot be assigned");
                                    }
                                    ProcessImplicitCasts(ilgen, typeof(double), retRight, lineNumber);
                                    ilgen.BeginScope();
                                    LocalBuilder lbvar = ilgen.DeclareLocal(typeof(double));
                                    ilgen.Emit(OpCodes.Stloc, lbvar);
                                    ilgen.Emit(OpCodes.Dup, lbvar);

                                    string varName = functionTree.SubTree[0].SubTree[0].Entry;
                                    object v = localVars[varName];
                                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, v);
                                    string fieldName;
                                    if (typeof(Vector3) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("vector does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else if (typeof(Quaternion) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("rotation does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber,
                                            string.Format("Variable '{0}' has no components",
                                                varName));
                                    }
                                    LocalBuilder lbtarget = ilgen.DeclareLocal(varType);
                                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    ilgen.Emit(OpCodes.Stloc, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloc, lbvar);
                                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloc, lbtarget);
                                    ilgen.Emit(OpCodes.Mul);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    if (retLeft != typeof(Vector3) || retRight != typeof(double))
                                    {
                                        ProcessImplicitCasts(ilgen, retLeft, retRight, lineNumber);
                                    }
                                    if (retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                    {
                                        ilgen.Emit(OpCodes.Mul);
                                    }
                                    else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                    {
                                        MethodInfo mi = retLeft.GetMethod("op_Multiply", new Type[] { retLeft, retRight });
                                        if (null == mi)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '*=' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        if (retLeft != mi.ReturnType)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '*=' cannot be processed on {0} and {1}", MapType(retLeft), MapType(retRight)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '*=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, lineNumber);
                                }
                                return retLeft;

                            case "/=":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                {
                                    if (functionTree.SubTree[0].SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        throw new CompilerException(lineNumber, "Component of left-hand value cannot be assigned");
                                    }
                                    ProcessImplicitCasts(ilgen, typeof(double), retRight, lineNumber);
                                    ilgen.BeginScope();
                                    LocalBuilder lbvar = ilgen.DeclareLocal(typeof(double));
                                    ilgen.Emit(OpCodes.Stloc, lbvar);
                                    ilgen.Emit(OpCodes.Dup, lbvar);

                                    string varName = functionTree.SubTree[0].SubTree[0].Entry;
                                    object v = localVars[varName];
                                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, v);
                                    string fieldName;
                                    if (typeof(Vector3) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("vector does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else if (typeof(Quaternion) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("rotation does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber,
                                            string.Format("Variable '{0}' has no components",
                                                varName));
                                    }
                                    LocalBuilder lbtarget = ilgen.DeclareLocal(varType);
                                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    ilgen.Emit(OpCodes.Stloc, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloc, lbvar);
                                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloc, lbtarget);
                                    ilgen.Emit(OpCodes.Div);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    if (retLeft != typeof(Vector3) || retRight != typeof(double))
                                    {
                                        ProcessImplicitCasts(ilgen, retLeft, retRight, lineNumber);
                                    }
                                    if (retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                    {
                                        ilgen.Emit(OpCodes.Div);
                                    }
                                    else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                    {
                                        MethodInfo mi = retLeft.GetMethod("op_Division", new Type[] { retLeft, retRight });
                                        if (null == mi)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '/=' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        if (retLeft != mi.ReturnType)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '/=' cannot be processed on {0} and {1}", MapType(retLeft), MapType(retRight)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '/=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, lineNumber);
                                }
                                return retLeft;

                            case "%=":
                                if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                {
                                    if (functionTree.SubTree[0].SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        throw new CompilerException(lineNumber, "Component of left-hand value cannot be assigned");
                                    }
                                    ProcessImplicitCasts(ilgen, typeof(double), retRight, lineNumber);
                                    ilgen.BeginScope();
                                    LocalBuilder lbvar = ilgen.DeclareLocal(typeof(double));
                                    ilgen.Emit(OpCodes.Stloc, lbvar);
                                    ilgen.Emit(OpCodes.Dup, lbvar);

                                    string varName = functionTree.SubTree[0].SubTree[0].Entry;
                                    object v = localVars[varName];
                                    Type varType = GetVarType(scriptTypeBuilder, stateTypeBuilder, v);
                                    string fieldName;
                                    if (typeof(Vector3) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("vector does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else if (typeof(Quaternion) == varType)
                                    {
                                        switch (functionTree.SubTree[0].SubTree[1].Entry)
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
                                                throw new CompilerException(lineNumber,
                                                    string.Format("rotation does not have component '{0}'",
                                                        functionTree.SubTree[0].SubTree[1].Entry));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber,
                                            string.Format("Variable '{0}' has no components",
                                                varName));
                                    }
                                    LocalBuilder lbtarget = ilgen.DeclareLocal(varType);
                                    GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                                    ilgen.Emit(OpCodes.Stloc, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldloc, lbvar);
                                    ilgen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloca, lbtarget);
                                    ilgen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                                    ilgen.Emit(OpCodes.Ldloc, lbtarget);
                                    ilgen.Emit(OpCodes.Rem);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
                                    ilgen.EndScope();
                                }
                                else
                                {
                                    ProcessImplicitCasts(ilgen, retLeft, retRight, lineNumber);
                                    if (retLeft == typeof(int) || retLeft == typeof(double) || retLeft == typeof(string))
                                    {
                                        ilgen.Emit(OpCodes.Rem);
                                    }
                                    else if (retLeft == typeof(Vector3) || retLeft == typeof(Quaternion) || retLeft == typeof(LSLKey) || retLeft == typeof(AnArray))
                                    {
                                        MethodInfo mi = retLeft.GetMethod("op_Division", new Type[] { retLeft, retRight });
                                        if (null == mi)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("internal error. operator '%=' for {0} and {1} missing.", MapType(retLeft), MapType(retRight)));
                                        }
                                        ilgen.Emit(OpCodes.Call, mi);
                                        if (retLeft != mi.ReturnType)
                                        {
                                            throw new CompilerException(lineNumber, string.Format("operator '%=' cannot be processed on {0} and {1}", MapType(retLeft), MapType(retRight)));
                                        }
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format("operator '%=' not supported for {0} and {1}", MapType(retLeft), MapType(retRight)));
                                    }
                                    ilgen.Emit(OpCodes.Dup);
                                    SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, varInfo, lineNumber);
                                }
                                return retLeft;

                            default:
                                throw new CompilerException(lineNumber, string.Format("binary operator '{0}' not supported", functionTree.Entry));
                        }
                    }
                #endregion

                #region Left unary operators
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

                            case "-":
                                ret = ProcessExpressionPart(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    functionTree.SubTree[0],
                                    lineNumber,
                                    localVars);
                                if (ret == typeof(int) || ret == typeof(double))
                                {
                                    ilgen.Emit(OpCodes.Neg);
                                }
                                else if(ret == typeof(Vector3))
                                {
                                    ilgen.Emit(OpCodes.Call, typeof(Vector3).GetMethod("op_UnaryNegation"));
                                }
                                else if (ret == typeof(Quaternion))
                                {
                                    ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("op_UnaryNegation"));
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format("operator '-' not supported for {0}", MapType(ret)));
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
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
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
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
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
                                throw new CompilerException(lineNumber, string.Format("left unary operator '{0}' not supported", functionTree.Entry));
                        }
                    }
                #endregion

                #region Right unary operators
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
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
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
                                        SetVarFromStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v, lineNumber);
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
                                throw new CompilerException(lineNumber, string.Format("right unary operator '{0}' not supported", functionTree.Entry));
                        }
                    }
                #endregion

                case Tree.EntryType.ReservedWord:
                    throw new CompilerException(lineNumber, string.Format("'{0}' is a reserved word", functionTree.Entry));

                #region Constants
                case Tree.EntryType.StringValue:
                    /* string value */
                    {
                        Tree.ConstantValueString val = (Tree.ConstantValueString)functionTree.Value;
                        ilgen.Emit(OpCodes.Ldstr, val.Value);
                        return typeof(string);
                    }

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
                #endregion

                case Tree.EntryType.Variable:
                    /* variable */
                    try
                    {
                        object v = localVars[functionTree.Entry];
                        return GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    }
                    catch (Exception e)
                    {
                        throw new CompilerException(lineNumber, string.Format("Variable '{0}' not defined", functionTree.Entry));
                    }

                case Tree.EntryType.Level:
                    switch(functionTree.Entry)
                    {
                        case "[":
                            /* we got a list */
                            ilgen.BeginScope();
                            {
                                LocalBuilder lb = ilgen.DeclareLocal(typeof(AnArray));
                                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                                ilgen.Emit(OpCodes.Stloc, lb);

                                for(int i = 0; i < functionTree.SubTree.Count; ++i)
                                {
                                    Tree st = functionTree.SubTree[i++];
                                    if(i + 1 < functionTree.SubTree.Count)
                                    {
                                        if(functionTree.SubTree[i].Entry != ",")
                                        {
                                            throw new CompilerException(lineNumber, "Wrong list declaration");
                                        }
                                    }
                                    ilgen.Emit(OpCodes.Ldloc, lb);
                                    Type ret = ProcessExpressionPart(compileState, scriptTypeBuilder, stateTypeBuilder, ilgen, st, lineNumber, localVars);
                                    if(ret == typeof(void))
                                    {
                                        throw new CompilerException(lineNumber, "Function has no return value");
                                    }
                                    else if(ret == typeof(int) || ret == typeof(double) || ret == typeof(string))
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { ret }));
                                    }
                                    else if(ret == typeof(LSLKey) || ret == typeof(Vector3) || ret == typeof(Quaternion))
                                    {
                                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                                    }
                                    else if(ret == typeof(AnArray))
                                    {
                                        throw new CompilerException(lineNumber, "Lists cannot be put into lists");
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, "Internal error");
                                    }
                                }
                                ilgen.Emit(OpCodes.Ldloc, lb);
                            }
                            ilgen.EndScope();
                            return typeof(AnArray);

                        case "(":
                            return ProcessExpressionPart(compileState, scriptTypeBuilder, stateTypeBuilder, ilgen, functionTree.SubTree[0], lineNumber, localVars);

                        default:
                            throw new CompilerException(lineNumber, string.Format("unexpected level entry '{0}'", functionTree.Entry));
                    }

                case Tree.EntryType.Unknown:
                    /* variable? */
                    try
                    {
                        object v = localVars[functionTree.Entry];
                        return GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    }
                    catch
                    {
                        throw new CompilerException(lineNumber, string.Format("unknown variable '{0}'", functionTree.Entry));
                    }

                #region Typecasts
                case Tree.EntryType.Typecast:
                    {
                        Type toType;
                        switch(functionTree.Entry)
                        {
                            case "integer":
                                toType = typeof(int);
                                break;

                            case "float":
                                toType = typeof(double);
                                break;

                            case "string":
                                toType = typeof(string);
                                break;

                            case "key":
                                toType = typeof(LSLKey);
                                break;

                            case "list":
                                toType = typeof(AnArray);
                                break;

                            case "vector":
                                toType = typeof(Vector3);
                                break;

                            case "rotation":
                            case "quaternion":
                                toType = typeof(Quaternion);
                                break;

                            default:
                                throw new CompilerException(lineNumber, string.Format("{0} is not a type", functionTree.Entry));
                        }

                        Type fromType = ProcessExpressionPart(
                            compileState,
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            ilgen,
                            functionTree.SubTree[0],
                            lineNumber,
                            localVars);
                        ProcessCasts(ilgen, toType, fromType, lineNumber);
                        return toType;
                    }
                #endregion

                default:
                    throw new CompilerException(lineNumber, string.Format("unknown '{0}'", functionTree.Entry));
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
            if(functionLine.Line[startAt] == "@")
            {
                throw compilerException(functionLine, "Invalid label declaration");
            }
            #region Jump to label
            else if(functionLine.Line[startAt] == "jump")
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
            else if(functionLine.Line[startAt] == "return")
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
