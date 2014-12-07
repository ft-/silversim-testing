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

namespace SilverSim.Scripting.LSL
{
    public class Parser : ParserBase
    {
        public Parser()
        {

        }

        public void read_pass_1(List<string> args)
        {
            char c;
            string token = string.Empty;
            begin();
            args.Clear();
            bool is_preprocess = false;
            int parenthesislevel = 0;

            for(;;)
            {
                c = readc();
redo:
                switch(c)
                {
                    case '\x20':
                    case '\x09':
                    case '\r':
                        break;
                    case '\n':      /* these all are simply white space */
                        if(is_preprocess)
                        {
                            if(0 == args.Count)
                                return;
                            if(args[args.Count - 1] != "\\")
                                return;
                        }
                        break;

                    case ';':       /* end of statement */
                        if(0 != token.Length)
                            args.Add(token);
                        args.Add(";");
                        if (args.Count != 0 && parenthesislevel == 0)
                            return;
                        break;

                    case '{':       /* opening statement */
                        if(0 != token.Length)
                            args.Add(token);
                        args.Add("{");
                        return;

                    case '}':       /* closing statement */
                        if(0 != token.Length)
                            args.Add(token);
                        args.Add("}");
                        return;
                
                    case '\"':      /* string literal */
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        do
                        {
                            if(c == '\\')
                            {
                                token += (char) c;
                                c = readc();
                                token += (char) c;
                            }
                            else
                                token += (char) c;
                            c = readc();
                        } while(c != '\"');
                        token += "\"";
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        break;
            
                    case '\'':      /* string literal */
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        do
                        {
                            if(c == '\\')
                            {
                                token += (char) c;
                                c = readc();
                                token += (char) c;
                            }
                            else
                                token += (char) c;
                            c = readc();
                        } while(c != '\'');
                        token += "\'";
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        break;

                    case '@':
                    case ',':       /* special tokens (all these do not make up compound literals) */
                    case '~':
                    case '?':
                    case '\\':
                    case '[':
                    case ']':
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        args.Add(new string(new char[] {c}));
                        break;

                    case '(':
                        ++parenthesislevel;
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        args.Add(new string(new char[] {c}));
                        break;

                    case ')':
                        if(parenthesislevel == 0)
                        {
                            throw new ParenthesisMismatchError();
                        }
                        --parenthesislevel;
                        if(0 != token.Length)
                            args.Add(token);
                        token = "";
                        args.Add(new string(new char[] {c}));
                        break;

                    case '<':
                        if(is_preprocess)
                        {
                            if(args.Count != 0)
                            {
                                if(args[0] == "#include" || args[0] == "#include_once")
                                {
                                    /* preprocessor literal */
                                    if(0 != token.Length)
                                        args.Add(token);
                                    token = "";
                                    c = '\"';
                                    do
                                    {
                                        token += (char) c;
                                        c = readc();
                                    } while(c != '>');
                                    token += "\"";
                                    if(0 != token.Length)
                                        args.Add(token);
                                    token = "";
                                    break;
                                }
                            }
                        }
                        /* fall-through since it is a special case only in preprocessor handling */
                        goto defaultcase;

                    default:        /* regular tokens */
                defaultcase:
                        if(Char.IsWhiteSpace(c))
                        {
                            if(0 != token.Length)
                            {
                                args.Add(token);
                                token = "";
                            }
                        }
                        else
                        {
                            if(token == "" && args.Count == 0 && c == '#')
                                is_preprocess = true;
                            while(!Char.IsWhiteSpace(c) && c != ';' && c != '(' && c != ')' && c != ',' && c != '~' && c != '\\' && c != '?' && c != '@' && c != '{' && c != '}' && c != '[' && c != ']')
                            {
                                token += (char) c;

                                if(token == "//")
                                {
                                    /* got C++-style comment */
                                    while(c != '\n')
                                    {
                                        try
                                        {
                                            c = readc();
                                        }
                                        catch(EndOfFileException e)
                                        {
                                            if(args.Count != 0)
                                                return;
                                            throw e;
                                        }
                                    }
                                    token = "";
                                    goto redo;
                                }
                                if(token == "/*")
                                {
                                    /* got C-style comment */
                                    for(;;)
                                    {
                                        try
                                        {
                                            c = readc();
                                        }
                                        catch(EndOfFileException e)
                                        {
                                            if(args.Count != 0)
                                                return;
                                            throw e;
                                        }
                                        if(c != '*')
                                            continue;
                                        do
                                        {
                                            try
                                            {
                                                c = readc();
                                            }
                                            catch(EndOfFileException e)
                                            {
                                                if(args.Count != 0)
                                                    return;
                                                throw e;
                                            }
                                        } while(c == '*');

                                        if(c == '/')
                                            break;
                                    }

                                    token = "";
                                    c = ' ';
                                    goto redo;
                                }

                                try
                                {
                                    c = readc();
                                }
                                catch(EndOfFileException e)
                                {
                                    if(token.Length != 0)
                                    {
                                        args.Add(token);
                                        return;
                                    }
                                    throw e;
                                }
                            }
                            args.Add(token);
                            token = "";
                            goto redo;
                        }
                        break;
                }
            }
        }

