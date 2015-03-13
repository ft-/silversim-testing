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

using System;

namespace SilverSim.Main.Common
{
    public static class VersionInfo
    {
        public static string ProductName
        { 
            get
            {
                return "SilverSim/Arriba";
            }
        }

        public static string Shard
        {
            get
            {
                return "Development";
            }
        }

        public static string Version
        {
            get
            {
                return "Test";
            }
        }

        public static bool IsPlatformMono
        {
            get 
            { 
                return Type.GetType("Mono.Runtime") != null;
            }
        }

        public static string RuntimeInformation
        {
            get
            {
                string ru = String.Empty;

                if(Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ru = "Win";
                }
                else if(Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    ru = "OSX";
                }
                else
                {
                    ru = Environment.OSVersion.Platform.ToString();
                }

                if(IsPlatformMono)
                {
                    ru += "/Mono";
                }
                else
                {
                    ru += "/.NET";
                }

                return ru;
            }
        }

        public static string SimulatorVersion
        {
            get
            {
                if (Environment.Is64BitProcess)
                {
                    return string.Format("{0} {1} {2} {3}", ProductName, Version, RuntimeInformation, MachineWidth);
                }
                else
                {
                    return string.Format("{0} {1} {2} {3}", ProductName, Version, RuntimeInformation, MachineWidth);
                }
            }
        }

        public static string MachineWidth
        {
            get
            {
                if(Environment.Is64BitProcess)
                {
                    return "64-bit";
                }
                else
                {
                    return "32-bit";
                }
            }
        }
    }
}
