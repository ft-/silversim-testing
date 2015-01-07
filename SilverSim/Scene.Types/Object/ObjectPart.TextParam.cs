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

using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public class TextParam
        {
            #region Constructor
            public TextParam()
            {

            }
            #endregion

            #region Fields
            public string Text = string.Empty;
            public ColorAlpha TextColor = new ColorAlpha(0, 0, 0, 0);
            #endregion
        }
        private readonly TextParam m_Text = new TextParam();


        public TextParam Text
        {
            get
            {
                TextParam res = new TextParam();
                lock (m_Text)
                {
                    res.Text = m_Text.Text;
                    res.TextColor = new ColorAlpha(m_Text.TextColor);
                }
                return res;
            }
            set
            {
                lock (m_Text)
                {
                    m_Text.Text = value.Text;
                    m_Text.TextColor = new ColorAlpha(value.TextColor);
                }
                UpdateExtraParams();
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }
    }
}
