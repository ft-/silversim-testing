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
        class TypecastExpression : IExpressionStackElement
        {
            Tree m_TypecastTree;
            Type m_TargetType;
            int m_LineNumber;

            public TypecastExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                TypeBuilder scriptTypeBuilder,
                TypeBuilder stateTypeBuilder,
                ILGenerator ilgen,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_LineNumber = lineNumber;
                m_TypecastTree = functionTree.SubTree[0];
                switch (functionTree.Entry)
                {
                    case "(integer)":
                        m_TargetType = typeof(int);
                        break;

                    case "(float)":
                        m_TargetType = typeof(double);
                        break;

                    case "(string)":
                        m_TargetType = typeof(string);
                        break;

                    case "(key)":
                        m_TargetType = typeof(LSLKey);
                        break;

                    case "(list)":
                        m_TargetType = typeof(AnArray);
                        break;

                    case "(vector)":
                        m_TargetType = typeof(Vector3);
                        break;

                    case "(rotation)":
                    case "(quaternion)":
                        m_TargetType = typeof(Quaternion);
                        break;

                    default:
                        throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a valid typecast", functionTree.Entry));
                }
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
                if(null == innerExpressionReturn)
                {
                    return m_TypecastTree;
                }
                else
                {
                    ProcessCasts(ilgen, m_TargetType, innerExpressionReturn, m_LineNumber);
                    throw new ReturnTypeException(m_TargetType, m_LineNumber);
                }
            }
        }
    }
}
