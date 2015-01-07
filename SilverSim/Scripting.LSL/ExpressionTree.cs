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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scripting.Common;
using System.Globalization;

namespace SilverSim.Scripting.LSL
{
    public class ExpressionTree
    {
        public enum EntryType
        {
            Unknown,
            Value,
            OperatorUnknown,
            OperatorLeftUnary,
            OperatorRightUnary,
            OperatorBinary,
            Reserved,
            Invalid,
            First,
            Function,
            Array, /* name for list */
            Vector,
            Rotation,
            Separator
        }

        public List<ExpressionTree> SubTree = new List<ExpressionTree>();
        public string Entry = string.Empty;
        public int Precedence = 0;
        public bool IsFollowedByParenthesis = false;

        public EntryType Type = EntryType.Unknown;

        public abstract class ValueBase
        {
            public ValueBase()
            {

            }
        }

        ValueBase Value;

        public abstract class ConstantValue : ValueBase
        {
            public abstract ValueBase Negate();
            public abstract ValueBase Clone();
        }

        public class ConstantValueInt : ConstantValue
        {
            public Int64 Value;

            public ConstantValueInt(Int64 value)
            {
                Value = value;
            }

            public ConstantValueInt(string str)
            {
                if (str.StartsWith("0x"))
                {
                    Value = Int64.Parse(str, NumberStyles.HexNumber);
                }
                else if (str.StartsWith("0b"))
                {
                }
                else if (str.StartsWith("0"))
                {

                }
                else
                {
                    Value = Int64.Parse(str);
                }
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override ValueBase Negate()
            {
                return new ConstantValueInt(-Value);
            }

            public override ValueBase Clone()
            {
                return new ConstantValueInt(Value);
            }
        }

        public class ConstantValueFloat : ConstantValue
        {
            public double Value;
            public ConstantValueFloat(double value)
            {

            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override ValueBase Negate()
            {
                return new ConstantValueFloat(-Value);
            }

            public override ValueBase Clone()
            {
                return new ConstantValueFloat(Value);
            }
        }

        public class ConstantValueString : ConstantValue
        {
            public string Value;
            public ConstantValueString(string value)
            {
                Value = value;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override ValueBase Negate()
            {
                throw new NotSupportedException();
            }

            public override ValueBase Clone()
            {
                return new ConstantValueString(Value);
            }
        }

        public sealed class OperatorInfo
        {
            public enum OperatorType
            {
                Unknown,
                RightUnary,
                LeftUnary,
                Binary
            }

            public string Name;
            public OperatorType Type;

            public OperatorInfo()
            {
                Type = OperatorType.Unknown;
            }

            public OperatorInfo(string name, OperatorType type)
            {
                Name = name;
                Type = type;
            }
        }

        protected bool m_IsLeftSquaredBrackedBinary = true;

