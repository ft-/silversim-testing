// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using System;
using System.IO;
using System.Reflection;

namespace SilverSim.Scene.Physics.Bullet
{
    [PluginName("BulletPhysics")]
    public class Factory : IPluginFactory
    {
        static object BulletSharpLock = new object();
        static Assembly BulletSharpAssembly = null;

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if(IsWindows())
            {
                lock (BulletSharpLock)
                {
                    if(BulletSharpAssembly != null)
                    {

                    }
                    else if (Is64BitProcess())
                    {
                        BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/windows/64/BulletSharp.dll"));
                    }
                    else
                    {
                        BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/windows/32/BulletSharp.dll"));
                    }
                }
            }
            Assembly PhysicsImplementation = Assembly.LoadFile(Path.GetFullPath("plugins/SilverSim.Scene.Physics.Bullet.Implementation.dll"));
            return ((IPluginFactory)PhysicsImplementation.CreateInstance("SilverSim.Scene.Physics.Bullet.Implementation.PluginFactory")).Initialize(loader, ownSection);
        }

        static bool IsWindows()
        {
            PlatformID platformId = Environment.OSVersion.Platform;

            return (platformId == PlatformID.Win32NT
                || platformId == PlatformID.Win32S
                || platformId == PlatformID.Win32Windows
                || platformId == PlatformID.WinCE);
        }

        static bool Is64BitProcess()
        {
            return IntPtr.Size == 8;
        }
    }
}
