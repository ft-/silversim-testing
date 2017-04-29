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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SilverSim.Scene.Types.Script
{
    public class LocalizedScriptMessage : IListenEventLocalization
    {
        readonly object m_NlsRefObject;
        readonly string m_NlsId;
        readonly object[] m_Param;
        readonly string m_NlsDefMessage;

        public LocalizedScriptMessage(object nlsRefObject, string nlsId, string nlsDefMessage, params object[] param)
        {
            m_NlsRefObject = nlsRefObject;
            m_NlsId = nlsId;
            m_NlsDefMessage = nlsDefMessage;
            m_Param = param;
        }

        public string Localize(ListenEvent le, CultureInfo currentCulture)
        {
            return string.Format(m_NlsRefObject.GetLanguageString(currentCulture, m_NlsId, m_NlsDefMessage), m_Param);
        }
    }

    public abstract class ScriptInstance
    {
        public abstract void PostEvent(IScriptEvent e);
        public abstract bool IsRunning { get; set; }
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
        public IScriptWorkerThreadPool ThreadPool { get; set; }
        public event Action<ScriptInstance> OnStateChange;
        public event Action<ScriptInstance> OnScriptReset;
        public abstract IScriptState ScriptState { get; }
        public virtual bool HasTouchEvent { get { return false; } }
        public virtual bool HasMoneyEvent { get { return false; } }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract ObjectPartInventoryItem Item { get; }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract ObjectPart Part { get; }

        public abstract double ExecutionTime { get; set; }

        public ScriptInstance()
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
            IScriptWorkerThreadPool pool = ThreadPool;
            if (null != pool)
            {
                pool.AbortScript(this);
            }
        }

        public abstract bool IsLinkMessageReceiver { get; }

        public abstract void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions);

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        protected void TriggerOnStateChange()
        {
            var ev = OnStateChange; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                foreach (Action<ScriptInstance> del in ev.GetInvocationList().OfType<Action<ScriptInstance>>())
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
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        protected void TriggerOnScriptReset()
        {
            var ev = OnScriptReset; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                foreach (Action<ScriptInstance> del in ev.GetInvocationList().OfType<Action<ScriptInstance>>())
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
        }

        public void Sleep(double secs)
        {
            Thread.Sleep(TimeSpan.FromSeconds(secs));
        }
    }
}
