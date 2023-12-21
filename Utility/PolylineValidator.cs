using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Tortoise.Utility
{
	public static class PolylineValidator
	{
		public static bool curveIsPolyline(GH_Curve ghCurve, bool checkIfClosed = false)
		{
			if (ghCurve == null || ghCurve.Value == null)
				return false;

			Curve curve = ghCurve.Value;

			bool isPolyline = curve.IsPolyline();

			if (checkIfClosed == true)
			{
				return isPolyline && curve.IsClosed;
			}
			else 
			{
				return isPolyline;
			}
			
		}

		public static bool curvesArePolylines(List<GH_Curve> curveList, bool checkIfClosed = false)
		{
			if (curveList == null || curveList.Count == 0)
				return false;

			foreach (var ghCurve in curveList)
			{
				if (!curveIsPolyline(ghCurve, checkIfClosed))
					return false;
			}

			return true;
		}

		public static bool curvesArePolylinesTree(GH_Structure<GH_Curve> curvesTree, bool checkIfClosed = false)
		{
			if (curvesTree == null || curvesTree.IsEmpty)
				return false;

			foreach (var branch in curvesTree.Branches)
			{
				if (!curvesArePolylines(branch, checkIfClosed))
					return false;
			}

			return true;
		}
	}
}
