﻿// SilverSim is distributed under the terms of the
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

using log4net;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    public abstract class ServerParamServiceInterface
    {
        public readonly RwLockedDictionaryAutoAdd<string, RwLockedList<IServerParamAnyListener>> GenericServerParamListeners = new RwLockedDictionaryAutoAdd<string, RwLockedList<IServerParamAnyListener>>(() => new RwLockedList<IServerParamAnyListener>());
        public readonly RwLockedList<IServerParamAnyListener> AnyServerParamListeners = new RwLockedList<IServerParamAnyListener>();
        public readonly RwLockedDictionaryAutoAdd<string, RwLockedList<Action<UUID, string>>> SpecificParamListeners = new RwLockedDictionaryAutoAdd<string, RwLockedList<Action<UUID, string>>>(() => new RwLockedList<Action<UUID, string>>());
        public readonly RwLockedDictionaryAutoAdd<string, RwLockedList<IServerParamAnyListener>> StartsWithServerParamListeners = new RwLockedDictionaryAutoAdd<string, RwLockedList<IServerParamAnyListener>>(() => new RwLockedList<IServerParamAnyListener>());

        private static readonly ILog m_ServerParamUpdateLog = LogManager.GetLogger("SERVER PARAM UPDATE");

        public string this[UUID regionID, string parameter, string defvalue]
        {
            get
            {
                string value;
                return (TryGetValue(regionID, parameter, out value)) ?
                    value :
                    defvalue;
            }
        }

        private readonly object m_UpdateSequenceLock = new object();

        public string this[UUID regionID, string parameter]
        {
            get
            {
                string value;
                if(!TryGetValue(regionID, parameter, out value))
                {
                    throw new KeyNotFoundException();
                }
                return value;
            }
            set
            {
                lock(m_UpdateSequenceLock)
                {
                    Store(regionID, parameter, value);
                    RwLockedList<IServerParamAnyListener> listenerList;
                    RwLockedList<Action<UUID, string>> specificList;
                    if(SpecificParamListeners.TryGetValue(parameter, out specificList))
                    {
                        foreach(var del in specificList)
                        {
#if DEBUG
                            m_ServerParamUpdateLog.DebugFormat("sending update to {0} with parameter {1}/{2}", del.GetType().FullName, regionID.ToString(), parameter);
#endif
                            try
                            {
                                del(regionID, value);
                            }
                            catch(Exception e)
                            {
                                m_ServerParamUpdateLog.WarnFormat("Failed to update {0} with parameter {1}/{2}: {3}: {4}\n{5}", del.GetType().FullName, regionID.ToString(), parameter, e.GetType().FullName, e.Message, e.StackTrace);
                            }
                        }
                    }
                    if (GenericServerParamListeners.TryGetValue(parameter, out listenerList))
                    {
                        foreach (var listener in listenerList)
                        {
#if DEBUG
                            m_ServerParamUpdateLog.DebugFormat("sending update to {0} with parameter {1}/{2}", listener.GetType().FullName, regionID.ToString(), parameter);
#endif
                            try
                            {
                                listener.TriggerParameterUpdated(regionID, parameter, value);
                            }
                            catch (Exception e)
                            {
                                m_ServerParamUpdateLog.WarnFormat("Failed to update {0} with parameter {1}/{2}: {3}: {4}\n{5}", listener.GetType().FullName, regionID.ToString(), parameter, e.GetType().FullName, e.Message, e.StackTrace);
                            }
                        }
                    }
                    foreach(var listener in AnyServerParamListeners)
                    {
#if DEBUG
                        m_ServerParamUpdateLog.DebugFormat("sending update to {0} with parameter {1}/{2}", listener.GetType().FullName, regionID.ToString(), parameter);
#endif
                        try
                        {
                            listener.TriggerParameterUpdated(regionID, parameter, value);
                        }
                        catch (Exception e)
                        {
                            m_ServerParamUpdateLog.WarnFormat("Failed to update {0} with parameter {1}/{2}: {3}: {4}\n{5}", listener.GetType().FullName, regionID.ToString(), parameter, e.GetType().FullName, e.Message, e.StackTrace);
                        }
                    }

                    foreach(var kvp in StartsWithServerParamListeners)
                    {
                        if(parameter.StartsWith(kvp.Key))
                        {
                            foreach (var listener in kvp.Value)
                            {
#if DEBUG
                                m_ServerParamUpdateLog.DebugFormat("sending update to {0} with parameter {1}/{2}", listener.GetType().FullName, regionID.ToString(), parameter);
#endif
                                try
                                {
                                    listener.TriggerParameterUpdated(regionID, parameter, value);
                                }
                                catch (Exception e)
                                {
                                    m_ServerParamUpdateLog.WarnFormat("Failed to update {0} with parameter {1}/{2}: {3}: {4}\n{5}", listener.GetType().FullName, regionID.ToString(), parameter, e.GetType().FullName, e.Message, e.StackTrace);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected abstract void Store(UUID regionID, string parameter, string value);

        public abstract bool TryGetValue(UUID regionID, string parameter, out string value);
        public abstract bool Contains(UUID regionID, string parameter);
        public abstract bool TryGetExplicitValue(UUID regionID, string parameter, out string value);

        public abstract List<string> this[UUID regionID] { get; }

        /* specific for use by configuration loader */
        public abstract List<KeyValuePair<UUID, string>> this[string parametername] { get; }

        public abstract List<KeyValuePair<UUID, string>> KnownParameters { get; }

        public abstract bool Remove(UUID regionID, string parameter);

        public bool GetBoolean(UUID regionID, string parameter) => bool.Parse(this[regionID, parameter]);

        public bool GetBoolean(UUID regionID, string parameter, bool defvalue) => bool.Parse(this[regionID, parameter, defvalue.ToString()]);

        public string GetString(UUID regionID, string parameter) => this[regionID, parameter];

        public string GetString(UUID regionID, string parameter, string defvalue) => this[regionID, parameter, defvalue];

        public int GetInteger(UUID regionID, string parameter) => int.Parse(this[regionID, parameter]);

        public int GetInteger(UUID regionID, string parameter, int defvalue) => int.Parse(this[regionID, parameter, defvalue.ToString()]);

        public float GetFloat(UUID regionID, string parameter) => float.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);

        public float GetFloat(UUID regionID, string parameter, float defvalue) => float.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);

        public double GetDouble(UUID regionID, string parameter) => double.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);

        public double GetDouble(UUID regionID, string parameter, double defvalue) => double.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);

        public Vector3 GetVector(UUID regionID, string parameter) => Vector3.Parse(this[regionID, parameter]);

        public Vector3 GetVector(UUID regionID, string parameter, Vector3 v) => Vector3.Parse(this[regionID, parameter, v.ToString()]);

        public UUID GetUUID(UUID regionID, string parameter) => UUID.Parse(this[regionID, parameter]);

        public UUID GetUUID(UUID regionID, string parameter, UUID defvalue) => UUID.Parse(this[regionID, parameter, defvalue.ToString()]);

        public Quaternion GetQuaternion(UUID regionID, string parameter) => Quaternion.Parse(this[regionID, parameter]);

        public Quaternion GetQuaternion(UUID regionID, string parameter, Quaternion defvalue) => Quaternion.Parse(this[regionID, parameter, defvalue.ToString()]);
    }
}
