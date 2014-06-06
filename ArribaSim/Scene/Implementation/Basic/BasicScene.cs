/*
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

using ArribaSim.Scene.Management.IM;
using ArribaSim.Scene.ServiceInterfaces.Chat;
using ArribaSim.Scene.Types.Scene;
using ArribaSim.Types;
using ArribaSim.Types.IM;
using System;

namespace ArribaSim.Scene.Implementation.Basic
{
    class BasicScene : SceneInterface, ISceneObjects, ISceneObjectGroups, ISceneObjectParts
    {
        private ChatServiceInterface m_ChatService;

        public BasicScene(ChatServiceInterface chatService, UUID id, GridVector position, uint sizeX, uint sizeY)
        {
            ID = id;
            GridPosition = position;
            SizeX = sizeX;
            SizeY = sizeY;
            m_ChatService = chatService;
            IMRouter.SceneIM.Add(IMSend);
            OnRemove += RemoveScene;
        }

        private bool IMSend(GridInstantMessage im)
        {
            return false;
        }

        private void RemoveScene(SceneInterface s)
        {
            IMRouter.SceneIM.Remove(IMSend);
        }

        public override ISceneObjects Objects
        {
            get
            {
                return this;
            }
        }
        
        public override ISceneObjectGroups ObjectGroups 
        { 
            get
            {
                return this;
            }
        }

        public override ISceneObjectParts Primitives 
        { 
            get
            {
                return this;
            }
        }
    }
}
