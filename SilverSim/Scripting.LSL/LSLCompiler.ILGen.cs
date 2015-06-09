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
    }
}
