﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [Flags]
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum ReadyFlags : ulong
        {
            None = 0,
            PhysicsTerrain = 1 << 0,
            SceneObjects = 1 << 1,
            Remove = 1 << 2,
            LoginsEnable = 1 << 3,

            ExpectedFlags = PhysicsTerrain | SceneObjects
        }

        public readonly LoginController LoginControl = new LoginController();

        [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
        public class LoginController
        {
            ReadyFlags m_CurrentFlags = ReadyFlags.ExpectedFlags;
            readonly object m_Lock = new object();

            public LoginController()
            {

            }

            public void Ready(ReadyFlags lf)
            {
                lock(m_Lock)
                {
                    ReadyFlags oldFlags = m_CurrentFlags;
                    m_CurrentFlags &= (~lf);
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

            public bool IsLoginEnabled
            {
                get
                {
                    return m_CurrentFlags == ReadyFlags.None;
                }
            }

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            void TriggerLoginsEnabled(bool state)
            {
                var ev = OnLoginsEnabled;
                if(ev != null)
                {
                    foreach (Action<bool> del in ev.GetInvocationList().OfType<Action<bool>>())
                    {
                        try
                        {
                            del(state);
                        }
                        catch (Exception e)
                        {
                            m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                        }
                    }
                }
            }

            public event Action<bool> OnLoginsEnabled;
        }
    }
}
