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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Globalization;
using System.Linq;

namespace SilverSim.Scene.Types.Script
{
    public class LocalizedScriptMessage : IListenEventLocalization
    {
        private readonly object m_NlsRefObject;
        private readonly string m_NlsId;
        private readonly object[] m_Param;
        private readonly string m_NlsDefMessage;
        public string LinkName { get; set; }
        public int LinkNumber { get; set; }
        public string ScriptName { get; set; }
        public int LineNumber { get; set; }

        public LocalizedScriptMessage(object nlsRefObject, string nlsId, string nlsDefMessage, params object[] param)
        {
            m_NlsRefObject = nlsRefObject;
            m_NlsId = nlsId;
            m_NlsDefMessage = nlsDefMessage;
            m_Param = param;
        }

        public string Localize(ListenEvent le, CultureInfo currentCulture) =>
            string.Format(m_NlsRefObject.GetLanguageString(currentCulture, m_NlsId, m_NlsDefMessage), m_Param);
    }

    public abstract class ScriptInstance
    {
        public abstract void PostEvent(IScriptEvent e);
        public abstract bool IsRunning { get; set; }
        public bool IsRunningAllowed { get; set; }
        public bool IsAborting { get; private set; }
        /* Remove and Dispose must deregister all possible handles */
        public abstract void Remove();
        public abstract void Reset();
        public abstract void Start(int startparam = 0);
        public bool IsResetRequired { get; set; } /* only used during startup */

        public abstract void ProcessEvent();
        public abstract void ShoutError(string msg);
        public abstract void ShoutError(IListenEventLocalization localizedMessage);
        public abstract bool HasEventsPending { get; }
        public IScriptWorkerThreadPool ThreadPool;
        public event Action<ScriptInstance> OnStateChange;
        public event Action<ScriptInstance> OnScriptReset;
        public abstract IScriptState ScriptState { get; }
        public virtual bool HasTouchEvent => false;
        public virtual bool HasMoneyEvent => false;

        public abstract ObjectPartInventoryItem Item { get; }

        public abstract ObjectPart Part { get; }

        public abstract double ExecutionTime { get; set; }

        public abstract int StartParameter { get; set; }

        protected ScriptInstance()
        {
            IsAborting = false;
        }

        public void AbortBegin()
        {
            IsRunning = false;
            IsAborting = true;
        }

        public void Abort()
        {
            IsRunning = false;
            IsAborting = true;
            ThreadPool?.AbortScript(this);
        }

        public abstract bool IsLinkMessageReceiver { get; }

        public abstract void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions);

        protected void TriggerOnStateChange()
        {
            foreach (Action<ScriptInstance> del in OnStateChange?.GetInvocationList().OfType<Action<ScriptInstance>>() ?? new Action<ScriptInstance>[0])
            {
                try
                {
                    del(this);
                }
                catch
                {
                    /* no action required */
                }
            }
        }

        protected void TriggerOnScriptReset()
        {
            foreach (Action<ScriptInstance> del in OnScriptReset?.GetInvocationList().OfType<Action<ScriptInstance>>() ?? new Action<ScriptInstance>[0])
            {
                try
                {
                    del(this);
                }
                catch
                {
                    /* no action required */
                }
            }
        }

        public void Sleep(double secs)
        {
            ThreadPool?.Sleep(TimeSpan.FromSeconds(secs));
        }

        public void IncrementScriptEventCounter()
        {
            ThreadPool?.IncrementScriptEventCounter();
        }
    }
}
