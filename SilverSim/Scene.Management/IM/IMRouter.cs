// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.IM;
using ThreadedClasses;

namespace SilverSim.Scene.Management.IM
{
    public static class IMRouter
    {
        #region Fields
        public static RwLockedList<OnSendDelegate> OfflineIM = new RwLockedList<OnSendDelegate>();
        public static RwLockedList<OnSendDelegate> GridIM = new RwLockedList<OnSendDelegate>();
        public static RwLockedList<OnSendDelegate> SceneIM = new RwLockedList<OnSendDelegate>();
        #endregion

        public delegate bool OnSendDelegate(GridInstantMessage im);

        #region Methods
        public static void SendWithResultDelegate(GridInstantMessage im)
        {
            im.OnResult(im, SendSync(im));
        }

        public static bool SendSync(GridInstantMessage im)
        {
            bool success = false;
            foreach (OnSendDelegate del in SceneIM)
            {
                success = success || del(im);
            }
            foreach (OnSendDelegate del in GridIM)
            {
                success = success || del(im);
            }
            if (!success)
            {
                foreach (OnSendDelegate del in OfflineIM)
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
