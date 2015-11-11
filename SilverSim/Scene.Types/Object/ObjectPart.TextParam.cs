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

            public byte[] Serialization
            {
                get
                {
                    byte[] textBytes = UTF8NoBOM.GetBytes(Text);
                    byte[] serialization = new byte[4 + textBytes.Length];
                    Buffer.BlockCopy(textBytes, 0, serialization, 4, textBytes.Length);
                    serialization[0] = TextColor.R_AsByte;
                    serialization[1] = TextColor.G_AsByte;
                    serialization[2] = TextColor.B_AsByte;
                    serialization[3] = TextColor.A_AsByte;
                    return serialization;
                }
                set
                {
                    if(value.Length < 4)
                    {
                        throw new ArgumentException("array length must be at least 4.");
                    }
                    TextColor.R_AsByte = value[0];
                    TextColor.G_AsByte = value[1];
                    TextColor.B_AsByte = value[2];
                    TextColor.A_AsByte = value[3];
                    Text = value.Length > 4 ?
                        UTF8NoBOM.GetString(value, 4, value.Length - 4) :
                        Text = string.Empty;
                }
            }
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
