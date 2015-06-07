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
        #region LSL Integer Overflow
        /* special functions for converts
         * 
         * Integer Overflow
         * The compiler treats integers outside the range -2147483648 to 2147483647 somewhat strangely. No compile time warning or error is generated. (If the following explanation, doesn't make sense to you don't worry -- just know to avoid using numbers outside the valid range in your script.)

         * - For an integer outside the range -2147483648 to 2147483647, the absolute value of the number is reduced to fall in the range 0 to 4294967295 (0xFFFFFFFF).
         * - This number is then parsed as an unsigned 32 bit integer and cast to the corresponding signed integer.
         * - If the value in the script had a negative sign, the sign of the internal representation is switched.
         * - The net effect is that very large positive numbers get mapped to -1 and very large negative numbers get mapped to 1.
         */

        public static int ConvToInt(double v)
        {
            try
            {
                return (int)v;
            }
            catch
            {
                if(v > 0)
                {
                    try
                    {
                        return (int)((uint)v);
                    }
                    catch
                    {
                        return -1;
                    }
                }
                else
                {
                    try
                    {
                        return (int)-((uint)v);
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
        }

        public static int ConvToInt(string v)
        {
            if(v.ToLower().StartsWith("0x"))
            {
                try
                {
                    return (int)uint.Parse(v.Substring(2), NumberStyles.HexNumber);
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                try
                {
                    return int.Parse(v);
                }
                catch
                {
                    try
                    {
                        if(v.StartsWith("-"))
                        {
                            try
                            {
                                return -((int)uint.Parse(v.Substring(1)));
                            }
                            catch
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            try
                            {
                                return (int)uint.Parse(v.Substring(1));
                            }
                            catch
                            {
                                return -1;
                            }
                        }
                    }
                    catch
                    {
                        if(v.StartsWith("-"))
                        {
                            return 1;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
        }

        public static int LSL_IntegerMultiply(int a, int b)
        {
#warning implement overflow behaviour for integer multiply
            return a * b;
        }

        public static int LSL_IntegerDivision(int a, int b)
        {
            if (a == -2147483648 && b == -1)
            {
                return -2147483648;
            }
            else
            {
                return a / b;
            }
        }

        public static int LSL_IntegerModulus(int a, int b)
        {
            if (a == -2147483648 && b == -1)
            {
                return 0;
            }
            else
            {
                return a / b;
            }
        }
        #endregion

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

        class ILLabelInfo
        {
            public Label Label;
            public bool IsDefined = false;
            public List<int> UsedInLines = new List<int>();

            public ILLabelInfo(Label label, bool isDefined)
            {
                Label = label;
                IsDefined = isDefined;
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

        bool IsValidType(Type t)
        {
            if (t == typeof(string)) return true;
            if (t == typeof(int)) return true;
            if (t == typeof(double)) return true;
            if (t == typeof(LSLKey)) return true;
            if (t == typeof(Quaternion)) return true;
            if (t == typeof(Vector3)) return true;
            if (t == typeof(AnArray)) return true;
            if (t == typeof(void)) return true;
            return false;
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
            if (t == typeof(void)) return "void";
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
                    try
                    {
                        object v = localVars[functionTree.Entry];
                        return GetVarToStack(scriptTypeBuilder, stateTypeBuilder, ilgen, v);
                    }
                    catch(Exception e)
                    {
                        throw new CompilerException(lineNumber, string.Format("Variable '{0}' not defined", functionTree.Entry));
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
                    
                default:
                    throw new CompilerException(lineNumber, string.Format("unknown '{0}'", functionTree.Entry));
            }
        }

        void ProcessImplicitCasts(ILGenerator ilgen, Type toType, Type fromType, int lineNumber)
        {
            if (fromType == toType)
            {

            }
            else if(toType == typeof(void))
            {
            }
            else if (fromType == typeof(string) && toType == typeof(LSLKey))
            {

            }
            else if (fromType == typeof(LSLKey) && toType == typeof(string))
            {

            }
            else if (fromType == typeof(int) && toType == typeof(double))
            {

            }
            else if(toType == typeof(AnArray))
            {

            }
            else if (toType == typeof(bool))
            {

            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("Unsupported implicit typecast from {0} to {1}", MapType(fromType), MapType(toType)));
            }
            ProcessCasts(ilgen, toType, fromType, lineNumber);
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
            else if(toType == typeof(LSLKey))
            {
                if(fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { fromType }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("function does not return anything"));
                }
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
                /* yes, we need special handling for conversion of string to integer or float to integer. (see section about Integer Overflow) */
                if(fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
                }
                else if(fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
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
                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
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
                if ((((FieldBuilder)v).Attributes & FieldAttributes.Static) != 0)
                {
                    ilgen.Emit(OpCodes.Ldsfld, ((FieldBuilder)v));
                }
                else
                {
                    ilgen.Emit(OpCodes.Ldfld, ((FieldBuilder)v));
                }
                retType = ((FieldBuilder)v).FieldType;
            }
            else if (v is FieldInfo)
            {
                if ((((FieldInfo)v).Attributes & FieldAttributes.Static) != 0)
                {
                    ilgen.Emit(OpCodes.Ldsfld, ((FieldInfo)v));
                }
                else
                {
                    ilgen.Emit(OpCodes.Ldfld, ((FieldInfo)v));
                }
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
            object v,
            int lineNumber)
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
                if ((((FieldInfo)v).Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, "Setting constants is not allowed");
                }
                ilgen.Emit(OpCodes.Stfld, ((FieldBuilder)v));
            }
            else if (v is FieldInfo)
            {
                if ((((FieldInfo)v).Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, "Setting constants is not allowed");
                }
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

        void ProcessBlock(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            Type returnType,
            ILGenerator ilgen,
            List<LineInfo> functionBody,
            Dictionary<string, object> localVars,
            Dictionary<string, ILLabelInfo> labels,
            ref int lineIndex)
        {
            Dictionary<string, ILLabelInfo> outerLabels = labels;
            List<string> markedLabels = new List<string>();
            /* we need a copy here */
            localVars = new Dictionary<string, object>(localVars);
            if (null != labels)
            {
                labels = new Dictionary<string, ILLabelInfo>(labels);
            }
            else
            {
                labels = new Dictionary<string, ILLabelInfo>();
            }

            for (; lineIndex < functionBody.Count; ++lineIndex)
            {
                LineInfo functionLine = functionBody[lineIndex];
                LocalBuilder lb;
                switch (functionLine.Line[0])
                {
                    #region Label definition
                    case "@":
                        if (functionLine.Line.Count != 3 || functionLine.Line[2] != ";")
                        {
                            throw compilerException(functionLine, "not a valid label definition");
                        }
                        else
                        {
                            string labelName = functionLine.Line[1];
                            if (!labels.ContainsKey(labelName))
                            {
                                Label label = ilgen.DefineLabel();
                                labels[functionLine.Line[1]] = new ILLabelInfo(label, true);
                            }
                            else if (labels[labelName].IsDefined)
                            {
                                throw compilerException(functionLine, "label already defined");
                            }
                            else
                            {
                                labels[labelName].IsDefined = true;
                            }
                            ilgen.MarkLabel(labels[labelName].Label);
                        }
                        break;
                    #endregion

                    #region Variable declarations
                    /* type named things are variable declaration */
                    case "integer":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                            ilgen.Emit(OpCodes.Ldsfld, typeof(Vector3).GetField("Zero"));
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "list":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                    case "quaternion":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw compilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
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
                            ilgen.Emit(OpCodes.Ldsfld, typeof(Quaternion).GetField("Identity"));
                        }
                        ilgen.Emit(OpCodes.Stloc, lb);
                        break;
                    #endregion

                    #region Control Flow (Loops)
                    case "for":
                        {   /* for(a;b;c) */
                            int semicolon1, semicolon2;
                            int endoffor;
                            int countparens = 0;
                            for (endoffor = 0; endoffor <= functionLine.Line.Count; ++endoffor)
                            {
                                if (functionLine.Line[endoffor] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endoffor] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endoffor != functionLine.Line.Count - 1 && endoffor != functionLine.Line.Count - 2)
                            {
                                throw compilerException(functionLine, "Invalid 'for' encountered");
                            }

                            semicolon1 = functionLine.Line.IndexOf(";");
                            semicolon2 = functionLine.Line.IndexOf(";", semicolon1 + 1);
                            if (2 != semicolon1)
                            {
                                ProcessStatement(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    returnType,
                                    ilgen,
                                    2,
                                    semicolon1 - 1,
                                    functionLine,
                                    localVars,
                                    labels);
                            }
                            Label endlabel = ilgen.DefineLabel();
                            Label looplabel = ilgen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "For End Label"));
                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.For,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                looplabel,
                                endlabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            ilgen.MarkLabel(looplabel);

                            if (semicolon1 + 1 != semicolon2)
                            {
                                ProcessExpression(
                                    compileState,
                                    scriptTypeBuilder,
                                    stateTypeBuilder,
                                    ilgen,
                                    typeof(bool),
                                    semicolon1 + 1,
                                    semicolon2 - 1,
                                    functionLine,
                                    localVars);
                                ilgen.Emit(OpCodes.Brfalse, endlabel);
                            }

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
                                ilgen.EndScope();
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
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofwhile] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if ((endofwhile != functionLine.Line.Count - 1 && endofwhile != functionLine.Line.Count - 2) || endofwhile == 2)
                            {
                                throw compilerException(functionLine, "Invalid 'while' encountered");
                            }

                            Label looplabel = ilgen.DefineLabel();
                            Label endlabel = ilgen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "While End Label"));
                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.While,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                looplabel,
                                endlabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            ilgen.Emit(OpCodes.Br, endlabel);

                            ilgen.MarkLabel(looplabel);
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
                            ilgen.Emit(OpCodes.Brfalse, endlabel);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
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
                        }
                        break;

                    case "do":
                        {
                            Label looplabel = ilgen.DefineLabel();
                            Label endlabel = ilgen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "Do While End Label"));
                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.DoWhile,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                looplabel,
                                endlabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            ilgen.MarkLabel(looplabel);
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
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
                        }
                        break;
                    #endregion

                    #region Control Flow (Conditions)
                    case "if":
                        {
                            Label eoiflabel = ilgen.DefineLabel();
                            Label endlabel = ilgen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(eoiflabel, new KeyValuePair<int, string>(functionLine.LineNumber, "IfElse End Of All Label"));
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "IfElse End Label"));

                            int endofif;
                            int countparens = 0;
                            for (endofif = 0; endofif <= functionLine.Line.Count; ++endofif)
                            {
                                if (functionLine.Line[endofif] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofif] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.If,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                null,
                                endlabel,
                                eoiflabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            if ((endofif != functionLine.Line.Count - 1 && endofif != functionLine.Line.Count - 2) || endofif == 2)
                            {
                                throw compilerException(functionLine, "Invalid 'if' encountered");
                            }

                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                typeof(bool),
                                2,
                                endofif - 1,
                                functionLine,
                                localVars);
                            ilgen.Emit(OpCodes.Brfalse, endlabel);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
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
                        }
                        break;

                    case "else":
                        if (null == compileState.LastBlock)
                        {
                            throw compilerException(functionLine, "No matching 'if' found for 'else'");
                        }
                        else if (functionLine.Line.Count > 1 && functionLine.Line[1] == "if")
                        { /* else if */
                            Label eoiflabel = compileState.LastBlock.EndOfIfFlowLabel.Value;
                            Label endlabel = ilgen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "ElseIf End Label"));

                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.ElseIf,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                null,
                                endlabel,
                                eoiflabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            int endofif;
                            int countparens = 0;
                            for (endofif = 0; endofif <= functionLine.Line.Count; ++endofif)
                            {
                                if (functionLine.Line[endofif] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofif] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if ((endofif != functionLine.Line.Count - 1 && endofif != functionLine.Line.Count - 2) || endofif == 2)
                            {
                                throw compilerException(functionLine, "Invalid 'else if' encountered");
                            }

                            ProcessExpression(
                                compileState,
                                scriptTypeBuilder,
                                stateTypeBuilder,
                                ilgen,
                                typeof(bool),
                                3,
                                endofif - 1,
                                functionLine,
                                localVars);
                            ilgen.Emit(OpCodes.Brfalse, endlabel);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
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
                        }
                        else
                        {
                            /* else */
                            Label eoiflabel = compileState.LastBlock.EndOfIfFlowLabel.Value;
                            Label endlabel = ilgen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "Else End Label"));

                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.Else,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                null,
                                endlabel,
                                eoiflabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
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
                        }
                        break;
                    #endregion

                    #region New unconditional block
                    case "{": /* new unconditional block */
                        compileState.PopControlFlowImplicits(ilgen, functionLine.LineNumber);
                        {
                            ControlFlowElement elem = new ControlFlowElement(ControlFlowType.UnconditionalBlock, true);
                            compileState.PushControlFlow(elem);
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
                        break;
                    #endregion

                    #region End of unconditional/conditional block
                    case "}": /* end unconditional/conditional block */
                        {
                            Dictionary<int, string> messages = new Dictionary<int, string>();
                            foreach (KeyValuePair<string, ILLabelInfo> kvp in labels)
                            {
                                if (!kvp.Value.IsDefined)
                                {
                                    foreach (int line in kvp.Value.UsedInLines)
                                    {
                                        messages[line] = string.Format("Label '{0}' not defined", kvp.Key);
                                    }
                                }
                            }
                            if (messages.Count != 0)
                            {
                                throw new CompilerException(messages);
                            }
                            ControlFlowElement elem = compileState.PopControlFlowExplicit(ilgen, functionLine.LineNumber);
                            if (elem.IsExplicitBlock && elem.Type != ControlFlowType.Entry)
                            {
                                ilgen.EndScope();
                            }
                        }
                        /* no increment here, is done outside */
                        return;
                    #endregion

                    default:
                        ProcessStatement(
                            compileState,
                            scriptTypeBuilder,
                            stateTypeBuilder,
                            returnType,
                            ilgen,
                            0,
                            functionLine.Line.Count - 2,
                            functionLine,
                            localVars,
                            labels);
                        compileState.PopControlFlowImplicit(ilgen, functionLine.LineNumber);
                        break;
                }
            }

            throw compilerException(functionBody[functionBody.Count - 1], "Missing '}'");
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
            compileState.InitControlFlow();
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
                case "quaternion":
                    returnType = typeof(Quaternion);
                    break;

                case "void":
                    returnType = typeof(void);
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
                    case "quaternion":
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
                ilgen.Emit(OpCodes.Ldsfld, typeof(Vector3).GetField("Zero"));
            }
            else if (returnType == typeof(Quaternion))
            {
                ilgen.Emit(OpCodes.Ldsfld, typeof(Quaternion).GetField("Identity"));
            }
            else if (returnType == typeof(LSLKey))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[0]));
            }
            ilgen.Emit(OpCodes.Ret);
            compileState.FinishControlFlowChecks();
        }

        Dictionary<string, object> AddConstants(CompileState compileState, TypeBuilder typeBuilder, ILGenerator ilgen)
        {
            Dictionary<string, object> localVars = new Dictionary<string, object>();
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
                                localVars[f.Name] = f;
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
                        script_ilgen.Emit(OpCodes.Stfld, fb);
                        varIsInited.Add(varName);
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
                    script_ilgen.Emit(OpCodes.Stfld, fb);
                    varIsInited.Add(varName);
                }
                else if(fb.FieldType == typeof(double))
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
                    if(functionDeclaration[functionStart] == ",")
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
