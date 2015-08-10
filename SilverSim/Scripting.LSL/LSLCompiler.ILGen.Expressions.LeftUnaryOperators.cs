// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
