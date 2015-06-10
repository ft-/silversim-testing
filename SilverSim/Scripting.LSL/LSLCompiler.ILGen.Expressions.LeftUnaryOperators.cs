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

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.LSL.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        class LeftUnaryOperators : IExpressionStackElement
        {
            Tree m_ExpressionTree;
            string m_Operator;
            int m_LineNumber;

            public LeftUnaryOperators(
                LSLCompiler lslCompiler,
                CompileState compileState,
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_Operator = functionTree.Entry;
                m_ExpressionTree = functionTree.SubTree[0];
                m_LineNumber = lineNumber;
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
                if (null != innerExpressionReturn)
                {
                    switch (m_Operator)
                    {
                        case "!":
                            if (innerExpressionReturn == typeof(int))
                            {
                                ilgen.Emit(OpCodes.Ldc_I4_0);
                                ilgen.Emit(OpCodes.Ceq);
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '!' not supported for {0}", MapType(innerExpressionReturn)));
                            }
                            throw new ReturnTypeException(innerExpressionReturn, m_LineNumber);

                        case "-":
                            if (innerExpressionReturn == typeof(int) || innerExpressionReturn == typeof(double))
                            {
                                ilgen.Emit(OpCodes.Neg);
                            }
                            else if (innerExpressionReturn == typeof(Vector3))
                            {
                                ilgen.Emit(OpCodes.Call, typeof(Vector3).GetMethod("op_UnaryNegation"));
                            }
                            else if (innerExpressionReturn == typeof(Quaternion))
                            {
                                ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("op_UnaryNegation"));
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '-' not supported for {0}", MapType(innerExpressionReturn)));
                            }
                            throw new ReturnTypeException(innerExpressionReturn, m_LineNumber);

                        case "~":
                            if (innerExpressionReturn == typeof(int))
                            {
                                ilgen.Emit(OpCodes.Neg);
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '~' not supported for {0}", MapType(innerExpressionReturn)));
                            }
                            throw new ReturnTypeException(innerExpressionReturn, m_LineNumber);

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("left unary operator '{0}' not supported", m_Operator));
                    }
                }
                else
                {
                    return m_ExpressionTree;
                }
            }
        }
    }
}
