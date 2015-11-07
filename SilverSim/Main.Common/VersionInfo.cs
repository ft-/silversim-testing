// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SilverSim.Main.Common
{
    public static class VersionInfo
    {
        [SuppressMessage("Gendarme.Rules.Portability", "DoNotHardcodePathsRule", Justification = "Gendarme misinterprets the string")]
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

        [SuppressMessage("Gendarme.Rules.Portability", "DoNotHardcodePathsRule", Justification = "Gendarme misinterprets the string")]
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
                        if (displayName != null)
                        {
                            ru += "/" + displayName.Invoke(null, null);
                        }
                        else
                        {
                            ru += "/Mono";
                        }
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

        public static string SimulatorVersion
        {
            get
            {
                return string.Format("{0} {1} {2} {3}", ProductName, Version, RuntimeInformation, MachineWidth);
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
