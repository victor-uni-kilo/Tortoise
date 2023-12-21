using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tortoise.Utility
{
	public static class PolygonIdentifier
	{
		public enum PolygonFragmentType
		{
			Hexagon,
			EquilateralTriangle,
			EquilateralRhomboid,
			TruncatedHexagon,
			TwinTruncatedHexagon,
			Unknown
		}

		public static bool identifyFragment(Polyline polyline, out PolygonFragmentType fragmentType)
		{
			fragmentType = PolygonFragmentType.Unknown;

			if (polyline == null || !polyline.IsClosed || polyline.SegmentCount < 3) return false;

			//Analyse the Polygon
			int polylineSideCount = polyline.SegmentCount;
			double initSideLength = polyline.SegmentAt(0).Length;
			bool hasEqualSides = true;

			for (int i = 1; i < polylineSideCount; i++)
			{
				double currentSideLength = polyline.SegmentAt(i).Length;

				if (!RhinoMath.EpsilonEquals(initSideLength, currentSideLength, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
				{
					hasEqualSides = false;
					break;
				}
			}

			switch (polylineSideCount)
			{
				case 6 when hasEqualSides:
					fragmentType = PolygonFragmentType.Hexagon;
					return true;
				case 6 when !hasEqualSides:
					fragmentType = PolygonFragmentType.TwinTruncatedHexagon;
					return true;
				case 4 when hasEqualSides:
					fragmentType = PolygonFragmentType.EquilateralRhomboid;
					return true;
				case 4 when !hasEqualSides:
					fragmentType = PolygonFragmentType.TruncatedHexagon;
					return true;
				case 3 when hasEqualSides:
					fragmentType = PolygonFragmentType.EquilateralTriangle;
					return true;
				default:
					fragmentType = PolygonFragmentType.Unknown;
					return true;
			}
		}
	}
}
