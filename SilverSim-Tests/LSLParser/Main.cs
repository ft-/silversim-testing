using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scripting.LSL;
using SilverSim.Scripting.Common.ExpressionTree;
using System.IO;

namespace Tests.SilverSim.LSLParser
{
    public static class LSLParserTest
    {
        public static void Main(string[] args)
        {
            List<char> multilslops = new List<char>();
            List<char> singlelslops = new List<char>();
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
            singlelslops.Add('.');
            singlelslops.Add('(');
            singlelslops.Add(')');
            singlelslops.Add('[');
            singlelslops.Add(']');
            singlelslops.Add('!');
            singlelslops.Add(',');
            
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
                        Tree tree = new Tree(arguments, multilslops, singlelslops);
                        foreach (Tree ent in tree.SubTree)
                        {
                            System.Console.Write(ent.Value + " ");
                        }
                        System.Console.WriteLine();
                    }
                }
                catch (Parser.EndOfFileException)
                {

                }
            }
        }
    }
}
