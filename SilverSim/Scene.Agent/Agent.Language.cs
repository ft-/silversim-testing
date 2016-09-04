// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Globalization;

namespace SilverSim.Scene.Agent
{
    partial class Agent
    {
        #region AgentLanguage
        string m_AgentLanguage = string.Empty;
        CultureInfo m_AgentCultureInfo;
        readonly object m_AgentLanguageLock = new object();

        public string AgentLanguage
        {
            get
            {
                lock (m_AgentLanguageLock)
                {
                    return m_AgentLanguage;
                }
            }

            set
            {
                lock (m_AgentLanguageLock)
                {
                    m_AgentLanguage = value;
                    try
                    {
                        m_AgentCultureInfo = new CultureInfo(value);
                    }
                    catch
                    {
                        m_AgentCultureInfo = EnUsCulture;
                    }
                }
            }
        }

        static readonly CultureInfo EnUsCulture = new CultureInfo("en-US");
        public CultureInfo CurrentCulture
        {
            get
            {
                lock (m_AgentLanguageLock)
                {
                    return m_AgentCultureInfo ?? EnUsCulture;
                }
            }
        }
        #endregion
    }
}
