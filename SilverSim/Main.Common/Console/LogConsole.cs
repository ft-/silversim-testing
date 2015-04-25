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

using log4net.Core;
using System;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Main.Common.Console
{
    public class LogConsole : CmdIO.TTY, IPlugin, IPluginShutdown
    {
        private BlockingQueue<LoggingEvent> m_LogQueue = new BlockingQueue<LoggingEvent>();
        private Thread m_LogThread;
        private bool m_Shutdown = false;

        public LogConsole()
        {
            m_LogThread = new Thread(LogThread);
            m_LogThread.Start();
            Log.LogController.Queues.Add(m_LogQueue);
        }

        ~LogConsole()
        {
            Log.LogController.Queues.Remove(m_LogQueue);
        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public void Shutdown()
        {
            m_Shutdown = true;
        }

        public override void Write(string text)
        {
            System.Console.WriteLine(text);
        }

        #region Output logic
        public new void LockOutput()
        {
        }

        public new void UnlockOutput()
        {
        }

        private void WriteColorText(ConsoleColor color, string sender)
        {
            try
            {
                lock (this)
                {
                    try
                    {
                        System.Console.ForegroundColor = color;
                        System.Console.Write(sender);
                        System.Console.ResetColor();
                    }
                    catch (ArgumentNullException)
                    {
                        // Some older systems dont support coloured text.
                        System.Console.WriteLine(sender);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static readonly ConsoleColor[] Colors =
        {
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow
        };

        private void LogWrite(LoggingEvent le)
        {
            string timeStr = le.TimeStamp.ToString("HH:mm:ss");
            string outText = string.Format("{0} - [{1}]: {2}", timeStr, le.LoggerName, le.RenderedMessage.Trim());

            LockOutput();

            if (le.Level == Level.Error)
            {
                WriteColorText(ConsoleColor.Red, outText);
                if (null != le.ExceptionObject)
                {
                    System.Console.WriteLine();
                    WriteColorText(ConsoleColor.Red, le.ExceptionObject.ToString());
                }
            }
            else if (le.Level == Level.Warn)
            {
                WriteColorText(ConsoleColor.Yellow, outText);
                if (null != le.ExceptionObject)
                {
                    System.Console.WriteLine();
                    WriteColorText(ConsoleColor.Red, le.ExceptionObject.ToString());
                }
            }
            else
            {
                System.Console.Write("{0} - ", timeStr);
                System.Console.Write("[");
                ConsoleColor color = Colors[(Math.Abs(le.LoggerName.ToUpper().GetHashCode()) % Colors.Length)];
                WriteColorText(color, le.LoggerName);
                System.Console.Write("]: ");
                System.Console.Write(le.RenderedMessage.Trim());
                if (null != le.ExceptionObject)
                {
                    System.Console.WriteLine();
                    WriteColorText(ConsoleColor.Red, le.ExceptionObject.ToString());
                }
            }
            System.Console.WriteLine();

            UnlockOutput();
        }
        #endregion

        #region Console Threads
        private void LogThread()
        {
            Thread.CurrentThread.Name = "Local Console Log Thread";
            for (; !m_Shutdown; )
            {
                try
                {
                    LogWrite(m_LogQueue.Dequeue(1000));
                }
                catch (BlockingQueue<LoggingEvent>.TimeoutException)
                {
                    continue;
                }
            }
        }

        #endregion
    }
}
