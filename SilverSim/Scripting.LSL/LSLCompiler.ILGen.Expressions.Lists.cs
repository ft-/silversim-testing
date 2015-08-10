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
        class ListExpression : IExpressionStackElement
        {
            LocalBuilder m_NewList;
            List<Tree> m_ListElements = new List<Tree>();
            int m_LineNumber;

            public ListExpression(
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
                ilgen.BeginScope();
                m_NewList = ilgen.DeclareLocal(typeof(AnArray));
                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                ilgen.Emit(OpCodes.Stloc, m_NewList);
                for (int i = 0; i < functionTree.SubTree.Count; ++i)
                {
                    Tree st = functionTree.SubTree[i++];
                    if (i + 1 < functionTree.SubTree.Count)
                    {
                        if (functionTree.SubTree[i].Entry != ",")
                        {
                            throw new CompilerException(lineNumber, "Wrong list declaration");
                        }
                    }
                    m_ListElements.Add(st);
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
                if(null != innerExpressionReturn)
                {
                    if (innerExpressionReturn == typeof(void))
                    {
                        throw new CompilerException(m_LineNumber, "Function has no return value");
                    }
                    else if (innerExpressionReturn == typeof(int) || innerExpressionReturn == typeof(double) || innerExpressionReturn == typeof(string))
                    {
                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { innerExpressionReturn }));
                    }
                    else if (innerExpressionReturn == typeof(LSLKey) || innerExpressionReturn == typeof(Vector3) || innerExpressionReturn == typeof(Quaternion))
                    {
                        ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    }
                    else if (innerExpressionReturn == typeof(AnArray))
                    {
                        throw new CompilerException(m_LineNumber, "Lists cannot be put into lists");
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, "Internal error");
                    }
                    m_ListElements.RemoveAt(0);
                }

                ilgen.Emit(OpCodes.Ldloc, m_NewList);
                if (m_ListElements.Count == 0)
                {
                    ilgen.EndScope();
                    throw new ReturnTypeException(typeof(AnArray), m_LineNumber);
                }
                else
                {
                    return m_ListElements[0];
                }
            }
        }
    }
}
