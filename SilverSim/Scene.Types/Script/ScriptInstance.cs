// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

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
        public event Action<ScriptInstance> OnStateChange;
        public event Action<ScriptInstance> OnScriptReset;
        public abstract IScriptState ScriptState { get; }

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
                foreach (Action<ScriptInstance> del in ev.GetInvocationList())
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
                foreach (Action<ScriptInstance> del in ev.GetInvocationList())
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
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
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
            ObjectPart part = Part;
            ObjectGroup objgroup = part.ObjectGroup;
            ObjectPart rootPart = objgroup.RootPart;
            UUI creator = rootPart.Creator;
            UUI owner = objgroup.Owner;

            if (OSSLPermissions.TryGetValue(name, out perms))
            {
                if (perms.Creators.Contains(creator))
                {
                    return;
                }
                if (perms.Owners.Contains(owner))
                {
                    return;
                }
                /* TODO: implement parcel rights */

                if (perms.IsAllowedForEstateOwner &&
                    objgroup.Scene.Owner == owner)
                {
                    return;
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
