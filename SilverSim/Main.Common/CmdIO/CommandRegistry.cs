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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Main.Common.CmdIO
{
    public static class CommandRegistry 
    {
        public delegate void CommandDelegate(List<string> args, TTY io, UUID limitedToScene /* is UUID.Zero for all allowed */);

        public static RwLockedDictionary<string, CommandDelegate> Commands = new RwLockedDictionary<string, CommandDelegate>();
        public static RwLockedDictionary<string, CommandDelegate> LoadCommands = new RwLockedDictionary<string, CommandDelegate>();
        public static RwLockedDictionary<string, CommandDelegate> SaveCommands = new RwLockedDictionary<string, CommandDelegate>();

        static CommandRegistry()
        {
            Commands.Add("load", LoadCommand_Handler);
            Commands.Add("save", SaveCommand_Handler);
        }

        static void LoadCommand_Handler(List<string> args, TTY io, UUID limitedToScene)
        {
            CommandDelegate del;
            if(args.Count < 2)
            {
                io.Write("Invalid load command");
                return;
            }
            try
            {
                del = LoadCommands[args[1]];
            }
            catch (Exception)
            {
                io.WriteFormatted("Unsupported load command '{0}'", args[1]);
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

        static void SaveCommand_Handler(List<string> args, TTY io, UUID limitedToScene)
        {
            CommandDelegate del;
            if (args.Count < 2)
            {
                io.Write("Invalid save command");
                return;
            }
            try
            {
                del = SaveCommands[args[1]];
            }
            catch (Exception)
            {
                io.WriteFormatted("Unsupported save command '{0}'", args[1]);
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

        public static void ExecuteCommand(List<string> args, TTY io)
        {
            CommandDelegate del;
            try
            {
                del = Commands[args[0]];
            }
            catch(Exception)
            {
                io.WriteFormatted("Invalid command '{0}'", args[0]);
                return;
            }

            try
            {
                del(args, io, UUID.Zero);
            }
            catch(Exception e)
            {
                io.WriteFormatted("Command execution error {0}: {1}", e.GetType().ToString(), e.ToString());
            }
        }

        public static void ExecuteCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            CommandDelegate del;
            try
            {
                del = Commands[args[0]];
            }
            catch (Exception)
            {
                io.WriteFormatted("Invalid command '{0}'", args[0]);
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
}
