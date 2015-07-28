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

===============================================================================
The following code segment is based on SL viewer code which is distributed under LGPL V2.1.

*/

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public class LLPath
    {
        public bool IsOpen { get; private set; }
        public float Step { get; private set; }
        public class Pt
        {
            public Quaternion Rot = Quaternion.Identity;
            public Vector3 Pos = Vector3.Zero;
            public Vector3 Scale = Vector3.One;
            public double TexT = 0;

            public Pt()
            {

            }
        }

        public readonly List<Pt> Path = new List<Pt>();

        public LLPath(ObjectPart.PrimitiveShape shape, double detail = 1f, int split = 0, bool is_sculpted = false, int sculpt_size = 0)
        {

        }

        readonly double[] TableScale = new double[] { 1, 1, 1, 0.5f, 0.707107f, 0.53f, 0.525f, 0.5f };

        void GenNGon(ObjectPart.PrimitiveShape.Decoded shape, int sides, double offset = 0f, double end_scale = 1f, double twist_scale = 1f)
        {
	        double revolutions = shape.Revolutions;
	        double skew		= shape.Skew;
	        double skew_mag	= Math.Abs(skew);
	        double hole_x		= shape.Scale.X * (1.0f - skew_mag);
	        double hole_y		= shape.Scale.Y;

	        /* Calculate taper begin/end for x,y (Negative means taper the beginning) */
	        double taper_x_begin = 1.0f;
	        double taper_x_end = 1.0f - shape.Taper.X;
	        double taper_y_begin = 1.0f;
	        double taper_y_end = 1.0f - shape.Taper.Y;

	        if ( taper_x_end > 1.0f )
	        {
		        /* Flip tapering */
		        taper_x_begin	= 2.0f - taper_x_end;
		        taper_x_end		= 1.0f;
	        }
	        if ( taper_y_end > 1.0f )
	        {
		        /* Flip tapering */
		        taper_y_begin	= 2.0f - taper_y_end;
		        taper_y_end		= 1.0f;
	        }

	        /* For spheres, the radius is usually zero. */
	        double radius_start = 0.5f;
	        if (sides < 8)
	        {
		        radius_start = TableScale[sides];
	        }

	        /* Scale the radius to take the hole size into account. */
	        radius_start *= 1.0f - hole_y;
	
	        /* Now check the radius offset to calculate the start,end radius. 
             * (Negative means decrease the start radius instead).
             */
	        double radius_end    = radius_start;
	        double radius_offset = shape.RadiusOffset;
	        if (radius_offset < 0f)
	        {
		        radius_start *= 1f + radius_offset;
	        }
	        else
	        {
		        radius_end   *= 1f - radius_offset;
	        }	

	        // Is the path NOT a closed loop?
	        IsOpen = ( (shape.PathEnd*end_scale - shape.PathBegin < 1.0f) ||
		      (skew_mag > 0.001f) ||
			  (Math.Abs(taper_x_end - taper_x_begin) > 0.001f) ||
			  (Math.Abs(taper_y_end - taper_y_begin) > 0.001f) ||
			  (Math.Abs(radius_end - radius_start) > 0.001f) );

	        double ang, c, s;
	        Quaternion twist, qang;
	        Pt pt = new Pt();
	        Vector3 path_axis = Vector3.UnitX;
        	double twist_begin = shape.TwistBegin * twist_scale;
	        double twist_end = shape.TwistEnd * twist_scale;

	        /* We run through this once before the main loop, to make sure
	         * the path begins at the correct cut.
             */
	        double step= 1.0f / sides;
	        double t = shape.PathBegin;
	        ang = 2.0f*Math.PI*revolutions * t;
	        s = Math.Sin(ang)*radius_start.Lerp(radius_end, t);
            c = Math.Cos(ang) * radius_start.Lerp(radius_end, t);


	        pt.Pos = new Vector3(
                0 + ((double)0f).Lerp(shape.Shear.X, s) + (-skew).Lerp(skew, t) * 0.5f,
			    c + ((double)0f).Lerp(shape.Shear.Y, s), 
				s);
	        pt.Scale = new Vector3(
                hole_x * taper_x_begin.Lerp(taper_x_end, t),
		        hole_y * taper_y_begin.Lerp(taper_y_end, t),
		        0);
	        pt.TexT  = t;

	        /* Twist rotates the path along the x,y plane */
        	twist = new Quaternion(twist_begin.Lerp(twist_end,t) * 2f * Math.PI - Math.PI,0,0,1);
	        /* Rotate the point around the circle's center. */
	        qang = new Quaternion(path_axis, ang);

	        pt.Rot = twist * qang;

            Path.Add(pt);

	        t += step;

	        /* Snap to a quantized parameter, so that cut does not
	         * affect most sample points.
             */
	        t = ((int)(t * sides)) / (double)sides;

	        /* Run through the non-cut dependent points. */
	        while (t < shape.PathEnd)
	        {
                pt = new Pt();

		        ang = 2.0f*Math.PI*revolutions * t;
		        c   = Math.Cos(ang)*radius_start.Lerp(radius_end, t);
		        s   = Math.Sin(ang)*radius_start.Lerp(radius_end, t);

		        pt.Pos = new Vector3(
                    0 + ((double)0f).Lerp(shape.Shear.X, s) + (-skew).Lerp(skew, t) * 0.5f,
                    c + ((double)0f).Lerp(shape.Shear.Y, s), 
                    s);

		        pt.Scale = new Vector3(
                    hole_x * taper_x_begin.Lerp(taper_x_end, t),
                    hole_y * taper_y_begin.Lerp(taper_y_end, t),
                    0);
		        pt.TexT  = t;

		        /* Twist rotates the path along the x,y plane */
		        twist = new Quaternion(twist_begin.Lerp(twist_end,t) * 2f * Math.PI - Math.PI,0,0,1);
		        // Rotate the point around the circle's center.
		        qang = new Quaternion(path_axis, ang);
		        pt.Rot = twist * qang;

                Path.Add(pt);

		        t+=step;
	        }

	        // Make one final pass for the end cut.
	        t = shape.PathEnd;
	        pt		= new Pt();
	        ang = 2.0f*Math.PI*revolutions * t;
	        c   = Math.Cos(ang)*radius_start.Lerp(radius_end, t);
	        s   = Math.Sin(ang)*radius_start.Lerp(radius_end, t);

	        pt.Pos = new Vector3(
                0 + ((double)0f).Lerp(shape.Shear.X, s) + (-skew).Lerp(skew, t) * 0.5f,
                c + ((double)0f).Lerp(shape.Shear.Y, s), 
				s);
	        pt.Scale = new Vector3(
                hole_x * taper_x_begin.Lerp(taper_x_end, t),
                hole_y * taper_y_begin.Lerp(taper_y_end, t),
                0);
	        pt.TexT  = t;

	        /* Twist rotates the path along the x,y plane */
	        twist = new Quaternion(twist_begin.Lerp(twist_end,t) * 2f * Math.PI - Math.PI,0,0,1);
	        /* Rotate the point around the circle's center. */
	        qang = new Quaternion(path_axis, ang);
	        pt.Rot = twist * qang;
            Path.Add(pt);
        }
    }
}