        public void Split(
            List<string> arguments,
            int arg_begin,
            int arg_end,
            List<List<OperatorInfo>> operators,
            List<string> reservedwords)
        {
            ExpressionTree previous = null;
            for (int argpos = arg_begin; argpos < arg_end; ++argpos)
            {
                if (arguments[argpos] == "[")
                {
                    ++argpos;
                    if (argpos == arg_end)
                    {
                        throw new EvaluationException("missing ']'");
                    }
                    int itend;
                    int level = 0;
                    List<int> sepplaces = new List<int>();
                    for (itend = argpos; itend != arg_end; ++itend)
                    {
                        if (arguments[itend] == "(")
                        {
                            ++level;
                        }
                        if (arguments[itend] == ")")
                        {
                            if (--level < 0)
                            {
                                throw new EvaluationException("parentheses mismatch");
                            }
                        }

                        if (arguments[itend] == "[" && level == 0)
                        {
                            throw new EvaluationException("lists cannot be nested");
                        }
                        if (arguments[itend] == "]" && level == 0)
                        {
                            break;
                        }
                        if(arguments[itend] == "," && level == 0)
                        {
                            if (0 == level)
                            {
                                sepplaces.Add(itend);
                            }
                        }
                    }

                    ExpressionTree f_subtree = new ExpressionTree();
                    //f_subtree.Split(arguments, argpos, itend, operators, reservedwords);
                    //SubTree.Add(f_subtree);
                    argpos = itend;
                    previous = f_subtree;
                }
                else if (arguments[argpos] == "(")
                {
                    if (null != previous)
                    {
                        previous.IsFollowedByParenthesis = true;
                    }
                    ++argpos;
                    if (argpos == arg_end)
                    {
                        throw new EvaluationException("missing ')'");
                    }
                    int level = 1;
                    int itend;
                    for (itend = argpos; itend != arg_end; ++itend)
                    {
                        if (arguments[itend] == "(")
                        {
                            ++level;
                        }
                        if (arguments[itend] == ")")
                        {
                            if (--level == 0)
                            {
                                break;
                            }
                        }
                    }

                    if (level != 0)
                    {
                        throw new EvaluationException("parenthesis mismatch");
                    }
                    ExpressionTree f_subtree = new ExpressionTree();
                    f_subtree.Split(arguments, argpos, itend, operators, reservedwords);
                    SubTree.Add(f_subtree);
                    argpos = itend;
                    previous = f_subtree;
                }
                else if (arguments[argpos] == "<")
                {
                    if(argpos == 0)
                    {

                    }
                    else if(LSL_UnsortedOperators.IndexOf(arguments[argpos-1]) < 0 &&
                        arguments[argpos - 1] != "(" && arguments[argpos - 1] != "," && arguments[argpos - 1] != "[" && arguments[argpos -1] != "return")
                    {
                        /* cannot be a vector or rotation constant */
                        ExpressionTree f_subtree = new ExpressionTree();
                        f_subtree.Entry = arguments[argpos];
                        SubTree.Add(f_subtree);
                        previous = f_subtree;
                    }
                    else
                    {
                        ++argpos;
                        if (argpos == arg_end)
                        {
                            throw new EvaluationException("missing '>'");
                        }
                        int level = 0;
                        int itend;
                        List<int> sepplaces = new List<int>();
                        for (itend = argpos; itend != arg_end; ++itend)
                        {
                            if(arguments[itend] == "(")
                            {
                                ++level;
                            }
                            if(arguments[itend] == ")")
                            {
                                if(--level < 0)
                                {
                                    throw new EvaluationException("parentheses mismatch");
                                }
                            }

                            if (arguments[itend] == ">" && level == 0)
                            {
                                if(itend == arg_end - 1)
                                {
                                    break;
                                }
                                else if(LSL_UnsortedOperators.IndexOf(arguments[argpos+1]) < 0 &&
                                    arguments[argpos + 1] != ")" && arguments[argpos + 1] != "," && arguments[argpos + 1] != "]")
                                {
                                    /* cannot be a vector or rotation constant end */
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (arguments[itend] == ",")
                            {
                                if (0 == level)
                                {
                                    sepplaces.Add(itend);
                                }
                            }
                        }

                        if (level != 0)
                        {
                            throw new EvaluationException("angle bracket mismatch");
                        }
                        ExpressionTree f_subtree = new ExpressionTree();
                        //f_subtree.Split(arguments, argpos, itend, operators, reservedwords);
                        //SubTree.Add(f_subtree);
                        if (sepplaces.Count == 2)
                        {
                            Type = EntryType.Vector;
                        }
                        else if(sepplaces.Count == 3)
                        {
                            Type = EntryType.Rotation;
                        }
                        else
                        {
                            throw new EvaluationException("angle brackets do not specify a vector nor a rotation");
                        }
                        argpos = itend;
                        previous = f_subtree;
                    }
                }
                else if (arguments[argpos] == ")")
                {
                    throw new EvaluationException("')' without '('");
                }
                else if (arguments[argpos] == "]")
                {
                    throw new EvaluationException("']' without '['");
                }
                else
                {
                    ExpressionTree f_subtree = new ExpressionTree();
                    f_subtree.Entry = arguments[argpos];
                    SubTree.Add(f_subtree);
                    previous = f_subtree;
                }
            }

            EntryType type = EntryType.First;
            Classify(operators, reservedwords, ref type);
            FinishClassify();

        }

        public void Clear()
        {
            SubTree.Clear();
            Entry = string.Empty;
            Type = EntryType.Unknown;
        }

        public void MoveTo(ExpressionTree tree)
        {
            tree.Clear();
            tree.Type = Type;
            tree.Entry = Entry;
            tree.Precedence = Precedence;
            tree.IsFollowedByParenthesis = IsFollowedByParenthesis;
            tree.Value = Value;

            tree.SubTree = SubTree;
            SubTree = new List<ExpressionTree>();
            Value = null;
            Entry = string.Empty;
            Type = EntryType.Unknown;
        }

        public void Finish(List<List<OperatorInfo>> operators, List<string> reservedwords)
        {
            Reorder(operators, reservedwords);
            CleanupTree();
            CleanupUnknown();
        }

        public delegate ValueBase SolveItemDelegate(ExpressionTree item, ExpressionTree parentitem);

        public void SolveTree(SolveItemDelegate solveitem)
        {
            SolveSubtree(solveitem, null);
        }

        public class EvaluationException : Exception
        {
            public EvaluationException(string msg)
                : base(msg)
            {

            }
        }

        public class ReservedWordIncorrectlyUsedException : Exception
        {
            public ReservedWordIncorrectlyUsedException(string msg)
                : base(msg)
            {

            }
        }

        public class UnaryOperatorHasNoOperandException : Exception
        {
            public UnaryOperatorHasNoOperandException(string msg)
                : base(msg)
            {

            }
        }

        public class ResetTreeIteratorCommand : Exception
        {
            public ResetTreeIteratorCommand()
            {

            }
        }

        public class DeleteItemAndResetTreeIteratorCommand : Exception
        {
            public DeleteItemAndResetTreeIteratorCommand()
            {

            }
        }

        private void Reorder(List<List<OperatorInfo>> operators, List<string> reservedwords)
        {
            int it;
            /* first fixup function */
            for (it = 0; it < SubTree.Count; )
            {
                int itn = it;
                if (EntryType.Value == SubTree[it].Type && SubTree[it].IsFollowedByParenthesis)
                {
                    ++itn;
                    if (itn == SubTree.Count)
                    {
                        throw new EvaluationException("invalid function in reorder() (item \"" + SubTree[it].Entry + "\"");
                    }
                    SubTree[it].SubTree.AddRange(SubTree[itn].SubTree);
                    SubTree[it].Type = EntryType.Function;
                    SubTree.RemoveAt(itn);
                    it = itn;
                }
                else
                {
                    ++it;
                }
            }

            /* first handle all unary expressions */
            for (it = 0; it < SubTree.Count; )
            {
                int itn = it;
                switch (SubTree[it].Type)
                {
                    case EntryType.OperatorLeftUnary:
                        /* check if operator is already resolved */
                        if (SubTree[it].SubTree.Count != 0)
                        {
                            ++it;
                            break;
                        }

                        ++itn;
                        if (itn == SubTree.Count)
                        {
                            throw new EvaluationException("invalid left unary operator in reorder()");
                        }
                        SubTree[it].SubTree.Add(SubTree[itn]);
                        it = itn;
                        SubTree.RemoveAt(itn);
                        break;
                    case EntryType.OperatorRightUnary:
                        /* check if operator is already resolved */
                        if (SubTree[it].SubTree.Count != 0)
                        {
                            ++it;
                            break;
                        }

                        if (itn == SubTree.Count)
                        {
                            throw new EvaluationException("invalid right unary operator in reorder()");
                        }
                        --itn;
                        SubTree[it].SubTree.Add(SubTree[itn]);
                        SubTree.RemoveAt(itn);
                        break;

                    default:
                        ++it;
                        break;
                }
            }
            /* second handle all binary expressions (we have to resolve precedence) */
            for (int precedence = 0; precedence < operators.Count; ++precedence)
            {
                for (it = 0; it < SubTree.Count; )
                {
                    int itn = it;
                    int itp = it;

                    switch (SubTree[it].Type)
                    {
                        case EntryType.OperatorBinary:
                            if (SubTree[it].Precedence != precedence)
                            {
                                ++it;
                                break;
                            }
                            /* check if operator is already resolved */
                            if (SubTree[it].SubTree.Count != 0)
                            {
                                ++it;
                                break;
                            }

                            if (itn == SubTree.Count)
                            {
                                throw new EvaluationException("invalid binary operator in reorder()");
                            }
                            if (itp < 0)
                            {
                                throw new EvaluationException("invalid binary operator in reorder()");
                            }
                            ++itn;
                            --itp;

                            SubTree[it].SubTree.Add(SubTree[itp]);
                            SubTree[it].SubTree.Add(SubTree[itn]);
                            SubTree.RemoveAt(itn);
                            SubTree.RemoveAt(itp);
                            break;

                        default:
                            ++it;
                            break;
                    }
                }
            }

            /* third issue reordering on all values */
            for (it = 0; it < SubTree.Count; ++it)
            {
                SubTree[it].Reorder(operators, reservedwords);
            }

        }

        private void CleanupUnknown()
        {
        /* last transform ET_UNKNOWN[any] to any */
        recheck_unknown:

            for (int argpos = 0; argpos < SubTree.Count; ++argpos)
            {
                SubTree[argpos].CleanupUnknown();
                if (SubTree[argpos].Type == EntryType.Unknown && SubTree[argpos].Entry == "" && SubTree[argpos].SubTree.Count == 1)
                {
                    SubTree[argpos] = SubTree[argpos].SubTree[0];
                    goto recheck_unknown;
                }
            }
        }

        private void CleanupTree()
        {
            for (int it = 0; it < SubTree.Count; ++it)
            {
                if (SubTree[it].Entry == "[")
                {
                    SubTree[it].Type = EntryType.Array;
                }

                switch (SubTree[it].Type)
                {
                    case EntryType.Function:
                    case EntryType.Array:
                        SubTree[it].CleanupTree();
                        break;

                    case EntryType.Value:
                        /* collapse sub elements into this one */
                        do
                        {
                            if (SubTree[it].SubTree.Count > 1)
                            {
                                throw new EvaluationException("value contains more than value expression (CleanupTree())");
                            }
                            if (SubTree[it].SubTree.Count == 1)
                            {
                                SubTree[it].SubTree = SubTree[it].SubTree[0].SubTree;
                            }
                        }
                        while (SubTree[it].Type == EntryType.Value);
                        break;
                }
            }

            for (int it = 0; it < SubTree.Count; ++it)
            {
                SubTree[it].CleanupTree();
            }
        }

        private void SolveSubtree(SolveItemDelegate solveitem, ExpressionTree parentitem)
        {
            /* first solve the subtrees */
            bool restart;
            int it = 0;
            do
            {
                restart = false;
                try
                {
                    for (it = 0; it < SubTree.Count; ++it)
                    {
                        SubTree[it].SolveSubtree(solveitem, this);
                    }
                }
                catch (ResetTreeIteratorCommand)
                {
                    restart = true;
                }
                catch (DeleteItemAndResetTreeIteratorCommand)
                {
                    SubTree.RemoveAt(it);
                    restart = true;
                }
            } while (restart);

            if (SubTree.Count == 0)
            {
                if (EntryType.Value != Type)
                {
                    throw new EvaluationException("solvevalues(): expression leaf is not a value (item \"" + Entry + "\")");
                }
                if (null == Value && null != solveitem)
                {
                    Value = solveitem(this, parentitem);
                }
            }
            else
            {
                for (it = 0; it < SubTree.Count; ++it)
                {
                    if (EntryType.Value == Type)
                    {
                        throw new EvaluationException("solvevalue(): non-leaf expression item cannot be a value / must be operator, array or function (item \"" + Entry + "\")");
                    }
                    if (null == Value && null != solveitem)
                    {
                        Value = solveitem(this, parentitem);
                    }
                }

                if (null == Value && null != solveitem)
                {
                    Value = solveitem(this, parentitem);
                }
            }
        }
        private void Classify(List<List<OperatorInfo>> operators,
            List<string> reservedwords,
            ref EntryType last)
        {
            if (SubTree.Count != 0)
            {
                EntryType sublast = EntryType.First == last ? EntryType.First : EntryType.Unknown;
                for (int it = 0; it < SubTree.Count; ++it)
                {
                    SubTree[it].Classify(operators, reservedwords, ref sublast);
                }
                Type = EntryType.Unknown;
                last = EntryType.Unknown;
            }
            else if (Type == EntryType.Unknown)
            {
                int i = 0;
                bool propable_operator = false;
                foreach (List<OperatorInfo> it in operators)
                {
                    foreach (OperatorInfo its in it)
                    {
                        if (Entry.StartsWith(its.Name))
                        {
                            propable_operator = true;
                        }

                        if (its.Name == Entry)
                        {
                            switch (its.Type)
                            {
                                case OperatorInfo.OperatorType.Unknown:
                                    Type = EntryType.OperatorUnknown;
                                    Precedence = i;
                                    break;
                                case OperatorInfo.OperatorType.LeftUnary:
                                    if ((last == EntryType.Unknown || last == EntryType.OperatorRightUnary) && last != EntryType.First)
                                    {
                                        break;
                                    }
                                    Type = EntryType.OperatorLeftUnary;
                                    Precedence = i;
                                    break;
                                case OperatorInfo.OperatorType.RightUnary:
                                    if (last != EntryType.Unknown)
                                    {
                                        break;
                                    }
                                    Type = EntryType.OperatorRightUnary;
                                    Precedence = i;
                                    break;
                                case OperatorInfo.OperatorType.Binary:
                                    if (last != EntryType.Unknown && last != EntryType.OperatorRightUnary)
                                    {
                                        break;
                                    }
                                    Type = EntryType.OperatorBinary;
                                    Precedence = i;
                                    break;
                            }
                        }
                        if (EntryType.Unknown != Type)
                        {
                            break;
                        }
                    }
                    if (EntryType.Unknown != Type)
                    {
                        break;
                    }
                }
                if (propable_operator && EntryType.Unknown == Type)
                {
                    Type = EntryType.Invalid;
                }

                if (Type == EntryType.Unknown)
                {
                    foreach (string itr in reservedwords)
                    {
                        if (itr == Entry)
                        {
                            Type = EntryType.Reserved;
                        }
                    }
                }

                last = Type;
            }
        }

        private void FinishClassify()
        {
            if (SubTree.Count != 0)
            {
                for (int it = 0; it < SubTree.Count; ++it)
                {
                    SubTree[it].FinishClassify();
                }
            }
            else
            {
                if (EntryType.Unknown == Type)
                {
                    Type = EntryType.Value;
                }
                if (EntryType.Invalid == Type)
                {
                    throw new EvaluationException("expression is invalid (item \"" + Entry + "\")");
                }
            }
        }

        public static readonly List<List<OperatorInfo>> ASSL_Operators = new List<List<OperatorInfo>>();
        public static readonly List<List<OperatorInfo>> LSL_Operators = new List<List<OperatorInfo>>();
        public static readonly List<string> LSL_UnsortedOperators = new List<string>();
        public static readonly List<string> LSL_ReservedWords = new List<string>(new string[]
            {
                "default",
                "do",
                "while",
                "else",
                "if",
                "for",
                "jump",
                "return",
                "state",
                "while",
                "vector",
                "rotation",
                "integer",
                "list",
                "float",
                "string",
                "print",
                "event",
                "key",
                "quaternion",
                "bool" /* OS AA */
            });

        public ExpressionTree()
        {
            m_IsLeftSquaredBrackedBinary = false;
        }

        static ExpressionTree()
        {
            List<OperatorInfo> opsubtree;

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("@", OperatorInfo.OperatorType.LeftUnary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo(",", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("++", OperatorInfo.OperatorType.RightUnary));
            opsubtree.Add(new OperatorInfo("--", OperatorInfo.OperatorType.RightUnary));
            opsubtree.Add(new OperatorInfo(".", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("[", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("]", OperatorInfo.OperatorType.RightUnary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("++", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("--", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("+", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("-", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("!", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("~", OperatorInfo.OperatorType.LeftUnary));
            opsubtree.Add(new OperatorInfo("*", OperatorInfo.OperatorType.LeftUnary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("*", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("/", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("%", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("+", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("-", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("<<", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo(">>", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("<", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("<=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo(">", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo(">=", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("==", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("!=", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("&", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("^", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("|", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("&&", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("||", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);
            ASSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("+=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("-=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("*=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("/=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("%=", OperatorInfo.OperatorType.Binary));
            LSL_Operators.Add(opsubtree);

            opsubtree = new List<OperatorInfo>();
            opsubtree.Add(new OperatorInfo("=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("+=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("-=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("*=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("/=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("%=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("&=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("^=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("|=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo("<<=", OperatorInfo.OperatorType.Binary));
            opsubtree.Add(new OperatorInfo(">>=", OperatorInfo.OperatorType.Binary));
            ASSL_Operators.Add(opsubtree);

            foreach(List<OperatorInfo> ops in ASSL_Operators)
            {
                foreach(OperatorInfo oi in ops)
                {
                    LSL_UnsortedOperators.Add(oi.Name);
                }
            }
        }

    }
}
