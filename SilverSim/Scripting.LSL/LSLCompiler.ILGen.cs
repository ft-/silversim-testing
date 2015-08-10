// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.LSL.Expression;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

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
    }
}
