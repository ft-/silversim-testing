/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Script
{
    public abstract class ScriptInstance : MarshalByRefObject
    {
        public abstract void PostEvent(IScriptEvent e);
        public abstract bool IsRunning { get; set; }
        public bool IsAborting { get; private set; }
        /* Remove and Dispose must deregister all possible handles */
        public abstract void Remove();
        public abstract void Reset();

        public abstract void ProcessEvent();
        public abstract void ShoutError(string msg);
        public abstract bool HasEventsPending { get; }
        public IScriptWorkerThreadPool ThreadPool { get; set; }
        public delegate void StateChangeEventDelegate(ScriptInstance si);
        public delegate void ScriptResetEventDelegate(ScriptInstance si);
        public event StateChangeEventDelegate OnStateChange;
        public event ScriptResetEventDelegate OnScriptReset;

        public abstract ObjectPartInventoryItem Item { get; }

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

        public abstract void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions);

        protected void TriggerOnStateChange()
        {
            var ev = OnStateChange; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                foreach (StateChangeEventDelegate del in ev.GetInvocationList())
                {
                    try
                    {
                        del(this);
                    }
                    catch
                    {
                    }
                }
            }
        }

        protected void TriggerOnScriptReset()
        {
            var ev = OnScriptReset; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                foreach (ScriptResetEventDelegate del in ev.GetInvocationList())
                {
                    try
                    {
                        del(this);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public class Permissions
        {
            public RwLockedList<UUI> Creators = new RwLockedList<UUI>();
            public RwLockedList<UUI> Owners = new RwLockedList<UUI>();
            public bool IsAllowedForParcelOwner;
            public bool IsAllowedForParcelMember;
            public bool IsAllowedForEstateOwner;
            public bool IsAllowedForEstateManager;

            public Permissions()
            {

            }
        }

        #region Threat Level System
        public enum ThreatLevelType : uint
        {
            None = 0,
            Nuisance = 1,
            VeryLow = 2,
            Low = 3,
            Moderate = 4,
            High = 5,
            VeryHigh = 6,
            Severe = 7
        }

        public ThreatLevelType ThreatLevel { get; protected set; }

        public static readonly RwLockedDictionary<string, Permissions> OSSLPermissions = new RwLockedDictionary<string, Permissions>();

        public void CheckThreatLevel(string name, ThreatLevelType level)
        {
            if ((int)level >= (int)ThreatLevel)
            {
                return;
            }

            Permissions perms;
            if (OSSLPermissions.TryGetValue(name, out perms))
            {
                if (perms.Creators.Contains(Part.ObjectGroup.RootPart.Creator))
                {
                    return;
                }
                if (perms.Owners.Contains(Part.ObjectGroup.Owner))
                {
                    return;
                }
                /* TODO: implement parcel rights */

                if (perms.IsAllowedForEstateOwner)
                {
                    if (Part.ObjectGroup.Scene.Owner == Part.ObjectGroup.Owner)
                    {
                        return;
                    }
                }

                if (perms.IsAllowedForEstateManager)
                {
                    /* TODO: implement estate managers */
                }
            }
            throw new Exception(string.Format("Function {0} not allowed", name));
        }
        #endregion
    }
}
