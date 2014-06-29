using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scripting.LSL;
using System.IO;

namespace Tests.ArribaSim.LSLParser
{
    public static class LSLParserTest
    {
        public static void Main(string[] args)
        {
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
                        
                        ExpressionTree tree = new ExpressionTree();
                        tree.Split(arguments,
                            0, 
                            arguments.Count,
                            ExpressionTree.LSL_Operators, 
                            ExpressionTree.LSL_ReservedWords);
                        
                    }
                }
                catch (Parser.EndOfFileException)
                {

                }
            }
        }
    }
}
