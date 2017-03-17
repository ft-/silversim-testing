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

using SilverSim.Threading;
using SilverSim.Types.IM;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Management.IM
{
    public class IMRouter
    {
        #region Fields
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public readonly RwLockedList<Func<GridInstantMessage, bool>> OfflineIM = new RwLockedList<Func<GridInstantMessage, bool>>();
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public readonly RwLockedList<Func<GridInstantMessage, bool>> GridIM = new RwLockedList<Func<GridInstantMessage, bool>>();
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public readonly RwLockedList<Func<GridInstantMessage, bool>> SceneIM = new RwLockedList<Func<GridInstantMessage, bool>>();
        #endregion

        #region Methods
        public void SendWithResultDelegate(GridInstantMessage im)
        {
            im.OnResult(im, SendSync(im));
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public bool SendSync(GridInstantMessage im)
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
