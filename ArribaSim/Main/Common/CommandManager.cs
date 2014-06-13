/*

ArribaSim is distributed under the terms of the
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
using System.IO;
using ThreadedClasses;

namespace ArribaSim.Main.Common
{
    public static class CommandManager
    {
        public delegate void CommandDelegate(string[] args, Stream consoleStream);

        struct CommandInfo
        {
            public CommandDelegate Delegate;
            public string Category;
            public string Description;
            public string Help;
        }

        private static RwLockedDictionary<string, CommandInfo> m_Commands = new RwLockedDictionary<string, CommandInfo>();

        public static void ClearCommands()
        {
            m_Commands.Clear();
        }

        public static void AddCommand(string category, string command, CommandDelegate del, string description, string help)
        {
            CommandInfo ci;
            ci.Delegate = del;
            ci.Description = description;
            ci.Category = category;
            ci.Help = help;

            m_Commands.Add(command, ci);
        }

        public static void Help(string[] args, Stream consoleStream)
        {
            if(args.Length == 0)
            {
                SortedList<string, string> sortedList = new SortedList<string, string>();
                foreach(KeyValuePair<string, CommandInfo> kvp in m_Commands)
                {
                    sortedList.Add(kvp.Value.Category + ":" + kvp.Key, kvp.Key + " - " + kvp.Value.Description);
                }
                using(StreamWriter ts = new StreamWriter(consoleStream))
                {
                    foreach(KeyValuePair<string, string> kvp in sortedList)
                    {
                        ts.Write(String.Format("{0}\n", kvp.Value));
                    }
                }
            }
        }

        public static CommandDelegate GetCommand(string command)
        {
            if(command == "help")
            {
                return Help;
            }
            return m_Commands[command].Delegate;
        }
    }
}
