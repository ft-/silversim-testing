// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Script
{
    public abstract class ScriptInstance
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

        public void Sleep(double secs)
        {
            Thread.Sleep(TimeSpan.FromSeconds(secs));
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
            throw new InvalidOperationException(string.Format("Function {0} not allowed", name));
        }
        #endregion
    }
}
