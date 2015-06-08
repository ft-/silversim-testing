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
            int blockLevel = 1;
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
                                ++blockLevel;
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
                                ++blockLevel;
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
                                ++blockLevel;
                            }
                        }
                        break;
                    #endregion

                    #region Control Flow (Conditions)
                    case "if":
                        compileState.PopControlFlowImplicit(ilgen, functionLine.LineNumber);
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
                                ++blockLevel;
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
                                ++blockLevel;
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
                                ++blockLevel;
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
                            ++blockLevel;
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
                            switch(--blockLevel)
                            {
                                case 0:
                                    return;
                                    
                                case -1:
                                    throw compilerException(functionLine, "Unmatched '}' found");

                                default:
                                    break;
                            }
                        }
                        break;
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

            if (blockLevel != 0)
            {
                throw compilerException(functionBody[functionBody.Count - 1], "Missing '}'");
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
                if (functionDeclaration[functionStart] == ",")
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
            if (returnType == typeof(int))
            {
                ilgen.Emit(OpCodes.Ldc_I4_0);
            }
            else if (returnType == typeof(double))
            {
                ilgen.Emit(OpCodes.Ldc_R8, 0f);
            }
            else if (returnType == typeof(string))
            {
                ilgen.Emit(OpCodes.Ldstr);
            }
            else if (returnType == typeof(AnArray))
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
    }
}
