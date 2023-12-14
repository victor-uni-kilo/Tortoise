using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Tortoise
{
	public class SubdivideHexUsingRegions : GH_Component
	{
		/// <summary>
		/// Initializes a new instance of the MyComponent1 class.
		/// </summary>
		public SubdivideHexUsingRegions()
			: base(
						"SubdivideHexUsingRegions",
						"SubHex",
						"Subdivides the hexagons into smaller ones and the remaining shapes, using region curves to select their centroids.",
						"Tortoise",
						"Subdivision"
						)
		{
		}

		/// <summary>
		/// Registers all the input parameters for this component.
		/// </summary>
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
		{
			pManager.AddCurveParameter("Hexagonal Cells", "H", "Hexagonal/Subdivided Grid Cells", GH_ParamAccess.tree);
			pManager.AddCurveParameter("Regions", "R", "Selection Regions", GH_ParamAccess.list);
		}
		/// <summary>
		/// Registers all the output parameters for this component.
		/// </summary>
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
		{
			pManager.AddCurveParameter("Subdivided Matrix", "M", "Selection Regions", GH_ParamAccess.list);
		}
		/// <summary>
		/// This is the method that actually does the work.
		/// </summary>
		/// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
		protected override void SolveInstance(IGH_DataAccess DA)
		{
			// INPUT WRAPPERS
			GH_Structure<GH_Curve> hexCellsInputTree = new GH_Structure<GH_Curve>();
			List<GH_Curve> regionCurvesInputList = new List<GH_Curve>();

			// OUTPUT WRAPPERS
			GH_Structure<GH_Curve> hexCellsOutputTree = new GH_Structure<GH_Curve>();

			// TYPE CASTING INPUT
			DataTree<Polyline> hexCells = null;
			List<Curve> regionCurves = null;

			// TYPE CASTING OUTPUT
			DataTree<Polyline> subdividedHexes = null;

			// DATA Validation
			if (!DA.GetDataTree<GH_Curve>(0, out hexCellsInputTree))
			{
				AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input H requires hexagon(s) of type Polyline");
				return;
			}

			if (!DA.GetDataList<GH_Curve>(1, regionCurvesInputList))
			{
				AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input R requires a selector curve of type Curve");
				return;
			}

			hexCells = new DataTree<Polyline>((IEnumerable<Polyline>)hexCellsInputTree);
			regionCurves = new List<Curve>((IEnumerable<Curve>)regionCurvesInputList);

			//bool isPolyline = gotHexCells is Polyline;

			if (hexCells.BranchCount > 0)
			{
				//RUN SPECIAL FUNCTION
				ProcessTreeBranch(hexCells, regionCurves, ref subdividedHexes);
				DA.SetDataTree(0, subdividedHexes);
			}
			else
			{
				AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong");
			}
		}

		/// <summary>
		/// Provides an Icon for the component.
		/// </summary>
		protected override System.Drawing.Bitmap Icon
		{
			get
			{
				//You can add image files to your project resources and access them like this:
				// return Resources.IconForThisComponent;
				return null;
			}
		}

		/// <summary>
		/// Gets the unique ID for this component. Do not change this ID after release.
		/// </summary>
		public override Guid ComponentGuid
		{
			get { return new Guid("240309A2-6B84-46FB-8AE1-75E31BF03C38"); }
		}

		// MAIN FUNCTION
		private void ProcessTreeBranch(DataTree<Polyline> traversalTree, List<Curve> regionCurves, ref DataTree<Polyline> subdividedHexes)
		{
			// ******* ITERATE OVER BRANCHES *******
			for (int iBranch = 0; iBranch < traversalTree.BranchCount; iBranch++)
			{
				GH_Path currentPath = traversalTree.Path(iBranch);
				List<Polyline> currentBranch = traversalTree.Branch(currentPath);

				// ******* ITERATE OVER A LIST (OR ITEM) OF A BRANCH *******
				for (int i = 0; i < currentBranch.Count; i++)
				{
					// Create SubPath {.;.;... + new}
					GH_Path subPath = new GH_Path(currentPath).AppendElement(i);

					// Create a new path by appending the current index
					Polyline currentPolyline = currentBranch[i];

					// WORK ONLY IF THE THERE ARE REGION CURVES AND IF POLYLINE IS HEXAGON
					if (regionCurves != null && regionCurves.Count > 0)
					{
						// DEFINE THE POLYLINE (segmentOperations) and POLYLINECURVE (booleanOperations)
						PolylineCurve cellPolylineCurve = currentPolyline.ToPolylineCurve();

						// FIND THE CENTROID
						Point3d centroidPoint = currentPolyline.CenterPoint();

						//Measure the a = r and get h = a*sqrt(3)/2
						double hex_a = currentPolyline.SegmentAt(0).Length;
						double hex_h = hex_a * Math.Sqrt(3) / 2;
						double newSideLength = hex_a / 3;

						// FILTER OUT THE SELECTED HEXES (byRegionCurves)
						if (IsPointInsideCurves(centroidPoint, regionCurves))
						{
							// PREPARE CURVES FOR NECESSARY CLASS METHODS
							List<Curve> subShapesAsCurves = new List<Curve>();
							List<Polyline> subShapesAsPolylines = new List<Polyline>();

							switch (currentPolyline.SegmentCount)
							{
								case 6:
									// HEX HEX HEX HEX HEX HEX
									// Get Central Hexagon via Scaling
									double scaleFactor = 1.0 / 3.0;
									Transform scaleTransform = Transform.Scale(centroidPoint, scaleFactor);
									PolylineCurve scaledHexagon = new PolylineCurve(currentPolyline);
									scaledHexagon.Transform(scaleTransform);

									// Extract vertices from the PolylineCurve
									Polyline scaledHexagonPolyline = new Polyline();
									scaledHexagon.TryGetPolyline(out scaledHexagonPolyline);

									// @ADD TO LIST (CENTRAL HEX)
									subShapesAsCurves.Add(scaledHexagon);

									// CREATE SUBDIVISION HEXAGONS
									for (int segmentIndex = 0; segmentIndex < currentPolyline.SegmentCount; segmentIndex++)
									{
										Line segment = currentPolyline.SegmentAt(segmentIndex);

										//Find the Midpoints
										Point3d midpoint = segment.PointAt(0.5);

										//Create Vectors for translation
										Vector3d translationVector = centroidPoint - midpoint;
										double amplitude = hex_h / 3 * 2;

										// Vector3d translateVector = translationDirection
										translationVector.Unitize();
										translationVector *= amplitude;

										//CopyMove
										PolylineCurve copiedHexagon = new PolylineCurve(scaledHexagon);
										copiedHexagon.Transform(Transform.Translation(translationVector));

										//@ADD TO CURVELIST (6 HEXES)
										subShapesAsCurves.Add(copiedHexagon);
									}

									break;
								case 4:
									// ROMB ROMB ROMB ROMB ROMB ROMB
									List<Point3d> rombPoints = new List<Point3d>();

									for (int segmentIndex = 0; segmentIndex < currentPolyline.SegmentCount; segmentIndex++)
									{
										Line segment = currentPolyline.SegmentAt(segmentIndex);
										//Find the cornerPoints
										Point3d cornerPoint = segment.PointAt(0);
										rombPoints.Add(cornerPoint);

										RhinoApp.WriteLine("cornerPoint " + cornerPoint);
									}
									// Find and generate the pair of two most remote ones

									Tuple<Point3d, Point3d> distantPair = FindMostDistantPair(rombPoints);

									Line helperLine = new Line(distantPair.Item1, distantPair.Item2);
									// Get Centroids of Future Hexagon

									Point3d smallCentroidA = helperLine.PointAt(1.0 / 6 * 2);
									Point3d smallCentroidB = helperLine.PointAt(1.0 / 6 * 4);

									Polyline hexA = CreateHexagon(smallCentroidA, newSideLength);
									Polyline hexB = CreateHexagon(smallCentroidB, newSideLength);

									List<Curve> subRombHexesAsCurves = new List<Curve>();
									subShapesAsCurves.Add(hexA.ToPolylineCurve());
									subShapesAsCurves.Add(hexB.ToPolylineCurve());

									break;
								case 3:
									// TRIANGLE TRIANGLE TRIANGLE

									Polyline hexC = CreateHexagon(centroidPoint, newSideLength);

									subShapesAsCurves.Add(hexC.ToPolylineCurve());

									break;
								default:
									break;
							}

							// FIND THE REMAINING SHAPES
							List<Curve> remainingSubShapesAsCurves = FindRemainingShapes(subShapesAsCurves, cellPolylineCurve);

							RhinoApp.WriteLine("remainingSubShapesAsCurves", remainingSubShapesAsCurves);
							// ADD remainingSubShapesAsCurves TO subShapesAsCurves

							subShapesAsCurves.AddRange(remainingSubShapesAsCurves);

							// CONVERT THE CURVES TO POLYLINES AND ADD TO - subShapesAsPolylines;
							foreach (var curveItem in subShapesAsCurves)
							{
								Polyline curveAsPolyline;
								if (curveItem.TryGetPolyline(out curveAsPolyline))
								{
									subShapesAsPolylines.Add(curveAsPolyline);
								}
							}

							RhinoApp.WriteLine("subShapesAsPolylines", subShapesAsPolylines);
							// APPEND TO TREE
							subdividedHexes.AddRange(subShapesAsPolylines, subPath);

						}
						else
						{
							subdividedHexes.Add(currentPolyline, subPath);
						}
					}
				}
			}
		}

		// HELPER FUNCTIONS
		private bool IsPointInsideCurves(Point3d testPoint, List<Curve> regionCurves)
		{
			bool result = false;

			foreach (Curve regionCurve in regionCurves)
			{
				if (regionCurve.IsClosed)
				{
					Plane regionPlane;
					if (regionCurve.TryGetPlane(out regionPlane))
					{
						PointContainment containmentValue = regionCurve.Contains(testPoint, regionPlane, 0.01);

						if (containmentValue == PointContainment.Inside)
						{
							result = true; // Set the result to true if any curve meets the criteria.
						}
					}
				}
			}

			return result; // Return the final result after checking all the curves.
		}

		private Tuple<Point3d, Point3d> FindMostDistantPair(List<Point3d> points)
		{
			if (points.Count < 4)
			{
				RhinoApp.WriteLine("Not enough points to find distance.");
				return null;
			}

			Point3d firstPoint = Point3d.Unset;
			Point3d secondPoint = Point3d.Unset;
			double maxDistance = double.MinValue;

			for (int i = 0; i < points.Count - 1; i++)
			{
				for (int j = i + 1; j < points.Count; j++)
				{
					double distance = points[i].DistanceTo(points[j]);

					if (distance > maxDistance)
					{
						maxDistance = distance;
						firstPoint = points[i];
						secondPoint = points[j];
					}
				}
			}

			if (firstPoint.IsValid && secondPoint.IsValid)
			{
				return Tuple.Create(firstPoint, secondPoint);
			}

			return null;
		}

		private Polyline CreateHexagon(Point3d centerPoint, double radius)
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

			return hexagon;
		}

		private List<Curve> FindRemainingShapes(List<Curve> subShapesAsCurves, Curve cellPolylineCurve)
		{

			// List for storing the resulting curves
			List<Curve> remainingShapesAsCurves = new List<Curve>();

			// Union of the subShapesAsCurves list items
			Curve[] shapeUnionArray = Curve.CreateBooleanUnion(subShapesAsCurves, 0.05);

			if (shapeUnionArray != null && shapeUnionArray.Length == 1)
			{
				Curve shapeUnionCurve = shapeUnionArray[0];

				// Create boolean differences
				Curve[] shapeDifferenceCurves = Curve.CreateBooleanDifference(cellPolylineCurve, shapeUnionCurve, 0.05);

				// Add the resulting curves to subShapesAsCurves
				foreach (var curveItem in shapeDifferenceCurves)
				{
					remainingShapesAsCurves.Add(curveItem);
				}

			}

			return remainingShapesAsCurves;
		}
	}
}