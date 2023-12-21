using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Tortoise.Utility
{
	public static class PointInsideRegionValidator
	{
		public static bool isPointInsideCurve(Point3d testPoint, GH_Curve regionCurve)
		{
			bool isInside = false;

			if (regionCurve.Value.IsClosed)
			{
				Plane regionPlane;

				if (regionCurve.Value.TryGetPlane(out regionPlane))
				{
					PointContainment containmentValue = regionCurve.Value.Contains(testPoint, regionPlane, 0.01);

					if (containmentValue == PointContainment.Inside)
					{
						isInside = true;

					}
				}
			}
				return isInside;
		}

		public static bool isPointInsideCurves(Point3d testPoint, List<GH_Curve> regionCurveList)
		{
			if (regionCurveList == null || regionCurveList.Count == 0)
				return false;

			foreach (var ghCurve in regionCurveList)
			{
				if (!isPointInsideCurve(testPoint, ghCurve))
					return false;
			}

			return true;
		}

		public static bool isPointInsideCurvesTree(Point3d testPoint, GH_Structure<GH_Curve> regionCurvesTree)
		{
			if (regionCurvesTree == null || regionCurvesTree.IsEmpty)
				return false;

			foreach (var branch in regionCurvesTree.Branches)
			{
				if (!isPointInsideCurves(testPoint,branch))
					return false;
			}

			return true;
		}
	}
}
