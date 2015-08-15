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

        private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            AssemblyName aName = new AssemblyName(args.Name);
            if (aName.Name == "BulletSharp")
            {
                return BulletSharpAssembly;
            }
            return null;
        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            lock (BulletSharpLock)
            {
                if (null == BulletSharpAssembly)
                {
                    /* we need a special helper to actually do the BulletSharp resolver */
                    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
                }

                switch (Environment.OSVersion.Platform)
                { 
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                        if(BulletSharpAssembly != null)
                        {

                        }
                        else if (Environment.Is64BitProcess)
                        {
                            BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/windows/64/BulletSharp.dll"));
                        }
                        else
                        {
                            BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/windows/32/BulletSharp.dll"));
                        }
                        break;

                    case PlatformID.MacOSX:
                        if(BulletSharpAssembly != null)
                        {

                        }
                        else if (Environment.Is64BitProcess)
                        {
                            BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/macosx/64/BulletSharp.dll"));
                        }
                        else
                        {
                            BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/macosx/32/BulletSharp.dll"));
                        }
                        break;

                    case PlatformID.Unix:
                        if(BulletSharpAssembly != null)
                        {

                        }
                        else if (Environment.Is64BitProcess)
                        {
                            BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/linux/64/BulletSharp.dll"));
                        }
                        else
                        {
                            BulletSharpAssembly = Assembly.LoadFile(Path.GetFullPath("platform-libs/linux/32/BulletSharp.dll"));
                        }
                        break;

                    default:
                        throw new NotSupportedException("Unsupported platform " + Environment.OSVersion.Platform.ToString() + " for bullet physics");
                }
            }
            Assembly PhysicsImplementation = Assembly.LoadFile(Path.GetFullPath("plugins/SilverSim.Scene.Physics.Bullet.Implementation.dll"));
            return ((IPluginFactory)PhysicsImplementation.CreateInstance("SilverSim.Scene.Physics.Bullet.Implementation.PluginFactory")).Initialize(loader, ownSection);
        }
    }
}
