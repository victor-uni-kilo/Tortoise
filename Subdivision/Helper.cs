using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tortoise.Utility;
using static Tortoise.Utility.PolygonIdentifier;

namespace Tortoise.Subdivision
{
	public static class Helper
	{
		public static List<GH_Curve> findRemainingFragments(List<GH_Curve> childShapesAsGHCurves, GH_Curve parentShapeGHCurve)
		{
			// OUTPUT
			List<GH_Curve> remainingShapesAsCurves = new List<GH_Curve>();

			// UNPACK THE LIST
			List<Curve> subShapesAsCurves = new List<Curve>();

			foreach (GH_Curve ghCurve in childShapesAsGHCurves)
			{
				subShapesAsCurves.Add(ghCurve.Value);
			}

			// Union of the subShapesAsCurves list items
			Curve[] shapeUnionArray = Curve.CreateBooleanUnion(subShapesAsCurves, 0.05);

			if (shapeUnionArray != null && shapeUnionArray.Length == 1)
			{
				Curve shapeUnionCurve = shapeUnionArray[0];

				// Create boolean differences
				Curve[] shapeDifferenceCurves = Curve.CreateBooleanDifference(parentShapeGHCurve.Value, shapeUnionCurve, 0.05);

				// Add the resulting curves to subShapesAsCurves
				foreach (var curveItem in shapeDifferenceCurves)
				{

					Polyline curveItemAsPolyline;
					curveItem.TryGetPolyline(out curveItemAsPolyline);

					PolygonFragmentType fragmentType;
					PolygonIdentifier.identifyFragment(curveItemAsPolyline, out fragmentType);

					if (fragmentType == PolygonFragmentType.TwinTruncatedHexagon)
					{
						Tuple<GH_Curve, GH_Curve> splitTruncatedHexagon = splitCojoined(curveItemAsPolyline);

						remainingShapesAsCurves.Add(new GH_Curve(splitTruncatedHexagon.Item1));
						remainingShapesAsCurves.Add(new GH_Curve(splitTruncatedHexagon.Item2));

					}
					else
					{
						remainingShapesAsCurves.Add(new GH_Curve(curveItem));
					};
				}
			}
			return remainingShapesAsCurves;
		}
		
		public static Tuple<GH_Curve, GH_Curve> splitCojoined(Polyline twinTruncatedHexagon)
		{
			Polyline truncatedHexagon1 = new Polyline();
			Polyline truncatedHexagon2 = new Polyline();

			Point3d twinCentroid = twinTruncatedHexagon.CenterPoint();

			// Sort the points based on their distance to the centroid
			List<Point3d> sortedPoints = new List<Point3d>(twinTruncatedHexagon);
			sortedPoints.Sort((p1, p2) => twinCentroid.DistanceTo(p1).CompareTo(twinCentroid.DistanceTo(p2)));

			Tuple<Point3d, Point3d> closestPoints = Tuple.Create(sortedPoints[0], sortedPoints[1]);

			bool addToTruncatedHexagon1 = true;

			foreach (Point3d point in twinTruncatedHexagon)
			{
				if (point.Equals(closestPoints.Item1))
				{
					truncatedHexagon1.Add(point);
					addToTruncatedHexagon1 = false;
				}
				else if (point.Equals(closestPoints.Item2))
				{
					if (truncatedHexagon1.Count == 4 && truncatedHexagon2.Count == 4)
					break;
					truncatedHexagon2.Add(point);
					addToTruncatedHexagon1 = true;
				}

				if (addToTruncatedHexagon1 && truncatedHexagon1.Count < 4)
				{
					truncatedHexagon1.Add(point);
				}
				else if (!addToTruncatedHexagon1 && truncatedHexagon2.Count < 4)
				{
					truncatedHexagon2.Add(point);
				}
			}
			// Close the polygons
			truncatedHexagon1.Add(truncatedHexagon1[0]);
			truncatedHexagon2.Add(truncatedHexagon2[0]);


			Tuple <GH_Curve, GH_Curve> twoTruncatedHexagon = Tuple.Create(new GH_Curve(truncatedHexagon1.ToNurbsCurve()), new GH_Curve(truncatedHexagon2.ToNurbsCurve()));

			return twoTruncatedHexagon;
		}
	}
}
