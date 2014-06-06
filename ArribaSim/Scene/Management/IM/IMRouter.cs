﻿/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Types.IM;
using ThreadedClasses;

namespace ArribaSim.Scene.Management.IM
{
    public static class IMRouter
    {
        #region Fields
        public static RwLockedList<OnSendDelegate> GridIM = new RwLockedList<OnSendDelegate>();
        public static RwLockedList<OnSendDelegate> SceneIM = new RwLockedList<OnSendDelegate>();
        #endregion

        public delegate bool OnSendDelegate(GridInstantMessage im);

        #region Methods
        public static void Send(GridInstantMessage im)
        {
            bool success = false;
            foreach(OnSendDelegate del in SceneIM)
            {
                success = success || del(im);
            }
            foreach(OnSendDelegate del in GridIM)
            {
                success = success || del(im);
            }
            im.OnResult(im, success);
        }
        #endregion
    }
}
