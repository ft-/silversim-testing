// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Main.Common.CmdIO
{
    public static class CommandRegistry 
    {
        public delegate void CommandDelegate(List<string> args, TTY io, UUID limitedToScene /* is UUID.Zero for all allowed */);

        public static readonly RwLockedDictionary<string, CommandDelegate> Commands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> CreateCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> DeleteCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> LoadCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> SaveCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> ShowCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> SetCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> GetCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static readonly RwLockedDictionary<string, CommandDelegate> ChangeCommands = new RwLockedDictionary<string, CommandDelegate>();

        static CommandRegistry()
        {
            Commands.Add("load", new CommandType("load", LoadCommands).Command_Handler);
            Commands.Add("save", new CommandType("save", SaveCommands).Command_Handler);
            Commands.Add("get", new CommandType("get", GetCommands).Command_Handler);
            Commands.Add("set", new CommandType("set", SetCommands).Command_Handler);
            Commands.Add("show", new CommandType("show", ShowCommands).Command_Handler);
            Commands.Add("create", new CommandType("create", CreateCommands).Command_Handler);
            Commands.Add("delete", new CommandType("delete", DeleteCommands).Command_Handler);
            Commands.Add("change", new CommandType("change", ChangeCommands).Command_Handler);
        }

        class CommandType
        {
            string m_Command;
            RwLockedDictionary<string, CommandDelegate> m_Dict;
            public CommandType(string command, RwLockedDictionary<string, CommandDelegate> dict)
            {
                m_Command = command;
                m_Dict = dict;
            }

            public void Command_Handler(List<string> args, TTY io, UUID limitedToScene)
            {
                CommandDelegate del;
                if (args.Count < 2)
                {
                    if (args[0] == "help")
                    {
                        string commands = m_Command + " command list:\n";
                        foreach(string cmd in m_Dict.Keys)
                        {
                            commands += string.Format("{0} {1}\n", m_Command, cmd);
                        }
                        io.Write(commands);
                    }
                    else
                    {
                        io.Write("Invalid " + m_Command + " command");
                    }
                    return;
                }
                try
                {
                    del = m_Dict[args[1]];
                }
                catch (Exception)
                {
                    io.WriteFormatted("Unsupported {1} command '{0}'", args[1], m_Command);
                    return;
                }

                try
                {
                    del(args, io, limitedToScene);
                }
                catch (Exception e)
                {
                    io.WriteFormatted("Command execution error {0}: {1}", e.GetType().ToString(), e.ToString());
                }

            }
        }

        public static void ExecuteCommand(List<string> args, TTY io)
        {
            ExecuteCommand(args, io, UUID.Zero);
        }

        public static void ExecuteCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            CommandDelegate del;
            if(args.Count == 0)
            {
                return;
            }
            else if (args[0] == "help")
            {
                if (args.Count == 1)
                {
                    string commands = "Command list:\n";
                    foreach (string cmd in Commands.Keys)
                    {
                        commands += string.Format("{0}\n", cmd);
                    }
                    io.Write(commands);
                    return;
                }
                else
                {
                    try
                    {
                        del = Commands[args[1]];
                        args.RemoveAt(1);
                    }
                    catch (Exception)
                    {
                        io.WriteFormatted("Invalid command '{0}' for help", args[1]);
                        return;
                    }
                }
            }
            else
            {
                try
                {
                    del = Commands[args[0]];
                }
                catch (Exception)
                {
                    io.WriteFormatted("Invalid command '{0}'", args[0]);
                    return;
                }
            }

            try
            {
                del(args, io, limitedToScene);
            }
            catch (Exception e)
            {
                io.WriteFormatted("Command execution error {0}: {1}", e.GetType().ToString(), e.ToString());
            }
        }
    }
}
