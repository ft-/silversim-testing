// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Main.Common.CmdIO
{
    public static class CommandRegistry 
    {
        // for documentation:
        //public delegate void CommandDelegate(List<string> args, TTY io, UUID limitedToScene /* is UUID.Zero for all allowed */);

        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> Commands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> CreateCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> DeleteCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> LoadCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> SaveCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> ShowCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> SetCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> GetCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> ChangeCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> ClearCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> EmptyCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> StartCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> RestartCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> StopCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> SelectCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> EnableCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> DisableCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> AlertCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        public static readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> KickCommands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();

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
            Commands.Add("select", new CommandType("select", SelectCommands).Command_Handler);
            Commands.Add("clear", new CommandType("clear", ClearCommands).Command_Handler);
            Commands.Add("empty", new CommandType("empty", EmptyCommands).Command_Handler);
            Commands.Add("restart", new CommandType("restart", RestartCommands).Command_Handler);
            Commands.Add("start", new CommandType("start", StartCommands).Command_Handler);
            Commands.Add("stop", new CommandType("stop", StopCommands).Command_Handler);
            Commands.Add("enable", new CommandType("enable", EnableCommands).Command_Handler);
            Commands.Add("disable", new CommandType("disable", DisableCommands).Command_Handler);
            Commands.Add("alert", new CommandType("alert", AlertCommands).Command_Handler);
            Commands.Add("kick", new CommandType("kick", KickCommands).Command_Handler);
        }

        sealed class CommandType
        {
            readonly string m_Command;
            readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_Dict;
            public CommandType(string command, RwLockedDictionary<string, Action<List<string>, TTY, UUID>> dict)
            {
                m_Command = command;
                m_Dict = dict;
            }

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public void Command_Handler(List<string> args, TTY io, UUID limitedToScene)
            {
                Action<List<string>, TTY, UUID> del;
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public static void ExecuteCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            Action<List<string>, TTY, UUID> del;
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
