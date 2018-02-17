using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenJp2.Net
{
    internal static class J2cInit
    {
        public static bool m_Inited { get; private set; }
        private static readonly object m_InitLock = new object();

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        public static void InitOpenJP2()
        {
            lock (m_InitLock)
            {
                if (!m_Inited)
                {
                    /* preload necessary windows dll */
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        if (Environment.Is64BitProcess)
                        {
                            if (IntPtr.Zero == LoadLibrary(Path.GetFullPath("platform-libs/windows/64/openjp2.dll")))
                            {
                                throw new FileNotFoundException("missing platform-libs/windows/64/openjp2.dll");
                            }
                        }
                        else
                        {
                            if (IntPtr.Zero == LoadLibrary(Path.GetFullPath("platform-libs/windows/32/openjp2.dll")))
                            {
                                throw new FileNotFoundException("missing platform-libs/windows/32/openjp2.dll");
                            }
                        }
                    }
                    m_Inited = true;
                }
            }
        }
    }
}
