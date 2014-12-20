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
using SilverSim.Scripting.LSL;
using SilverSim.Scripting.Common.Expression;
using System.IO;

namespace Tests.SilverSim.LSLParser
{
    public static class LSLParserTest
    {
        public static void Main(string[] args)
        {
            List<char> multilslops = new List<char>();
            List<char> singlelslops = new List<char>();
            List<char> numericchars = new List<char>();
            singlelslops.Add('~');
            multilslops.Add('+');
            multilslops.Add('-');
            multilslops.Add('*');
            multilslops.Add('/');
            multilslops.Add('%');
            multilslops.Add('<');
            multilslops.Add('>');
            multilslops.Add('=');
            multilslops.Add('&');
            multilslops.Add('|'); 
            multilslops.Add('^');
            singlelslops.Add('.');
            singlelslops.Add('(');
            singlelslops.Add(')');
            singlelslops.Add('[');
            singlelslops.Add(']');
            singlelslops.Add('!');
            singlelslops.Add(',');
            singlelslops.Add('@');
            numericchars.Add('.');
            numericchars.Add('a');
            numericchars.Add('b');
            numericchars.Add('c');
            numericchars.Add('d');
            numericchars.Add('e');
            numericchars.Add('f');
            numericchars.Add('A');
            numericchars.Add('B');
            numericchars.Add('C');
            numericchars.Add('D');
            numericchars.Add('E');
            numericchars.Add('F');
            numericchars.Add('x');
            numericchars.Add('+');
            numericchars.Add('-');

            List<string> reservedWords = new List<string>();
            reservedWords.Add("for");
            reservedWords.Add("if");
            reservedWords.Add("while");
            reservedWords.Add("do");
            reservedWords.Add("jump");
            reservedWords.Add("return");
            reservedWords.Add("state");
            reservedWords.Add("integer");
            reservedWords.Add("float");
            reservedWords.Add("string");
            reservedWords.Add("key");
            reservedWords.Add("list");
            reservedWords.Add("vector");
            reservedWords.Add("rotation");
            List<Dictionary<string, Resolver.OperatorType>> operators = new List<Dictionary<string, Resolver.OperatorType>>();
            Dictionary<string, string> blockOps = new Dictionary<string,string>();
            blockOps.Add("(", ")");
            blockOps.Add("[", "]");

            Dictionary<string, Resolver.OperatorType> plist;
            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("@", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("++", Resolver.OperatorType.RightUnary);
            plist.Add("--", Resolver.OperatorType.RightUnary);
            plist.Add(".", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("++", Resolver.OperatorType.LeftUnary);
            plist.Add("--", Resolver.OperatorType.LeftUnary);
            plist.Add("+", Resolver.OperatorType.LeftUnary);
            plist.Add("-", Resolver.OperatorType.LeftUnary);
            plist.Add("!", Resolver.OperatorType.LeftUnary);
            plist.Add("~", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("*", Resolver.OperatorType.Binary);
            plist.Add("/", Resolver.OperatorType.Binary);
            plist.Add("%", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("+", Resolver.OperatorType.Binary);
            plist.Add("-", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("<<", Resolver.OperatorType.Binary);
            plist.Add(">>", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("<", Resolver.OperatorType.Binary);
            plist.Add("<=", Resolver.OperatorType.Binary);
            plist.Add(">", Resolver.OperatorType.Binary);
            plist.Add(">=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("==", Resolver.OperatorType.Binary);
            plist.Add("!=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("&", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("^", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("|", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("&&", Resolver.OperatorType.Binary);
            plist.Add("||", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("=", Resolver.OperatorType.Binary);
            plist.Add("+=", Resolver.OperatorType.Binary);
            plist.Add("-=", Resolver.OperatorType.Binary);
            plist.Add("*=", Resolver.OperatorType.Binary);
            plist.Add("/=", Resolver.OperatorType.Binary);
            plist.Add("%=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("=", Resolver.OperatorType.Binary);
            plist.Add("+=", Resolver.OperatorType.Binary);
            plist.Add("-=", Resolver.OperatorType.Binary);
            plist.Add("*=", Resolver.OperatorType.Binary);
            plist.Add("/=", Resolver.OperatorType.Binary);
            plist.Add("%=", Resolver.OperatorType.Binary);
            plist.Add("&=", Resolver.OperatorType.Binary);
            plist.Add("^=", Resolver.OperatorType.Binary);
            plist.Add("|=", Resolver.OperatorType.Binary);
            plist.Add("<<=", Resolver.OperatorType.Binary);
            plist.Add(">>=", Resolver.OperatorType.Binary);


            Resolver resolver = new Resolver(reservedWords, operators, blockOps);
            
            foreach(string arg in args)
            {
                TextReader reader;
                try
                {
                    reader = new StreamReader(new FileStream(arg, FileMode.Open));
                }
                catch(FileNotFoundException)
                {
                    System.Console.WriteLine(string.Format("File {0} not found", arg));
                    return;
                }
                List<string> arguments = new List<string>();

                try
                {
                    Parser parser = new Parser();
                    parser.push(reader, arg);
                    for (; ; )
                    {
                        arguments.Clear();
                        parser.read(arguments);
                        foreach (string ent in arguments)
                        {
                            System.Console.Write(ent + " ");
                        }
                        System.Console.WriteLine();

                        System.Console.Write("O:" + parser.getfileinfo().LineNumber.ToString()+ ": ");
                        Tree tree = new Tree(arguments.GetRange(0, arguments.Count - 1), multilslops, singlelslops, numericchars);
                        foreach (Tree ent in tree.SubTree)
                        {
                            if (ent.Type == Tree.EntryType.StringValue)
                            {
                                System.Console.Write("\"" + ent.Entry + "\" ");
                            }
                            else
                            {
                                System.Console.Write(ent.Entry + " ");
                            }
                        }
                        System.Console.WriteLine();

                        if (arguments[arguments.Count - 1] == ";")
                        {
                            resolver.Process(tree);
                        }
                    }
                }
                catch (Parser.EndOfFileException)
                {

                }
            }
        }
    }
}
