using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Common.ExpressionTree
{
    public class Tree
    {
        public enum EntryType
        {
            Unknown,
            StringValue,
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

        public List<Tree> SubTree = new List<Tree>();
        public EntryType Type = EntryType.Unknown;
        public string Value = string.Empty;

        public Tree()
        {

        }

        /* pre-initializes an expression tree */
        public Tree(List<string> args, List<char> opcharacters, List<char> singleopcharacters)
        {
            Tree nt;
            foreach(string arg in args)
            {
                nt = null;
                if(arg.StartsWith("\""))
                {
                    nt = new Tree();
                    nt.Type = EntryType.StringValue;
                    nt.Value = arg.Substring(1, arg.Length - 2);
                    SubTree.Add(nt);
                    continue;
                }
                for (int i = 0; i < arg.Length; ++i)
                {
                    if (char.IsDigit(arg[0]))
                    {
                        if(nt != null)
                        {
                            if(nt.Type != EntryType.Value && nt.Type != EntryType.Unknown)
                            {
                                nt = new Tree();
                                nt.Type = EntryType.Value;
                                SubTree.Add(nt);
                            }
                        }
                        else
                        {
                            nt = new Tree();
                            nt.Type = EntryType.Value;
                            SubTree.Add(nt);
                        }
                        nt.Value += arg[i];
                    }
                    else if(nt != null && nt.Type == EntryType.Value && arg[i] == '.')
                    {
                        nt.Value += arg[i];
                    }
                    else if (singleopcharacters.Contains(arg[i]))
                    {
                        if (nt != null)
                        {
                            if (nt.Type != EntryType.OperatorUnknown)
                            {
                                nt = new Tree();
                                nt.Type = EntryType.OperatorUnknown;
                                SubTree.Add(nt);
                            }
                        }
                        else
                        {
                            nt = new Tree();
                            nt.Type = EntryType.OperatorUnknown;
                            SubTree.Add(nt);
                        }
                        nt.Value += arg[i];
                        nt = null;
                    }
                    else if (opcharacters.Contains(arg[i]))
                    {
                        if(nt != null)
                        {
                            if(nt.Type != EntryType.OperatorUnknown)
                            {
                                nt = new Tree();
                                nt.Type = EntryType.OperatorUnknown;
                                SubTree.Add(nt);
                            }
                        }
                        else
                        {
                            nt = new Tree();
                            nt.Type = EntryType.OperatorUnknown;
                            SubTree.Add(nt);
                        }
                        nt.Value += arg[i];
                    }
                    else
                    {
                        if (nt != null)
                        {
                            if (nt.Type != EntryType.Unknown && (nt.Type != EntryType.Value && arg[i] != 'f' && arg[i] != 'x'))
                            {
                                nt = new Tree();
                                nt.Type = EntryType.Unknown;
                                SubTree.Add(nt);
                            }
                        }
                        else
                        {
                            nt = new Tree();
                            nt.Type = EntryType.Unknown;
                            SubTree.Add(nt);
                        }
                        nt.Value += arg[i];
                    }
                }
            }
        }
    }
}
