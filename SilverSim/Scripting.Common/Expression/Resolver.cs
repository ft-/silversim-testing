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

namespace SilverSim.Scripting.Common.Expression
{
    public class Resolver
    {
        public class ResolverException : Exception
        {
            public ResolverException(string msg)
                : base(msg)
            {

            }
        }

        public enum OperatorType
        {
            Unknown,
            RightUnary,
            LeftUnary,
            Binary
        }

        List<string> m_ReservedWords;
        List<Dictionary<string, OperatorType>> m_Operators;
        Dictionary<string, string> m_BlockOps;

        public Resolver(List<string> reservedWords, List<Dictionary<string, OperatorType>> operators, Dictionary<string, string> blockOps)
        {
            m_ReservedWords = reservedWords;
            m_Operators = operators;
            m_BlockOps = blockOps;
        }

        void resolveValues(Tree nt)
        {
            if(nt.Type == Tree.EntryType.StringValue || nt.Type == Tree.EntryType.Value)
            {
                nt.Process();
            }

            foreach(Tree st in nt.SubTree)
            {
                resolveValues(st);
            }
        }

        void resolveSeparators(Tree nt)
        {
            if (nt.Entry == ",")
            {
                nt.Type = Tree.EntryType.Separator;
            }

            foreach (Tree st in nt.SubTree)
            {
                resolveSeparators(st);
            }
        }

        void resolveBlockOps(Tree nt)
        {
            if(nt.Type == Tree.EntryType.OperatorUnknown || nt.Type == Tree.EntryType.Unknown)
            {
                if(m_BlockOps.ContainsKey(nt.Entry))
                {
                    nt.Type = Tree.EntryType.LevelBegin;
                }
                if (m_BlockOps.ContainsValue(nt.Entry))
                {
                    nt.Type = Tree.EntryType.LevelEnd;
                }
            }
            foreach (Tree st in nt.SubTree)
            {
                resolveBlockOps(st);
            }
        }

        void identifyReservedWords(Tree nt)
        {
            if(nt.Type == Tree.EntryType.Unknown)
            {
                if(m_ReservedWords.Contains(nt.Entry))
                {
                    nt.Type = Tree.EntryType.ReservedWord;
                }
            }

            foreach(Tree st in nt.SubTree)
            {
                identifyReservedWords(st);
            }
        }

