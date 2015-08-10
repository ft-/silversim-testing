// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scripting.LSL.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        class RotationExpression : IExpressionStackElement
        {
            List<Tree> m_ListElements = new List<Tree>();
            int m_LineNumber;

            public RotationExpression(
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
                for (int i = 0; i < 4; ++i)
                {
                    m_ListElements.Add(functionTree.SubTree[i]);
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
                if (null != innerExpressionReturn)
                {
                    ProcessImplicitCasts(ilgen, typeof(double), innerExpressionReturn, m_LineNumber);
                    m_ListElements.RemoveAt(0);
                }

                if (m_ListElements.Count == 0)
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                    throw new ReturnTypeException(typeof(Quaternion), m_LineNumber);
                }
                else
                {
                    return m_ListElements[0];
                }
            }
        }
    }
}
