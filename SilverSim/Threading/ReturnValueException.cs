// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Threading
{
    [Serializable]
    public class ReturnValueException<TValue> : Exception
    {
        [NonSerialized]
        private TValue m_Value;

        public TValue Value
        {
            get { return m_Value; }
        }

        public ReturnValueException(TValue value)
        {
            m_Value = value;
        }
    }
}
