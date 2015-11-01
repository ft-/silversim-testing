// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.IM;
using System;
using System.Diagnostics.CodeAnalysis;
using ThreadedClasses;

namespace SilverSim.Scene.Management.IM
{
    public static class IMRouter
    {
        #region Fields
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static RwLockedList<Func<GridInstantMessage, bool>> OfflineIM = new RwLockedList<Func<GridInstantMessage, bool>>();
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static RwLockedList<Func<GridInstantMessage, bool>> GridIM = new RwLockedList<Func<GridInstantMessage, bool>>();
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static RwLockedList<Func<GridInstantMessage, bool>> SceneIM = new RwLockedList<Func<GridInstantMessage, bool>>();
        #endregion

        #region Methods
        public static void SendWithResultDelegate(GridInstantMessage im)
        {
            im.OnResult(im, SendSync(im));
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public static bool SendSync(GridInstantMessage im)
        {
            bool success = false;
            foreach (Func<GridInstantMessage, bool> del in SceneIM)
            {
                success = success || del(im);
            }
            foreach (Func<GridInstantMessage, bool> del in GridIM)
            {
                success = success || del(im);
            }
            if (!success)
            {
                foreach (Func<GridInstantMessage, bool> del in OfflineIM)
                {
                    success = del(im);
                    if (success)
                    {
                        break;
                    }
                }
            }
            return success;
        }
        #endregion
    }
}
