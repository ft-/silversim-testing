// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SilverSim.Main.Common.CmdIO
{
    public class CommandRegistry 
    {
        // for documentation:
        //public delegate void CommandDelegate(List<string> args, TTY io, UUID limitedToScene /* is UUID.Zero for all allowed */);

        readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_Commands = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_CreateCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_DeleteCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_LoadCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_SaveCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_ShowCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_SetCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_ResetCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_GetCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_ChangeCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_ClearCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_RemoveCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_EmptyCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_StartCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_RestartCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_StopCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_SelectCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_EnableCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_DisableCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_AlertCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_KickCommands;
        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_UnregisterCommands;

        readonly object m_RegisterCmdGroupLock = new object();

        public RwLockedDictionary<string, Action<List<string>, TTY, UUID>> Commands
        {
            get
            {
                return m_Commands;
            }
        }

        RwLockedDictionary<string, Action<List<string>, TTY, UUID>> CheckAddCommandType(string cmd, ref RwLockedDictionary<string, Action<List<string>, TTY, UUID>> dict)
        {
            lock (m_RegisterCmdGroupLock)
            {
                if(null == dict)
                {
                    dict = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
                    Commands.Add(cmd, new CommandType(cmd, dict).Command_Handler);
                }
                return dict;
            }
        }

        public void AddCreateCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("create", ref m_CreateCommands).Add(cmd, handler);
        }

        public void AddDeleteCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("delete", ref m_DeleteCommands).Add(cmd, handler);
        }

        public void AddUnregisterCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("unregister", ref m_UnregisterCommands).Add(cmd, handler);
        }

        public void AddLoadCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("load", ref m_LoadCommands).Add(cmd, handler);
        }

        public void AddSaveCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("save", ref m_SaveCommands).Add(cmd, handler);
        }

        public void AddGetCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("get", ref m_GetCommands).Add(cmd, handler);
        }

        public void AddSetCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("set", ref m_SetCommands).Add(cmd, handler);
        }

        public void AddResetCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("reset", ref m_ResetCommands).Add(cmd, handler);
        }

        public void AddRemoveCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("remove", ref m_RemoveCommands).Add(cmd, handler);
        }

        public void AddShowCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("show", ref m_ShowCommands).Add(cmd, handler);
        }

        public void AddChangeCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("change", ref m_ChangeCommands).Add(cmd, handler);
        }

        public void AddSelectCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("select", ref m_SelectCommands).Add(cmd, handler);
        }

        public void AddClearCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("clear", ref m_ClearCommands).Add(cmd, handler);
        }

        public void AddEmptyCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("empty", ref m_EmptyCommands).Add(cmd, handler);
        }

        public void AddRestartCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("restart", ref m_RestartCommands).Add(cmd, handler);
        }

        public void AddStartCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("start", ref m_StartCommands).Add(cmd, handler);
        }

        public void AddStopCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("stop", ref m_StopCommands).Add(cmd, handler);
        }

        public void AddEnableCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("enable", ref m_EnableCommands).Add(cmd, handler);
        }

        public void AddDisableCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("disable", ref m_DisableCommands).Add(cmd, handler);
        }

        public void AddAlertCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("alert", ref m_AlertCommands).Add(cmd, handler);
        }

        public void AddKickCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("kick", ref m_KickCommands).Add(cmd, handler);
        }

        public CommandRegistry()
        {
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
                        StringBuilder commands = new StringBuilder(m_Command + " command list:\n");
                        foreach(string cmd in m_Dict.Keys)
                        {
                            commands.AppendFormat("{0} {1}\n", m_Command, cmd);
                        }
                        io.Write(commands.ToString());
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

        public void ExecuteCommand(List<string> args, TTY io)
        {
            ExecuteCommand(args, io, UUID.Zero);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void ExecuteCommand(List<string> args, TTY io, UUID limitedToScene)
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
                    StringBuilder commands = new StringBuilder("Command list:\n");
                    foreach (string cmd in Commands.Keys)
                    {
                        commands.AppendFormat("{0}\n", cmd);
                    }
                    io.Write(commands.ToString());
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
