﻿/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Main.Common;
using System;
using System.Threading;

namespace ArribaSim.Main.Simulator
{
    public static class Application
    {
        private const string DEFAULT_CONFIG_FILENAME = "../data/ArribaSim.ini";

        public static ConfigurationLoader m_ConfigLoader;

        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "ArribaSim:Main";
            try
            {
                m_ConfigLoader = new ConfigurationLoader(args, DEFAULT_CONFIG_FILENAME, "Simulator.defaults.ini");
            }
            catch(ConfigurationLoader.ConfigurationError e)
            {
#if DEBUG
                System.Console.Write(e.StackTrace.ToString());
                System.Console.WriteLine();
#endif
                Environment.Exit(1);
            }
            catch(Exception e)
            {
#if DEBUG
                System.Console.Write(String.Format("Exception {0}: {1}", e.GetType().Name, e.Message));
                System.Console.Write(e.StackTrace.ToString());
                System.Console.WriteLine();
#endif
                Environment.Exit(1);
            }
        }
    }
}
