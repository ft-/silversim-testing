// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilverSim.Main.Common.CmdIO
{
    public class CommandRegistry
    {
        private readonly object m_RegisterCmdGroupLock = new object();

        public RwLockedDictionary<string, Action<List<string>, TTY, UUID>> Commands { get; } = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
        private readonly Dictionary<string, RwLockedDictionary<string, Action<List<string>, TTY, UUID>>> m_SubCommands = new Dictionary<string, RwLockedDictionary<string, Action<List<string>, TTY, UUID>>>();

        public RwLockedDictionary<string, Action<List<string>, TTY, UUID>> CheckAddCommandType(string cmd)
        {
            RwLockedDictionary<string, Action<List<string>, TTY, UUID>> dict;
            lock (m_RegisterCmdGroupLock)
            {
                if(!m_SubCommands.TryGetValue(cmd, out dict))
                {
                    dict = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
                    Commands.Add(cmd, new CommandType(cmd, dict).Command_Handler);
                    m_SubCommands.Add(cmd, dict);
                }
                return dict;
            }
        }

        public void AddRemoveAllCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            RwLockedDictionary<string, Action<List<string>, TTY, UUID>> removecmds = CheckAddCommandType("remove");
            RwLockedDictionary<string, Action<List<string>, TTY, UUID>> removeallcmds;
            lock (m_RegisterCmdGroupLock)
            {
                if (!m_SubCommands.TryGetValue("remove all", out removeallcmds))
                {
                    removeallcmds = new RwLockedDictionary<string, Action<List<string>, TTY, UUID>>();
                    removecmds.Add("all", new SubCommandType("remove all", removeallcmds).Command_Handler);
                    m_SubCommands.Add("remove all", removeallcmds);
                }
            }
            removeallcmds.Add(cmd, handler);
        }

        public void AddCreateCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("create").Add(cmd, handler);
        }

        public void AddDeleteCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("delete").Add(cmd, handler);
        }

        public void AddUnregisterCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("unregister").Add(cmd, handler);
        }

        public void AddLoadCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("load").Add(cmd, handler);
        }

        public void AddSaveCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("save").Add(cmd, handler);
        }

        public void AddGetCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("get").Add(cmd, handler);
        }

        public void AddSetCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("set").Add(cmd, handler);
        }

        public void AddResetCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("reset").Add(cmd, handler);
        }

        public void AddRemoveCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("remove").Add(cmd, handler);
        }

        public void AddShowCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("show").Add(cmd, handler);
        }

        public void AddChangeCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("change").Add(cmd, handler);
        }

        public void AddSelectCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("select").Add(cmd, handler);
        }

        public void AddClearCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("clear").Add(cmd, handler);
        }

        public void AddEmptyCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("empty").Add(cmd, handler);
        }

        public void AddRestartCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("restart").Add(cmd, handler);
        }

        public void AddStartCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("start").Add(cmd, handler);
        }

        public void AddStopCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("stop").Add(cmd, handler);
        }

        public void AddEnableCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("enable").Add(cmd, handler);
        }

        public void AddDisableCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("disable").Add(cmd, handler);
        }

        public void AddAlertCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("alert").Add(cmd, handler);
        }

        public void AddKickCommand(string cmd, Action<List<string>, TTY, UUID> handler)
        {
            CheckAddCommandType("kick").Add(cmd, handler);
        }

        private sealed class CommandType
        {
            private readonly string m_Command;
            private readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_Dict;
            public CommandType(string command, RwLockedDictionary<string, Action<List<string>, TTY, UUID>> dict)
            {
                m_Command = command;
                m_Dict = dict;
            }

            public void Command_Handler(List<string> args, TTY io, UUID limitedToScene)
            {
                Action<List<string>, TTY, UUID> del;
                if (args.Count < 2)
                {
                    if (args[0] == "help")
                    {
                        var commands = new StringBuilder(m_Command + " command list:\n");
                        var sorted = new SortedDictionary<string, Action<List<string>, TTY, UUID>>(m_Dict);
                        foreach(string cmd in sorted.Keys)
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

        private sealed class SubCommandType
        {
            private readonly string m_Command;
            private readonly RwLockedDictionary<string, Action<List<string>, TTY, UUID>> m_Dict;
            public SubCommandType(string command, RwLockedDictionary<string, Action<List<string>, TTY, UUID>> dict)
            {
                m_Command = command;
                m_Dict = dict;
            }

            public void Command_Handler(List<string> args, TTY io, UUID limitedToScene)
            {
                Action<List<string>, TTY, UUID> del;
                if (args.Count < 3)
                {
                    if (args[0] == "help")
                    {
                        var commands = new StringBuilder(m_Command + " command list:\n");
                        var sorted = new SortedDictionary<string, Action<List<string>, TTY, UUID>>(m_Dict);
                        foreach (string cmd in sorted.Keys)
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
                    del = m_Dict[args[2]];
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
                    var commands = new StringBuilder("Command list:\n");
                    SortedDictionary<string, Action<List<string>, TTY, UUID>> sorted = new SortedDictionary<string, Action<List<string>, TTY, UUID>>(Commands);
                    foreach (string cmd in sorted.Keys)
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