        void sortBlockOps(Tree nt, int i = 0)
        {
            List<Tree> blockSort = new List<Tree>();

            for (i = 0; i < nt.SubTree.Count; )
            {
                if (nt.SubTree[i].Type == Tree.EntryType.LevelBegin)
                {
                    Tree sub = nt.SubTree[i];
                    nt.SubTree[i].Type = Tree.EntryType.Level;
                    if (blockSort.Count != 0)
                    {
                        nt.SubTree.RemoveAt(i);
                        blockSort[blockSort.Count - 1].SubTree.Add(sub);
                    }
                    else
                    {
                        ++i;
                    }
                    blockSort.Add(sub);
                }
                else if (nt.SubTree[i].Type == Tree.EntryType.LevelEnd)
                {
                    if (m_BlockOps[blockSort[blockSort.Count - 1].Entry] != nt.SubTree[i].Entry)
                    {
                        throw new ResolverException(string.Format("'{1}' does not match '{0}'", blockSort[blockSort.Count - 1].Entry, nt.SubTree[i].Entry));
                    }
                    nt.SubTree.RemoveAt(i);
                    blockSort.RemoveAt(blockSort.Count - 1);
                }
                else
                {
                    if (blockSort.Count > 0)
                    {
                        blockSort[blockSort.Count - 1].SubTree.Add(nt.SubTree[i]);
                        nt.SubTree.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }
            if(blockSort.Count != 0)
            {
                throw new ResolverException(string.Format("'{0}' missing a matching '{1}'", blockSort[blockSort.Count - 1].Entry, m_BlockOps[blockSort[blockSort.Count - 1].Entry]));
            }
        }

        void identifyFunctions(Tree nt)
        {
            foreach(Tree sub in nt.SubTree)
            {
                identifyFunctions(sub);
            }
            for(int i = 0; i < nt.SubTree.Count - 1;++i)
            {
                if(nt.SubTree[i].Type == Tree.EntryType.Unknown &&
                    nt.SubTree[i + 1].Type == Tree.EntryType.Level &&
                    nt.SubTree[i + 1].Entry == "(")
                {
                    Tree st = nt.SubTree[i + 1];
                    nt.SubTree.RemoveAt(i + 1);
                    nt.SubTree[i].Type = Tree.EntryType.Function;

                    /* rebuild arguments */
                    List<Tree> argumentsList = new List<Tree>();
                    Tree argument = null;
                    bool lastIsSeparator = false;
                    for(int j = 0; j < st.SubTree.Count; ++j)
                    {
                        if(st.SubTree[j].Type != Tree.EntryType.Separator)
                        {
                            if(argument == null)
                            {
                                argument = new Tree();
                                argumentsList.Add(argument);
                                argument.Type = Tree.EntryType.FunctionArgument;
                            }
                            argument.SubTree.Add(st.SubTree[j]);
                            lastIsSeparator = false;
                        }
                        else
                        {
                            if(argument == null)
                            {
                                throw new ResolverException(string.Format("Missing parameter to function '{0}'", nt.SubTree[i].Entry));
                            }
                            argument = null;
                            lastIsSeparator = true;
                        }
                    }

                    if(lastIsSeparator)
                    {
                        throw new ResolverException(string.Format("Missing parameter to function '{0}'", nt.SubTree[i].Entry));
                    }
                    nt.SubTree[i].SubTree = argumentsList;
                }
            }
        }

        void reorderDeclarationArguments(Tree nt, int start, int end)
        {
            List<Tree> argumentsInput = nt.SubTree.GetRange(start + 1, end - start - 1);
            nt.SubTree.RemoveRange(start + 1, end - start);
            nt.SubTree[start].Type = Tree.EntryType.Declaration;

            /* rebuild arguments */
            List<Tree> argumentsList = new List<Tree>();
            Tree argument = null;
            bool lastIsSeparator = false;
            for (int j = 0; j < argumentsInput.Count; ++j)
            {
                if (argumentsInput[j].Type != Tree.EntryType.Separator)
                {
                    if (argument == null)
                    {
                        argument = new Tree();
                        argumentsList.Add(argument);
                        argument.Type = Tree.EntryType.DeclarationArgument;
                    }
                    argument.SubTree.Add(argumentsInput[j]);
                    lastIsSeparator = false;
                }
                else
                {
                    if (argument == null)
                    {
                        throw new ResolverException("Missing parameter to declaration by '<' and '>'");
                    }
                    argument = null;
                    lastIsSeparator = true;
                }
            }

            if (lastIsSeparator)
            {
                throw new ResolverException("Missing parameter to declaration by '<' and '>'");
            }
            nt.SubTree[start].SubTree = argumentsList;
        }

        void identifySquareBracketDeclarations(Tree nt)
        {
            int start ;
            foreach(Tree st in nt.SubTree)
            {
                identifySquareBracketDeclarations(st);
            }

            for(start = 0; start < nt.SubTree.Count; ++start)
            {
                Tree startnode = nt.SubTree[start];
                if(startnode.Entry == "<")
                {
                    if(start == 0)
                    {
                        int end;
                        /* definitely a declaration, search for end */
                        for(end = start + 1; end < nt.SubTree.Count; ++end)
                        {
                            if(nt.SubTree[end].Entry == ">")
                            {
                                if(end == nt.SubTree.Count - 1)
                                {
                                    /* has to be a declaration */
                                    break;
                                }
                                else if(nt.SubTree[end + 1].Type == Tree.EntryType.OperatorUnknown ||
                                    nt.SubTree[end + 1].Type == Tree.EntryType.OperatorBinary ||
                                    nt.SubTree[end + 1].Type == Tree.EntryType.Separator)
                                {
                                    /* has to be a declaration */
                                    break;
                                }
                            }
                        }
                        if(end == nt.SubTree.Count)
                        {
                            throw new ResolverException("Missing end of declaration begun by '<'");
                        }
                        else
                        {
                            reorderDeclarationArguments(nt, start, end);
                        }
                    }
                    else if(nt.SubTree[start - 1].Type == Tree.EntryType.OperatorUnknown ||
                        nt.SubTree[start - 1].Type == Tree.EntryType.OperatorBinary ||
                        nt.SubTree[start - 1].Type == Tree.EntryType.Separator)
                    {
                        int end;
                        /* a declaration too */
                        for (end = start + 1; end < nt.SubTree.Count; ++end)
                        {
                            if (nt.SubTree[end].Entry == ">")
                            {
                                if (end == nt.SubTree.Count - 1)
                                {
                                    /* has to be a declaration */
                                    break;
                                }
                                else if (nt.SubTree[end + 1].Type == Tree.EntryType.OperatorUnknown ||
                                    nt.SubTree[end + 1].Type == Tree.EntryType.OperatorBinary ||
                                    nt.SubTree[end + 1].Type == Tree.EntryType.Separator)
                                {
                                    /* has to be a declaration */
                                    break;
                                }
                            }
                        }
                        if (end == nt.SubTree.Count)
                        {
                            throw new ResolverException("Missing end of declaration begun by '<'");
                        }
                        else
                        {
                            reorderDeclarationArguments(nt, start, end);
                        }
                    }
                }
            }
        }

        bool isValidLeftHand(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.OperatorRightUnary:
                case Tree.EntryType.StringValue:
                case Tree.EntryType.Value:
                case Tree.EntryType.Function:
                case Tree.EntryType.Declaration:
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Level:
                    return true;

                default:
                    return false;
            }
        }

        bool isValidRightHand(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.OperatorLeftUnary:
                case Tree.EntryType.StringValue:
                case Tree.EntryType.Value:
                case Tree.EntryType.Function:
                case Tree.EntryType.Declaration:
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Level:
                    return true;

                default:
                    return false;
            }
        }

