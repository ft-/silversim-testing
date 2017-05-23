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

using System;
using System.Reflection;
using System.Text;

namespace SilverSim.Main.Common
{
    public static class VersionInfo
    {
        public static string ProductName => "SilverSim/Arriba";

        public static string Shard => "Development";

        public static string Version => "Test";

        public static bool IsPlatformMono => Type.GetType("Mono.Runtime") != null;

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
                    Type type = Type.GetType("Mono.Runtime");
                    if (type != null)
                    {
                        MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                        ru += displayName != null ? "/" + displayName.Invoke(null, null) : "/Mono";
                    }
                    else
                    {
                        ru += "/Mono";
                    }
                }
                else
                {
                    ru += "/.NET";
                }

                return ru;
            }
        }

        public static string ArchSpecificId
        {
            get
            {
                var ru = new StringBuilder();
                ru.Append(Environment.OSVersion.Platform.ToString());
                ru.Append('-');
                ru.Append(Environment.Is64BitProcess ? "64" : "32");
                return ru.ToString();
            }
        }

        public static string PlatformLibPath
        {
            get
            {
                var ru = new StringBuilder("platform-libs/");
                switch(Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                        ru.Append("windows/");
                        break;

                    case PlatformID.MacOSX:
                        ru.Append("macosx/");
                        break;

                    case PlatformID.Unix:
                        ru.Append("unix/");
                        break;

                    default:
                        ru.Append("unknown/");
                        break;
                }

                ru.Append(Environment.Is64BitProcess ? "64" : "32");
                return ru.ToString();
            }
        }

        public static string SimulatorVersion => string.Format("{0} {1} {2} {3}", ProductName, Version, RuntimeInformation, MachineWidth);

        public static string MachineWidth => Environment.Is64BitProcess ?
                    "64-bit" :
                    "32-bit";
    }
}
