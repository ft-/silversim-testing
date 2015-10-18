﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Main.Common.CmdIO
{
    public abstract class TTY 
    {
        public TTY()
        {

        }

        public UUID SelectedScene = UUID.Zero;

        public abstract void Write(string text);
        
        public void WriteFormatted(string format, params object[] parms)
        {
            Write(String.Format(format, parms));
        }

        public virtual bool HasPrompt 
        { 
            get
            {
                return false;
            }
        }

        public virtual void LockOutput()
        {

        }

        public virtual void UnlockOutput()
        {

        }

        public string CmdPrompt { get; set; }

        public virtual string ReadLine(string p, bool echoInput)
        {
            return string.Empty;
        }

        public string GetInput(string prompt)
        {
            return ReadLine(String.Format("{0}: ", prompt), true);
        }

        public string GetInput(string prompt, string defaultvalue)
        {
            string res = ReadLine(String.Format("{0} [{1}]: ", prompt, defaultvalue), true);
            if(0 == res.Length)
            {
                res = defaultvalue;
            }

            return res;
        }

        public string GetPass(string prompt)
        {
            return ReadLine(String.Format("{0}: ", prompt), false);
        }

        public List<string> GetCmdLine(string cmdline)
        {
            List<string> cmdargs = new List<string>();
            cmdline = cmdline.Trim();
            if (0 == cmdline.Length)
            {
                return cmdargs;
            }

            bool indoublequotes = false;
            bool insinglequotes = false;
            bool inargument = false;
            bool hasescape = false;
            string argument = string.Empty;

            foreach (char c in cmdline)
            {
                if (indoublequotes)
                {
                    if (hasescape)
                    {
                        hasescape = false;
                        argument += c.ToString();
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                                hasescape = true;
                                break;

                            case '\"':
                                indoublequotes = false;
                                cmdargs.Add(argument);
                                argument = string.Empty;
                                break;

                            default:
                                argument += c.ToString();
                                break;
                        }
                    }
                }
                else if (insinglequotes)
                {
                    if (hasescape)
                    {
                        hasescape = false;
                        argument += c.ToString();
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                                hasescape = true;
                                break;

                            case '\"':
                                insinglequotes = false;
                                cmdargs.Add(argument);
                                argument = string.Empty;
                                break;

                            default:
                                argument += c.ToString();
                                break;
                        }
                    }
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (inargument)
                    {
                        cmdargs.Add(argument);
                        argument = string.Empty;
                    }
                    inargument = false;
                }
                else if(c == '\"')
                {
                    if(inargument)
                    {
                        cmdargs.Add(argument);
                        argument = string.Empty;
                    }
                    indoublequotes = true;
                }
                else if (c == '\'')
                {
                    if (inargument)
                    {
                        cmdargs.Add(argument);
                        argument = string.Empty;
                    }
                    insinglequotes = true;
                }
                else
                {
                    argument += c.ToString();
                    inargument = true;
                }
            }

            if (indoublequotes || insinglequotes || inargument)
            {
                cmdargs.Add(argument);
            }

            return cmdargs;
        }
    }
}
