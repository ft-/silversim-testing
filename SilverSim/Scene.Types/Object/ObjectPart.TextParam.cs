// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
    }
}
