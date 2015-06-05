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

using SilverSim.Scripting.Common.Expression;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        void solveDeclarations(Tree tree)
        {
            foreach (Tree st in tree.SubTree)
            {
                solveDeclarations(st);

                if (st.Type == Tree.EntryType.Declaration)
                {
                    if (st.SubTree.Count == 3)
                    {
                        st.Type = Tree.EntryType.Vector;
                    }
                    else if (st.SubTree.Count == 4)
                    {
                        st.Type = Tree.EntryType.Rotation;
                    }
                    else
                    {
                        throw new Resolver.ResolverException("argument list for <> has neither 3 nor 4 arguments");
                    }
                }
            }
        }

        class ConstantValueVector : Tree.ConstantValue
        {
            public Vector3 Value;

            public ConstantValueVector(Vector3 v)
            {
                Value = v;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override Tree.ValueBase Negate()
            {
                return new ConstantValueVector(-Value);
            }
        }

        class ConstantValueRotation : Tree.ConstantValue
        {
            public Quaternion Value;

            public ConstantValueRotation(Quaternion v)
            {
                Value = v;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override Tree.ValueBase Negate()
            {
                return new ConstantValueRotation(-Value);
            }
        }

        void solveTypecasts(Tree tree)
        {
            int pos;
            for (pos = 0; pos < tree.SubTree.Count - 1; ++pos)
            {
                Tree st = tree.SubTree[pos];
                if (st.SubTree.Count == 1 && st.SubTree[0].Type == Tree.EntryType.ReservedWord && st.Type == Tree.EntryType.Level)
                {
                    st.Entry = st.SubTree[0].Entry;
                    st.Type = Tree.EntryType.Typecast;
                    st.SubTree.Add(tree.SubTree[pos + 1]);
                    tree.SubTree.RemoveAt(pos + 1);
                }
                else
                {
                    ++pos;
                }
                solveTypecasts(st);
            }
        }

        void solveVariables(CompileState cs, Tree tree)
        {
            foreach (Tree st in tree.SubTree)
            {
                solveVariables(cs, tree);

                if (st.Type == Tree.EntryType.Unknown)
                {
                    if (cs.m_VariableDeclarations.ContainsKey(st.Entry))
                    {
                        st.Type = Tree.EntryType.Variable;
                    }
                    foreach (List<string> vars in cs.m_LocalVariables)
                    {
                        if (vars.Contains(st.Entry))
                        {
                            st.Type = Tree.EntryType.Variable;
                        }
                    }
                }
            }
        }

        void solveConstantOperations(Tree tree)
        {
            foreach (Tree st in tree.SubTree)
            {
                solveConstantOperations(st);

                if (st.Entry != "<")
                {

                }
                else if (st.Type == Tree.EntryType.Vector)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null)
                    {
                        double[] v = new double[3];
                        for (int idx = 0; idx < 3; ++idx)
                        {
                            if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueFloat)
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueInt)
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new Resolver.ResolverException("constant vector cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueVector(new Vector3(v[0], v[1], v[2]));
                    }
                }
                else if (st.Type == Tree.EntryType.Rotation)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null &&
                        st.SubTree[3].SubTree[0].Value != null)
                    {
                        double[] v = new double[4];
                        for (int idx = 0; idx < 4; ++idx)
                        {
                            if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueFloat)
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueInt)
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new Resolver.ResolverException("constant rotation cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueRotation(new Quaternion(v[0], v[1], v[2], v[3]));

                    }
                }

                if (st.Type == Tree.EntryType.Typecast && st.SubTree[0].Value != null)
                {
                    /* solve a typecast */
                    switch (st.Entry)
                    {
                        case "string":
                            st.Value = new Tree.ConstantValueString(st.SubTree[0].Value.ToString());
                            break;

                        case "integer":
                            break;

                        case "float":
                            break;

                        case "vector":
                            break;

                        case "rotation":
                            break;

                        case "key":
                            break;

                        default:
                            throw new Resolver.ResolverException(string.Format("Invalid typecasting with {0}", st.Entry));
                    }
                }

                if (st.Type == Tree.EntryType.OperatorBinary && st.SubTree[0].Value != null && st.SubTree[1].Value != null)
                {
                    switch (st.Entry)
                    {
                        case "+":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueString && st.SubTree[1].Value is Tree.ConstantValueString)
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "-":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value -
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "*":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value.Cross(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value));
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value *
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "/":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value /
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "%":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "^":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ^
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<<":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <<
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">>":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >>
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "!=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "==":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ".":
                            throw new Resolver.ResolverException("'.' should not be used with constants");
                    }
                }
                else if (st.Type == Tree.EntryType.OperatorLeftUnary && st.SubTree[0].Value != null)
                {
                    if (st.Entry == "+")
                    {
                        st.Value = st.SubTree[0].Value;
                    }
                    else if (st.Entry == "-")
                    {
                        st.Value = st.SubTree[0].Value.Negate();
                    }
                    else if (st.Entry == "~")
                    {
                        if (st.Value is Tree.ConstantValueFloat)
                        {
                            throw new Resolver.ResolverException("float cannot be binary-negated");
                        }
                        else if (st.Value is Tree.ConstantValueInt)
                        {
                            st.Value = new Tree.ConstantValueInt(~((Tree.ConstantValueInt)(st.Value)).Value);
                        }
                        else if (st.Value is Tree.ConstantValueString)
                        {
                            throw new Resolver.ResolverException("string cannot be binary negated");
                        }
                    }
                    else if (st.Entry == "!")
                    {
                        if (st.Value is Tree.ConstantValueFloat)
                        {
                            throw new Resolver.ResolverException("float cannot be logically negated");
                        }
                        else if (st.Value is Tree.ConstantValueInt)
                        {
                            st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueInt)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (st.Value is Tree.ConstantValueString)
                        {
                            throw new Resolver.ResolverException("string cannot be logically negated");
                        }
                    }
                }
            }
        }

        void solveTree(CompileState cs, Tree resolvetree)
        {
            m_Resolver.Process(resolvetree);
            solveDeclarations(resolvetree);
            solveTypecasts(resolvetree);
            solveVariables(cs, resolvetree);
            solveConstantOperations(resolvetree);
        }

    }
}
