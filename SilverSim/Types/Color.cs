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

namespace SilverSim.Types
{
    public class Color
    {
        public double R = 0f;
        public double G = 0f;
        public double B = 0f;

        #region Constructors
        public Color()
        {

        }

        public Color(double r, double g, double b)
        {
            if (r < 0f) R = 0f;
            else if (r > 1f) R = 1f;
            else R = r;

            if (g < 0f) G = 0f;
            else if (g > 1f) G = 1f;
            else G = g;

            if (b < 0f) B = 0f;
            else if (b > 1f) B = 1f;
            else B = b;
        }

        public Color(Vector3 v)
        {
            if (v.X < 0f) R = 0f;
            else if (v.X > 1f) R = 1f;
            else R = v.X;

            if (v.Y < 0f) G = 0f;
            else if (v.Y > 1f) G = 1f;
            else G = v.Y;

            if (v.Z < 0f) B = 0f;
            else if (v.Z > 1f) B = 1f;
            else B = v.Z;
        }

        public Color(Color v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
        }
        #endregion

        #region Operators
        public static implicit operator Vector3(Color v)
        {
            return new Vector3(v.R, v.G, v.B);
        }
        #endregion

        #region Properties
        public Vector3 AsVector3
        {
            get
            {
                return new Vector3(R, G, B);
            }
        }
        #endregion
    }

    public class ColorAlpha : Color
    {
        public double A = 1f;

        #region Constructors
        public ColorAlpha()
        {
            
        }

        public ColorAlpha(double r, double g, double b, double alpha)
            : base(r, g, b)
        {
            if (alpha < 0f) A = 0f;
            else if (alpha > 1f) A = 1f;
            else A = alpha;
        }

        public ColorAlpha(Vector3 v, double alpha)
            : base(v)
        {
            if (alpha < 0f) A = 0f;
            else if (alpha > 1f) A = 1f;
            else A = alpha;
        }

        public ColorAlpha(Color v, double alpha)
            : base(v)
        {
            if (alpha < 0f) A = 0f;
            else if (alpha > 1f) A = 1f;
            else A = alpha;
        }

        public ColorAlpha(ColorAlpha v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
            A = v.A;
        }
        #endregion
    }
}
