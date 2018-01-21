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

using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Object.Parameters
{
    public sealed class TextParam
    {
        #region Fields
        public string Text = string.Empty;
        public ColorAlpha TextColor = new ColorAlpha(0, 0, 0, 0);
        #endregion

        public TextParam()
        {
        }

        public TextParam(TextParam src)
        {
            Text = src.Text;
            TextColor = src.TextColor;
        }

        public byte[] Serialization
        {
            get
            {
                byte[] textBytes = Text.ToUTF8Bytes();
                var serialization = new byte[4 + textBytes.Length];
                Buffer.BlockCopy(textBytes, 0, serialization, 4, textBytes.Length);
                serialization[0] = TextColor.R_AsByte;
                serialization[1] = TextColor.G_AsByte;
                serialization[2] = TextColor.B_AsByte;
                serialization[3] = TextColor.A_AsByte;
                return serialization;
            }
            set
            {
                if (value.Length < 4)
                {
                    throw new ArgumentException("array length must be at least 4.");
                }
                TextColor.R_AsByte = value[0];
                TextColor.G_AsByte = value[1];
                TextColor.B_AsByte = value[2];
                TextColor.A_AsByte = value[3];
                Text = value.Length > 4 ?
                    value.FromUTF8Bytes(4, value.Length - 4) :
                    string.Empty;
            }
        }
    }
}
