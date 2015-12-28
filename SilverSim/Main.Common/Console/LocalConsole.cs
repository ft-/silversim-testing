// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net.Core;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Main.Common.Console
{
    [Description("Local Console")]
    public class LocalConsole : CmdIO.TTY, IPlugin, IPluginShutdown
    {
        private int m_CursorXPosition;
        private int m_CursorYPosition = -1;
        readonly StringBuilder m_CommandLineBuffer = new StringBuilder();
        private bool m_EchoInput = true;
        readonly RwLockedList<string> m_CmdHistory = new RwLockedList<string>();

        readonly BlockingQueue<LoggingEvent> m_LogQueue = new BlockingQueue<LoggingEvent>();
        readonly Thread m_LogThread;
        readonly Thread m_InputThread;
        readonly object m_InputThreadLock = new object();
        private bool m_Shutdown;
        readonly string m_ConsoleTitle;

        public LocalConsole(string consoleTitle)
        {
            m_ConsoleTitle = consoleTitle;
            System.Console.Title = consoleTitle;
            CmdPrompt = "# ";
            m_LogThread = new Thread(LogThread);
            m_LogThread.Start();
            Log.LogController.Queues.Add(m_LogQueue);
            m_InputThread = new Thread(PromptThread);
            m_InputThread.IsBackground = true;
            m_InputThread.Start();
        }

        ~LocalConsole()
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
            lock (m_InputThreadLock)
            {
                m_InputThread.Abort();
            }
        }

        public override void Write(string text)
        {
            System.Console.WriteLine(text);
        }

        #region Output logic
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private int SetCursorTop(int top)
        {
            int left = System.Console.CursorLeft;

            if (left < 0)
            {
                System.Console.CursorLeft = 0;
            }
            else
            {
                int bufferWidth = System.Console.BufferWidth;

                if (bufferWidth > 0 && left >= bufferWidth)
                {
                    try
                    {
                        System.Console.CursorLeft = bufferWidth - 1;
                    }
                    catch
                    {

                    }
                }
            }

            if (top < 0)
            {
                top = 0;
            }
            else
            {
                int bufferHeight = System.Console.BufferHeight;

                if (bufferHeight > 0 && top >= bufferHeight)
                {
                    top = bufferHeight - 1;
                }
            }

            try
            {
                System.Console.CursorTop = top;
            }
            catch
            {

            }

            return top;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private int SetCursorLeft(int left)
        {
            int top = System.Console.CursorTop;

            if (top < 0)
            {
                System.Console.CursorTop = 0;
            }
            else
            {
                int bufferHeight = System.Console.BufferHeight;
                if (bufferHeight > 0 && top >= bufferHeight)
                {
                    try
                    {
                        System.Console.CursorTop = bufferHeight - 1;
                    }
                    catch
                    {

                    }
                }
            }

            if (left < 0)
            {
                left = 0;
            }
            else
            {
                int bufferWidth = System.Console.BufferWidth;

                if (bufferWidth > 0 && left >= bufferWidth)
                {
                    left = bufferWidth - 1;
                }
            }

            try
            {
                System.Console.CursorLeft = left;
            }
            catch
            {

            }

            return left;
        }

        private void Show()
        {
            lock (m_CommandLineBuffer)
            {
                if (m_CursorYPosition == -1 || System.Console.BufferWidth == 0)
                {
                    return;
                }

                int xc = CmdPrompt.Length + m_CursorXPosition;
                int new_x = xc % System.Console.BufferWidth;
                int new_y = m_CursorYPosition + xc / System.Console.BufferWidth;
                int end_y = m_CursorYPosition + (m_CommandLineBuffer.Length + CmdPrompt.Length) / System.Console.BufferWidth;

                if (end_y >= System.Console.BufferHeight) // wrap
                {
                    m_CursorYPosition--;
                    new_y--;
                    SetCursorLeft(0);
                    SetCursorTop(System.Console.BufferHeight - 1);
                    System.Console.WriteLine(" ");
                }

                m_CursorYPosition = SetCursorTop(m_CursorYPosition);
                SetCursorLeft(0);

                if (m_EchoInput)
                {
                    System.Console.Write("{0}{1}", CmdPrompt, m_CommandLineBuffer);
                }
                else
                {
                    System.Console.Write("{0}", CmdPrompt);
                }

                SetCursorTop(new_y);
                SetCursorLeft(new_x);
            }
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotUseLockedRegionOutsideMethodRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override void LockOutput()
        {
            Monitor.Enter(m_CommandLineBuffer);
            try
            {
                if (m_CursorYPosition != -1)
                {
                    m_CursorYPosition = SetCursorTop(m_CursorYPosition);
                    System.Console.CursorLeft = 0;

                    int count = m_CommandLineBuffer.Length + CmdPrompt.Length;

                    while (count-- > 0)
                    {
                        System.Console.Write(" ");
                    }

                    m_CursorYPosition = SetCursorTop(m_CursorYPosition);
                    SetCursorLeft(0);
                }
            }
            catch (Exception)
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotUseLockedRegionOutsideMethodRule")]
        public override void UnlockOutput()
        {
            if (m_CursorYPosition != -1)
            {
                m_CursorYPosition = System.Console.CursorTop;
                Show();
            }
            Monitor.Exit(m_CommandLineBuffer);
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void WriteColorText(ConsoleColor color, string sender)
        {
            try
            {
                lock (m_CommandLineBuffer)
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
                if(null != le.ExceptionObject)
                {
                    System.Console.WriteLine();
                    WriteColorText(ConsoleColor.Red, le.ExceptionObject.ToString());
                }
            }
            System.Console.WriteLine();

            UnlockOutput();
        }
        #endregion

        #region Input Function
        public override string ReadLine(string p, bool echoInput)
        {
            m_CursorXPosition = 0;
            CmdPrompt = p;
            m_EchoInput = echoInput;
            int historyLine = m_CmdHistory.Count;

            SetCursorLeft(0); // Needed for mono
            System.Console.Write(" "); // Needed for mono

            lock (m_CommandLineBuffer)
            {
                m_CursorYPosition = System.Console.CursorTop;
                m_CommandLineBuffer.Clear();
            }

            while (true)
            {
                Show();

                ConsoleKeyInfo key = System.Console.ReadKey(true);
                char enteredChar = key.KeyChar;

                if (!Char.IsControl(enteredChar))
                {
                    if (m_CursorXPosition >= 318)
                    {
                        continue;
                    }

                    m_CommandLineBuffer.Insert(m_CursorXPosition, enteredChar);
                    m_CursorXPosition++;
                }
                else
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Backspace:
                            if (m_CursorXPosition == 0)
                            {
                                break;
                            }

                            m_CommandLineBuffer.Remove(m_CursorXPosition - 1, 1);
                            m_CursorXPosition--;

                            SetCursorLeft(0);
                            m_CursorYPosition = SetCursorTop(m_CursorYPosition);

                            if (m_EchoInput)
                            {
                                System.Console.Write("{0}{1} ", CmdPrompt, m_CommandLineBuffer);
                            }
                            else
                            {
                                System.Console.Write(CmdPrompt);
                            }

                            break;

                        case ConsoleKey.Delete:
                            if (m_CursorXPosition == m_CommandLineBuffer.Length)
                            {
                                break;
                            }

                            m_CommandLineBuffer.Remove(m_CursorXPosition, 1);

                            SetCursorLeft(0);
                            m_CursorYPosition = SetCursorTop(m_CursorYPosition);

                            if (m_EchoInput)
                            {
                                System.Console.Write("{0}{1} ", CmdPrompt, m_CommandLineBuffer);
                            }
                            else
                            {
                                System.Console.Write(CmdPrompt);
                            }

                            break;

                        case ConsoleKey.End:
                            m_CursorXPosition = m_CommandLineBuffer.Length;
                            break;

                        case ConsoleKey.Home:
                            m_CursorXPosition = 0;
                            break;

                        case ConsoleKey.UpArrow:
                            if (historyLine < 1)
                            {
                                break;
                            }

                            historyLine--;
                            LockOutput();
                            m_CommandLineBuffer.Clear();
                            m_CommandLineBuffer.Append(m_CmdHistory[historyLine]);
                            m_CursorXPosition = m_CommandLineBuffer.Length;
                            UnlockOutput();
                            break;
                        case ConsoleKey.DownArrow:
                            if (historyLine >= m_CmdHistory.Count)
                            {
                                break;
                            }

                            historyLine++;
                            LockOutput();
                            if (historyLine == m_CmdHistory.Count)
                            {
                                m_CommandLineBuffer.Clear();
                            }
                            else
                            {
                                m_CommandLineBuffer.Clear();
                                m_CommandLineBuffer.Append(m_CmdHistory[historyLine]);
                            }
                            m_CursorXPosition = m_CommandLineBuffer.Length;
                            UnlockOutput();
                            break;

                        case ConsoleKey.LeftArrow:
                            if (m_CursorXPosition > 0)
                            {
                                m_CursorXPosition--;
                            }
                            break;

                        case ConsoleKey.RightArrow:
                            if (m_CursorXPosition < m_CommandLineBuffer.Length)
                            {
                                m_CursorXPosition++;
                            }
                            break;

                        case ConsoleKey.Enter:
                            SetCursorLeft(0);
                            m_CursorYPosition = SetCursorTop(m_CursorYPosition);

                            System.Console.WriteLine();

                            lock (m_CommandLineBuffer)
                            {
                                m_CursorYPosition = -1;
                            }

                            string commandLine = m_CommandLineBuffer.ToString();

                            // If we're not echoing to screen (e.g. a password) then we probably don't want it in history
                            return commandLine;

                        default:
                            break;
                    }
                }
            }
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
                catch(BlockingQueue<LoggingEvent>.TimeoutException)
                {
                    continue;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void PromptThread()
        {
            Thread.CurrentThread.Name = "Local Console Input Thread";

            for ( ;; )
            {
                string consoleTitle;
                if (SelectedScene == null)
                {
                    CmdPrompt = "(root) # ";
                    consoleTitle = m_ConsoleTitle + " # (root)";
                }
                else
                {
                    SceneInterface scene;
                    if (SceneManager.Scenes.TryGetValue(SelectedScene, out scene))
                    {
                        CmdPrompt = scene.Name + " # ";
                        consoleTitle = m_ConsoleTitle + " # " + scene.Name;
                    }
                    else
                    {
                        CmdPrompt = "(root) # ";
                        SelectedScene = UUID.Zero;
                        consoleTitle = m_ConsoleTitle + " # (root)";
                    }
                }
                try
                {
                    System.Console.Title = consoleTitle;
                }
                catch
                {

                }
                string cmd = ReadLine(CmdPrompt, true);

                if (0 == cmd.Length)
                {
                    continue;
                }
                try
                {
                    System.Console.Title = consoleTitle + " $ " + cmd;
                }
                catch
                {

                }

                if (m_CmdHistory.Count >= 100)
                {
                    m_CmdHistory.RemoveAt(0);
                }
                m_CmdHistory.Add(cmd);
                lock (m_InputThreadLock)
                {
                    CmdIO.CommandRegistry.ExecuteCommand(GetCmdLine(cmd), this);
                }
            }
        }
        #endregion
    }
}
