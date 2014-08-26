using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
