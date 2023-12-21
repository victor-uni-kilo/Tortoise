using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;

namespace Tortoise.Subdivision
{
	public static class HexagonShape
	{
		public static List<GH_Curve> apothemSubdivision(Polyline parentHexagon, int hexAlongApothemCount)
		{

			GH_Curve parentHexagonGHCurve = new GH_Curve(parentHexagon.ToNurbsCurve());
			List<GH_Curve> subdividedHexFragments = new List<GH_Curve>();

			if (hexAlongApothemCount == 0)
			{
				subdividedHexFragments.Add(parentHexagonGHCurve);
			}
			else
			{
				Point3d parentHexagonCentroid = new Point3d(parentHexagon.CenterPoint());
				double inscribedDistance = (parentHexagon.SegmentAt(0).Length * Math.Sqrt(3)) / 2;

				int apothemSubdivisionCount = hexAlongApothemCount + 1;

				double hexApothem = inscribedDistance / (hexAlongApothemCount + 0.5);
				double hexRadius = hexApothem / Math.Sqrt(3);

				//// FIND THE CENTROIDS & DRAW THE HEXES
				for (int i = 0; i <= apothemSubdivisionCount; i++)
				{
					if (i == 0)
					{
						GH_Curve newCentralHex = new GH_Curve(Draw.Fragment.Hexagon(parentHexagonCentroid, hexRadius));
						subdividedHexFragments.Add(newCentralHex);

					}
					else
					{
						Point3d previousPoint = Point3d.Unset;

						for (int segmentIndex = 0; segmentIndex <= parentHexagon.SegmentCount; segmentIndex++)
						{
							Line parentHexSegment;

							if (segmentIndex == parentHexagon.SegmentCount)
							{
								parentHexSegment = parentHexagon.SegmentAt(0);
							}
							else
							{
								parentHexSegment = parentHexagon.SegmentAt(segmentIndex);
							}

							Point3d parentSegmentMidpoint = parentHexSegment.PointAt(0.5);

							// Create a Vector3d for translation
							Vector3d translationVector = parentHexagonCentroid - parentSegmentMidpoint;
							translationVector.Unitize();

							double amplitude = hexApothem * i;

							Point3d smallHexCentroid = new Point3d(parentHexagonCentroid);
							smallHexCentroid.Transform(Transform.Translation(translationVector * amplitude));

							if (segmentIndex != 0)
							{
								LineCurve interpolationHelperCurve = new LineCurve(previousPoint, smallHexCentroid);
								double[] tParametars = interpolationHelperCurve.DivideByCount(i, true);

								if (tParametars.Length > 0)
								{
									foreach (double param in tParametars)
									{
										Point3d interPoint = interpolationHelperCurve.PointAt(param);

										// Check if a point is within parentHex
										if (Utility.PointInsideRegionValidator.isPointInsideCurve(interPoint, parentHexagonGHCurve))
										{
											//childHexagonsCentroid.Add(interPoint);
											GH_Curve newHex = new GH_Curve(Draw.Fragment.Hexagon(interPoint, hexRadius));
											subdividedHexFragments.Add(newHex);
										}

									}

								}
							}

							previousPoint = smallHexCentroid;

						}

					}

				}
				// TRIGGER A FUCNTION TO FIND REMAINIG SHAPES - NEEDS CURVES
				List<GH_Curve> remainingFragments = Helper.findRemainingFragments(subdividedHexFragments, parentHexagonGHCurve);
				foreach (GH_Curve fragment in remainingFragments) {
					subdividedHexFragments.Add(fragment);
				}

			}

			return subdividedHexFragments;

		}
	}
}
