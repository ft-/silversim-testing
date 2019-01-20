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

using SilverSim.Scene.Types.Object.Parameters;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private TextParam m_Text;

        public TextParam Text
        {
            get
            {
                TextParam tp = m_Text;
                if(tp == null)
                {
                    return m_ParentInfo.Text;
                }
                else
                {
                    return new TextParam(tp);
                }
            }
            set
            {
                bool changed;
                if(value == null)
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    changed = Interlocked.Exchange(ref m_Text, null) != null;
                }
                else
                {
                    TextParam oldParam = Interlocked.Exchange(ref m_Text, new TextParam(value));
                    changed = oldParam?.IsDifferent(value) ?? true;
                }
                if (changed)
                {
                    UpdateData(UpdateDataFlags.Compressed | UpdateDataFlags.Full);
                    m_Part.TriggerOnUpdate(0);
                }
            }
        }

        public bool HasText => m_Text != null;
    }
}
