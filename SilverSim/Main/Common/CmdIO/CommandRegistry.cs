using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;

namespace SilverSim.Main.Common.CmdIO
{
    public static class CommandRegistry 
    {
        public delegate void CommandDelegate(List<string> args, TTY io);

        public static RwLockedDictionary<string, CommandDelegate> Commands = new RwLockedDictionary<string, CommandDelegate>();

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
                del(args, io);
            }
            catch(Exception e)
            {
                io.WriteFormatted("Command execution error {0}: {1}", e.GetType().ToString(), e.ToString());
            }
        }
    }
}
