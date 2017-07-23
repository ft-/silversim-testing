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

using System.Drawing.Imaging;

namespace SilverSim.Scene.Types.Agent
{
    public static partial class AgentBakeAppearance
    {
        public static void ApplyTint(this ColorMatrix mat, SilverSim.Types.Color col)
        {
            mat.Matrix00 = (float)col.R;
            mat.Matrix11 = (float)col.G;
            mat.Matrix22 = (float)col.B;
            mat.Matrix33 = 1;
            mat.Matrix44 = 1;
        }

        public static System.Drawing.Color ToDrawing(this SilverSim.Types.Color color)
        {
            return System.Drawing.Color.FromArgb(color.R_AsByte, color.G_AsByte, color.B_AsByte);
        }

        public static System.Drawing.Color ToDrawing(this SilverSim.Types.ColorAlpha color)
        {
            return System.Drawing.Color.FromArgb(color.A_AsByte, color.R_AsByte, color.G_AsByte, color.B_AsByte);
        }

        public static System.Drawing.Color ToDrawingWithNewAlpha(this SilverSim.Types.Color color, double newalpha)
        {
            return System.Drawing.Color.FromArgb((int)(newalpha * 255), color.R_AsByte, color.G_AsByte, color.B_AsByte);
        }
    }
}
