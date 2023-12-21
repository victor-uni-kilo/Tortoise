using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Tortoise.Utility
{
	public static class ClosedCurveValidator
	{
		public static bool isClosedCurve(GH_Curve ghCurve)
		{
			if (ghCurve == null || ghCurve.Value == null)
				return false;

			Curve curve = ghCurve.Value;
			return curve.IsClosed;
		}

		public static bool areClosedCurves(List<GH_Curve> curveList)
		{
			if (curveList == null || curveList.Count == 0)
				return false;

			foreach (var ghCurve in curveList)
			{
				if (!isClosedCurve(ghCurve))
					return false;
			}

			return true;
		}

		public static bool areClosedCurvesTree(GH_Structure<GH_Curve> curvesTree)
		{
			if (curvesTree == null || curvesTree.IsEmpty)
				return false;

			foreach (var branch in curvesTree.Branches)
			{
				if (!areClosedCurves(branch))
					return false;
			}

			return true;
		}
	}
}
