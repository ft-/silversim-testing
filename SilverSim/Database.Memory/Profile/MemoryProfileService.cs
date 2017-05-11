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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Threading;
using SilverSim.Types;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.Memory.Profile
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("MySQL Profile Backend")]
    public partial class MemoryProfileService : ProfileServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        public MemoryProfileService()
        {

        }

        public override IClassifiedsInterface Classifieds
        {
            get
            {
                return this;
            }
        }

        public override INotesInterface Notes
        {
            get
            {
                return this;
            }
        }

        public override IPicksInterface Picks
        {
            get
            {
                return this;
            }
        }

        public override IUserPreferencesInterface Preferences
        {
            get
            {
                return this;
            }
        }

        public override IPropertiesInterface Properties
        {
            get
            {
                return this;
            }
        }

        public override void Remove(UUID scopeID, UUID accountID)
        {
            m_Classifieds.Remove(accountID);
            m_Notes.Remove(accountID);
            foreach(RwLockedDictionary<UUID, string> notes in m_Notes.Values)
            {
                notes.Remove(accountID);
            }
            m_PropertiesLock.AcquireReaderLock(-1);
            try
            {
                m_Properties.Remove(accountID);
            }
            finally
            {
                m_PropertiesLock.ReleaseReaderLock();
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
    #region Factory
    [PluginName("Profile")]
    public class MemoryProfileServiceFactory : IPluginFactory
    {
        public MemoryProfileServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryProfileService();
        }
    }
    #endregion
}
