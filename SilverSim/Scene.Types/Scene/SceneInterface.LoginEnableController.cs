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

using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [Flags]
        public enum ReadyFlags : ulong
        {
            None = 0,
            PhysicsTerrain = 1 << 0,
            SceneObjects = 1 << 1,
            Remove = 1 << 2,
            LoginsEnable = 1 << 3,

            ExpectedFlags = PhysicsTerrain | SceneObjects
        }

        public readonly LoginController LoginControl;

        public class LoginController
        {
            private ReadyFlags m_CurrentFlags = ReadyFlags.ExpectedFlags;
            private readonly object m_Lock = new object();
            private readonly SceneInterface m_Scene;

            public LoginController(SceneInterface scene)
            {
                m_Scene = scene;
            }

            public void Ready(ReadyFlags lf)
            {
                lock(m_Lock)
                {
                    ReadyFlags oldFlags = m_CurrentFlags;
                    m_CurrentFlags &= ~lf;
                    if (oldFlags != ReadyFlags.None && m_CurrentFlags == ReadyFlags.None)
                    {
                        TriggerLoginsEnabled(true);
                    }
                }
            }

            public void NotReady(ReadyFlags lf)
            {
                lock (m_Lock)
                {
                    ReadyFlags oldFlags = m_CurrentFlags;
                    m_CurrentFlags |= lf;
                    if (oldFlags == ReadyFlags.None && m_CurrentFlags != ReadyFlags.None)
                    {
                        TriggerLoginsEnabled(false);
                    }
                }
            }

            public bool IsLoginEnabled => m_CurrentFlags == ReadyFlags.None;

            private void TriggerLoginsEnabled(bool state)
            {
                UUID sceneID = m_Scene.ID;
                foreach (Action<UUID, bool> del in OnLoginsEnabled?.GetInvocationList() ?? new Delegate[0])
                {
                    try
                    {
                        del(sceneID, state);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                    }
                }
            }

            public event Action<UUID, bool> OnLoginsEnabled;
        }
    }
}
