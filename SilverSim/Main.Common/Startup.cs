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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SilverSim.Main.Common
{
    public class Startup
    {
        private ConfigurationLoader m_ConfigLoader;
        private readonly ManualResetEvent m_ShutdownEvent = new ManualResetEvent(false);

        public bool IsRunningAsService { get; set; }

        public ConfigurationLoader ConfigLoader => m_ConfigLoader;

        public void ExecuteCommand(List<string> args, CmdIO.TTY io)
        {
            m_ConfigLoader.ExecuteCommand(args, io, UUID.Zero);
        }

        private sealed class OutputText : CmdIO.TTY
        {
            private readonly StringBuilder m_Builder = new StringBuilder();

            public override void Write(string text)
            {
                m_Builder.Append(text);
            }

            public override string ToString() => m_Builder.ToString();
        }

        public string ExecuteCommand(List<string> args)
        {
            var t = new OutputText();
            ExecuteCommand(args, t);
            return t.ToString();
        }

        private sealed class OutputAction : CmdIO.TTY
        {
            private readonly Action<string> m_Output;
            private readonly Func<string, bool, string> m_Input;

            public OutputAction(Action<string> output, Func<string, bool, string> input = null)
            {
                m_Output = output;
                m_Input = input;
            }

            public override void Write(string text)
            {
                m_Output?.Invoke(text);
            }

            public override string ReadLine(string p, bool echoInput) => m_Input?.Invoke(p, echoInput) ?? string.Empty;
       }

        public void ExecuteCommand(List<string> args, Action<string> action)
        {
            m_ConfigLoader.ExecuteCommand(args, new OutputAction(action), UUID.Zero);
        }

        public void ExecuteCommand(List<string> args, Action<string> action, Func<string, bool, string> input)
        {
            m_ConfigLoader.ExecuteCommand(args, new OutputAction(action, input), UUID.Zero);
        }

        public bool TryGetData<T>(string datasource, out T data) => m_ConfigLoader.TryGetData(datasource, out data);

        public bool TryGetData(string datasource, out object data) => m_ConfigLoader.TryGetData(datasource, out data);

        public bool HaveData(string datasource) => m_ConfigLoader.HaveData(datasource);

        public object GetData(string datasource)
        {
            object o;
            return m_ConfigLoader.TryGetData(datasource, out o) ? o : null;
        }

        public bool Run(string[] args, Action<string> writeLine)
        {
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, m_ShutdownEvent, IsRunningAsService ? ConfigurationLoader.LocalConsole.Disallowed : ConfigurationLoader.LocalConsole.Allowed, IsRunningAsService);
            }
            catch (ConfigurationLoader.ConfigurationErrorException e)
            {
                writeLine(e.Message);
#if DEBUG
                writeLine(e.StackTrace);
#endif
                return false;
            }
            catch (ConfigurationLoader.TestingErrorException)
            {
                return false;
            }
            catch(SocketException e)
            {
                writeLine(string.Format("Startup Exception {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
                return false;
            }
            catch (Exception e)
            {
#if DEBUG
                writeLine(string.Format("Startup Exception {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
#else
                writeLine(string.Format("Startup Exception {0}: {1}", e.GetType().Name, e.Message));
#endif
                return false;
            }

            m_ShutdownEvent.WaitOne();

            m_ConfigLoader.Shutdown();
            return true;
        }

        public void Shutdown()
        {
            m_ConfigLoader?.TriggerShutdown();
        }

        ~Startup()
        {
            m_ShutdownEvent.Dispose();
        }
    }
}