        bool isValidUnaryLeft(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Value:
                case Tree.EntryType.StringValue:
                    return true;

                default:
                    return false;
            }
        }

        bool isValidUnaryRight(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.Unknown:
                    return true;

                default:
                    return false;
            }
        }

        void identifyOps(Tree nt)
        {
            int pos;

            foreach(Tree st in nt.SubTree)
            {
                identifyOps(st);
            }

            foreach (Dictionary<string, OperatorType> oilist in m_Operators)
            {
                for (pos = 0; pos < nt.SubTree.Count; ++pos)
                {
                    Tree st = nt.SubTree[pos];
                    if (st.Type == Tree.EntryType.OperatorUnknown)
                    {
                        OperatorType optype;
                        if(oilist.TryGetValue(st.Entry, out optype))
                        {
                            switch (optype)
                            {
                                case OperatorType.Binary:
                                    if (pos > 0 && pos < nt.SubTree.Count - 1)
                                    {
                                        if(isValidLeftHand(nt.SubTree[pos - 1]) &&
                                            isValidRightHand(nt.SubTree[pos + 1]))
                                        {
                                            st.Type = Tree.EntryType.OperatorBinary;
                                        }
                                    }
                                    break;
                                case OperatorType.LeftUnary:
                                    if(pos == 0 && pos < nt.SubTree.Count - 1)
                                    {
                                        if(isValidUnaryLeft(nt.SubTree[pos + 1]))
                                        {
                                            st.Type = Tree.EntryType.OperatorLeftUnary;
                                        }
                                    }
                                    else if (pos > 0 && pos < nt.SubTree.Count - 1 &&
                                        (nt.SubTree[pos - 1].Type != Tree.EntryType.OperatorUnknown ||
                                        nt.SubTree[pos - 1].Type != Tree.EntryType.OperatorBinary) &&
                                        isValidUnaryRight(nt.SubTree[pos + 1]))
                                    {
                                        st.Type = Tree.EntryType.OperatorLeftUnary;
                                    }
                                    break;
                                case OperatorType.RightUnary:
                                    if (pos > 0)
                                    {
                                        if (isValidUnaryRight(nt.SubTree[pos - 1]))
                                        {
                                            st.Type = Tree.EntryType.OperatorRightUnary;
                                        }
                                    }
                                    break;
                            }
                        }
                        if (st.Type != Tree.EntryType.OperatorUnknown)
                        {
                            break;
                        }
                    }
                }
            }
        }

        void sortOps(Tree tree)
        {
            int pos;
            foreach(Dictionary<string, OperatorType> plist in m_Operators)
            {
                for(pos = 0; pos < tree.SubTree.Count;)
                {
                    switch(tree.SubTree[pos].Type)
                    {
                        case Tree.EntryType.OperatorBinary:
                            if (plist.ContainsKey(tree.SubTree[pos].Entry) &&
                                !tree.SubTree[pos].ProcessedOpSort)
                            {
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos - 1]);
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos + 1]);
                                tree.SubTree[pos].ProcessedOpSort = true;
                                tree.SubTree.RemoveAt(pos + 1);
                                tree.SubTree.RemoveAt(pos - 1);
                            }
                            else
                            {
                                ++pos;
                            }
                            break;

                        case Tree.EntryType.OperatorLeftUnary:
                            if (plist.ContainsKey(tree.SubTree[pos].Entry) &&
                                !tree.SubTree[pos].ProcessedOpSort)
                            {
                                tree.SubTree[pos].ProcessedOpSort = true;
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos + 1]);
                                tree.SubTree.RemoveAt(pos + 1);
                            }
                            else
                            {
                                ++pos;
                            }
                            break;

                        case Tree.EntryType.OperatorRightUnary:
                            if (plist.ContainsKey(tree.SubTree[pos].Entry) &&
                                !tree.SubTree[pos].ProcessedOpSort)
                            {
                                tree.SubTree[pos].ProcessedOpSort = true;
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos - 1]);
                                tree.SubTree.RemoveAt(pos - 1);
                            }
                            else
                            {
                                ++pos;
                            }
                            break;

                        default:
                            ++pos;
                            break;
                    }
                }
            }

            foreach(Tree st in tree.SubTree)
            {
                sortOps(st);
            }
        }

        public void Process(Tree tree)
        {
            identifyReservedWords(tree);
            resolveValues(tree);
            resolveSeparators(tree);
            resolveBlockOps(tree);
            sortBlockOps(tree);
            identifySquareBracketDeclarations(tree);
            identifyFunctions(tree);
            identifyOps(tree);
            sortOps(tree);
        }
    }
}
