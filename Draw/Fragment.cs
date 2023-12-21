using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tortoise.Draw
{
	public static class Fragment
	{
		public static Curve Hexagon(Point3d centerPoint, double radius)
		{
			Polyline hexagon = new Polyline();

			for (int i = 0; i < 6; i++)
			{
				double angle = 2.0 * Math.PI * i / 6.0;
				double x = centerPoint.X + radius * Math.Cos(angle);
				double y = centerPoint.Y + radius * Math.Sin(angle);
				double z = centerPoint.Z; // Assuming the hexagon lies in the XY plane

				hexagon.Add(new Point3d(x, y, z));
			}

			hexagon.Add(hexagon[0]);

			return hexagon.ToNurbsCurve();
		}
	}
}
