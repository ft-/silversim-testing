// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    public abstract class ServerParamServiceInterface
    {
        public readonly RwLockedDictionaryAutoAdd<string, RwLockedList<IServerParamAnyListener>> GenericServerParamListeners = new RwLockedDictionaryAutoAdd<string, RwLockedList<IServerParamAnyListener>>(delegate() { return new RwLockedList<IServerParamAnyListener>(); });
        public readonly RwLockedList<IServerParamAnyListener> AnyServerParamListeners = new RwLockedList<IServerParamAnyListener>();
        public readonly RwLockedDictionaryAutoAdd<string, RwLockedList<Action<UUID, string>>> SpecificParamListeners = new RwLockedDictionaryAutoAdd<string, RwLockedList<Action<UUID, string>>>(delegate () { return new RwLockedList<Action<UUID, string>>(); });

        private static readonly ILog m_ServerParamUpdateLog = LogManager.GetLogger("SERVER PARAM UPDATE");

        public ServerParamServiceInterface()
        {

        }
        
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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

        readonly object m_UpdateSequenceLock = new object();

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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
                        foreach(Action<UUID, string> del in specificList)
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
                        foreach (IServerParamAnyListener listener in listenerList)
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
                    foreach(IServerParamAnyListener listener in AnyServerParamListeners)
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

        protected abstract void Store(UUID regionID, string parameter, string value);

        public abstract bool TryGetValue(UUID regionID, string parameter, out string value);
        public abstract bool Contains(UUID regionID, string parameter);

        public abstract List<string> this[UUID regionID]
        {
            get;
        }

        /* specific for use by configuration loader */
        public abstract List<KeyValuePair<UUID, string>> this[string parametername]
        {
            get;
        }

        public abstract bool Remove(UUID regionID, string parameter);

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public bool GetBoolean(UUID regionID, string parameter)
        {
            return bool.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public bool GetBoolean(UUID regionID, string parameter, bool defvalue)
        {
            return bool.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        public string GetString(UUID regionID, string parameter)
        {
            return this[regionID, parameter];
        }

        public string GetString(UUID regionID, string parameter, string defvalue)
        {
            return this[regionID, parameter, defvalue];
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public int GetInteger(UUID regionID, string parameter)
        {
            return int.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public int GetInteger(UUID regionID, string parameter, int defvalue)
        {
            return int.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public float GetFloat(UUID regionID, string parameter)
        {
            return float.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public float GetFloat(UUID regionID, string parameter, float defvalue)
        {
            return float.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public double GetDouble(UUID regionID, string parameter)
        {
            return double.Parse(this[regionID, parameter], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public double GetDouble(UUID regionID, string parameter, double defvalue)
        {
            return double.Parse(this[regionID, parameter, defvalue.ToString()], CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Vector3 GetVector(UUID regionID, string parameter)
        {
            return Vector3.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Vector3 GetVector(UUID regionID, string parameter, Vector3 v)
        {
            return Vector3.Parse(this[regionID, parameter, v.ToString()]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public UUID GetUUID(UUID regionID, string parameter)
        {
            return UUID.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public UUID GetUUID(UUID regionID, string parameter, UUID defvalue)
        {
            return UUID.Parse(this[regionID, parameter, defvalue.ToString()]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Quaternion GetQuaternion(UUID regionID, string parameter)
        {
            return Quaternion.Parse(this[regionID, parameter]);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Quaternion GetQuaternion(UUID regionID, string parameter, Quaternion defvalue)
        {
            return Quaternion.Parse(this[regionID, parameter, defvalue.ToString()]);
        }
    }
}
