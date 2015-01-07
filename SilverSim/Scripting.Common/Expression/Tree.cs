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
using System.Globalization;
using System.Text;

namespace SilverSim.Scripting.Common.Expression
{
    public class Tree
    {
        public enum EntryType
        {
            Unknown,
            StringValue,
            Value,
            OperatorUnknown,
            OperatorLeftUnary, /* e.g. ++x */
            OperatorRightUnary, /* e.g. x++ */
            OperatorBinary,
            ReservedWord,
            Invalid,
            Typecast,
            Function,
            FunctionArgument,
            Declaration,
            DeclarationArgument,
            Vector,
            Rotation,
            Separator,
            LevelBegin, /* intermediate step */
            LevelEnd, /* intermediate step */
            Level,
            ExpressionTree,
            Variable
        }

        public bool ProcessedOpSort = false;

        public List<Tree> SubTree = new List<Tree>();
        public EntryType Type = EntryType.Unknown;
        public string Entry = string.Empty;

        public abstract class ValueBase
        {
            public ValueBase()
            {

            }

            public abstract ValueBase Negate();
        }

        public abstract class ConstantValue : ValueBase
        {
        }

        public class ConstantValueInt : ConstantValue
        {
            public int Value;

            public ConstantValueInt(int value)
            {
                Value = value;
            }

            public ConstantValueInt(string str)
            {
                if (str.StartsWith("0x"))
                {
                    Value = int.Parse(str, NumberStyles.HexNumber);
                }
                else if (str.StartsWith("0"))
                {
                }
                else
                {
                    Value = int.Parse(str);
                }
            }

            public new string ToString()
            {
                return Value.ToString(CultureInfo.InvariantCulture);
            }

            public override ValueBase Negate()
            {
                return new ConstantValueInt(-Value);
            }
        }

        public class ConstantValueFloat : ConstantValue
        {
            public double Value;
            public ConstantValueFloat(double value)
            {
                Value = value;
            }

            public new string ToString()
            {
                return Value.ToString(CultureInfo.InvariantCulture);
            }

            public override ValueBase Negate()
            {
                return new ConstantValueFloat(-Value);
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
                throw new NotSupportedException("strings cannot be negated");
            }
        }

        public ValueBase Value;


        public Tree()
        {

        }

        /* pre-initializes an expression tree */
        public Tree(List<string> args, List<char> opcharacters, List<char> singleopcharacters, List<char> numericchars)
        {
            Type = EntryType.ExpressionTree;
            Tree nt;
            foreach(string arg in args)
            {
                nt = null;
                if(arg.StartsWith("\""))
                {
                    nt = new Tree();
                    nt.Type = EntryType.StringValue;
                    nt.Entry = arg.Substring(1, arg.Length - 2);
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
                        nt.Entry += arg[i];
                    }
                    else if(nt != null && nt.Type == EntryType.Value && numericchars.Contains(arg[i]))
                    {
                        nt.Entry += arg[i];
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
                        nt.Entry += arg[i];
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
                        nt.Entry += arg[i];
                    }
                    else
                    {
                        if (nt != null)
                        {
                            if (nt.Type != EntryType.Unknown)
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
                        nt.Entry += arg[i];
                    }
                }
            }
        }

        public void Process()
        {
            if(Type == EntryType.StringValue)
            {
                Value = new ConstantValueString(Entry);
            }
            else if(Type == EntryType.Value)
            {
                int val;
                float fval;
                if(int.TryParse(Entry, out val) || Entry.StartsWith("0x"))
                {
                    Value = new ConstantValueInt(val);
                }
                else if(float.TryParse(Entry, NumberStyles.Float, CultureInfo.InvariantCulture, out fval))
                {
                    Value = new ConstantValueFloat(fval);
                }
                else
                {
                    throw new Resolver.ResolverException(string.Format("'{0}' is not a value", Entry));
                }
            }
        }
    }
}
