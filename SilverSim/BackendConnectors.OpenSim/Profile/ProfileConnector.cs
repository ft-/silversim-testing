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

using SilverSim.ServiceInterfaces.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.BackendConnectors.OpenSim.Profile
{
    public partial class ProfileConnector : ProfileServiceInterface
    {
        internal IClassifiedsInterface m_Classifieds;
        internal IPicksInterface m_Picks;
        internal INotesInterface m_Notes;
        internal IUserPreferencesInterface m_Preferences;
        internal IPropertiesInterface m_Properties;

        public override ProfileServiceInterface.IClassifiedsInterface Classifieds
        {
            get { throw new NotImplementedException(); }
        }

        public override ProfileServiceInterface.IPicksInterface Picks
        {
            get { throw new NotImplementedException(); }
        }

        public override ProfileServiceInterface.INotesInterface Notes
        {
            get { throw new NotImplementedException(); }
        }

        public override ProfileServiceInterface.IUserPreferencesInterface Preferences
        {
            get { throw new NotImplementedException(); }
        }

        public override ProfileServiceInterface.IPropertiesInterface Properties
        {
            get { throw new NotImplementedException(); }
        }
    }
}