        public void eval_compounds(List<string> args)
        {
            for(int argi = 0; argi < args.Count; ++argi)
            {
                int i;
                char c = '\0';
                char lastc = '\0';
                int curlength = args[argi].Length;
                for(i = 0; i < curlength;)
                {
                    lastc = c;
                    c = args[argi][i];
                    /* ignore strings first */
                    if('\"' == c)
                        break;
                    else if('\'' == c)
                        break;
                    else if ('+' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal += */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "+=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '+')
                            {
                                /* compound literal ++ (contextual dependancy if there are more) */
                                int j = 2;
                                while (j < curlength && args[argi][j] == '+') 
                                    ++j;
                                if(j < curlength)
                                {
                                    args.Insert(argi, args[argi].Substring(0, j));
                                    ++argi;
                                    args[argi] = args[argi].Substring(j, curlength - j);
                                    curlength -= j;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "+");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('.' == c)
                    {
                        if(i + 1 < curlength)
                        {
                            if (!Char.IsDigit(args[argi][i + 1]))
                            {
                                if(i > 0)
                                {
                                    args.Insert(argi, args[argi].Substring(0, i));
                                    ++argi;
                                    args[argi] = args[argi].Substring(i, curlength - i);
                                    curlength -= i;
                                }
                                args.Insert(argi++, ".");
                        
                                i = 0;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
                            }
                            else
                                ++i;
                        }
                        else
                            ++i;
                    }
                    else if ('-' == c && ((lastc != 'e' && lastc != 'E') || args.Count == 0 || !char.IsDigit(args[argi][0])))
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal -= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "-=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '>')
                            {
                                /* compound literal -= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "->");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '-')
                            {
                                /* compound literal -- (contextual dependancy if there are more) */
                                int j = 2;
                                while (j < curlength && args[argi][j] == '-') 
                                    ++j;
                                if(j < curlength)
                                {
                                    args.Insert(argi, args[argi].Substring(0, j));
                                    ++argi;
                                    args[argi] = args[argi].Substring(j, curlength - j);
                                    curlength -= j;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "-");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('*' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal *= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "*=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "*");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if(':' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == ':')
                            {
                                /* compound literal :: */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "::");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, ":");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('/' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal /= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "/=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "/");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('%' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal %= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "%=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "%");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('<' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal <= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "<=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '<')
                            {
                                /* compound literals <<, <<= */
                                if (3 < curlength ? args[argi][2] == '=' : false)
                                {
                                    args.Insert(argi++, "<<=");
                                    args[argi] = args[argi].Substring(3, curlength - 3);
                                    curlength -= 3;
                                }
                                else if(2 < curlength)
                                {
                                    args.Insert(argi++, "<<");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "<");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('>' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal >= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, ">=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '>')
                            {
                                /* compound literals << <<= */
                                if (3 < curlength ? args[argi][2] == '=' : false)
                                {
                                    args.Insert(argi++, ">>=");
                                    args[argi] = args[argi].Substring(3, curlength - 3);
                                    curlength -= 3;
                                }
                                else if(2 < curlength)
                                {
                                    args.Insert(argi++, ">>");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, ">");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('=' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal == */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "==");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '>')
                            {
                                /* compound literal == */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "=>");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "=");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('!' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal != */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "!=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "!");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('^' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal ^= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "^=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "^");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('&' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal &= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "&=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '&')
                            {
                                /* compound literal && */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "&&");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "&");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('#' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '#')
                            {
                                /* compound literal ## */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "##");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "#");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else if('|' == c)
                    {
                        if(i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }
                    
                        i = 0;
                        if(1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal |= */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "|=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else if (args[argi][1] == '|')
                            {
                                /* compound literal || */
                                if(2 < curlength)
                                {
                                    args.Insert(argi++, "||");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                    break;
                            }
                            else
                            {
                                args.Insert(argi++, "|");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                            break;
                    }
                    else
                        ++i;
                }
            }
        }

        public void read_pass_2(List<string> arguments)
        {
            List<string> inargs = new List<string>(arguments);
            arguments.Clear();
            foreach(string it in inargs)
            {
                if(it == "+++")
                {
                    arguments.Add("++");
                    arguments.Add("+");
                }
                else if(it == "+++++")
                {
                    arguments.Add("++");
                    arguments.Add("+");
                    arguments.Add("++");
                }
                else if(it == "++++")
                {
                    arguments.Add("++");
                    arguments.Add("+");
                    arguments.Add("+");
                }
                else if(it == "---")
                {
                    arguments.Add("--");
                    arguments.Add("-");
                }
                else if(it == "----")
                {
                    arguments.Add("--");
                    arguments.Add("-");
                    arguments.Add("-");
                }
                else if(it == "-----")
                {
                    arguments.Add("--");
                    arguments.Add("-");
                    arguments.Add("--");
                }
                else
                {
                    arguments.Add(it);
                }
            }
        }

        public override void read(List<string> args)
        {
            read_pass_1(args);
            eval_compounds(args);
            read_pass_2(args);
        }

    }
}
