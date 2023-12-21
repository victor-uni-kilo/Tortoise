using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Tortoise.Utility;

namespace Tortoise
{
// ███████╗██╗   ██╗██████╗ ██████╗ ██╗██╗   ██╗██╗██████╗ ███████╗    
// ██╔════╝██║   ██║██╔══██╗██╔══██╗██║██║   ██║██║██╔══██╗██╔════╝    
// ███████╗██║   ██║██████╔╝██║  ██║██║██║   ██║██║██║  ██║█████╗      
// ╚════██║██║   ██║██╔══██╗██║  ██║██║╚██╗ ██╔╝██║██║  ██║██╔══╝      
// ███████║╚██████╔╝██████╔╝██████╔╝██║ ╚████╔╝ ██║██████╔╝███████╗    
// ╚══════╝ ╚═════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝  ╚═╝╚═════╝ ╚══════╝    
                                                                     
// ██╗  ██╗███████╗██╗  ██╗                                            
// ██║  ██║██╔════╝╚██╗██╔╝                                            
// ███████║█████╗   ╚███╔╝                                             
// ██╔══██║██╔══╝   ██╔██╗                                             
// ██║  ██║███████╗██╔╝ ██╗                                            
// ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝                                            
                                                                     
// ██╗   ██╗███████╗██╗███╗   ██╗ ██████╗                              
// ██║   ██║██╔════╝██║████╗  ██║██╔════╝                              
// ██║   ██║███████╗██║██╔██╗ ██║██║  ███╗                             
// ██║   ██║╚════██║██║██║╚██╗██║██║   ██║                             
// ╚██████╔╝███████║██║██║ ╚████║╚██████╔╝                             
//  ╚═════╝ ╚══════╝╚═╝╚═╝  ╚═══╝ ╚═════╝                              
                                                                     
// ██████╗ ███████╗ ██████╗ ██╗ ██████╗ ███╗   ██╗███████╗             
// ██╔══██╗██╔════╝██╔════╝ ██║██╔═══██╗████╗  ██║██╔════╝             
// ██████╔╝█████╗  ██║  ███╗██║██║   ██║██╔██╗ ██║███████╗             
// ██╔══██╗██╔══╝  ██║   ██║██║██║   ██║██║╚██╗██║╚════██║             
// ██║  ██║███████╗╚██████╔╝██║╚██████╔╝██║ ╚████║███████║             
// ╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝             
                                                                    
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
		/// 
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
		{
			pManager.AddCurveParameter("Hexagonal cells", "H", "Hexagonal / subdivided grid cells", GH_ParamAccess.tree);
			pManager.AddCurveParameter("Regions", "R", "Selection regions", GH_ParamAccess.list);
			pManager.AddIntegerParameter("Hexagon count along the apothem", "Hc", "Number of hexagons along the apothem", GH_ParamAccess.item, 0);
		}
		/// <summary>
		/// Registers all the output parameters for this component.
		/// </summary>
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
		{
			pManager.AddCurveParameter("Subdivided Matrix", "M", "Hexagonal/Subdivided Grid Cells", GH_ParamAccess.list);
		}
		/// <summary>
		/// This is the method that actually does the work.
		/// </summary>
		/// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
		/// 
		
		protected override void SolveInstance(IGH_DataAccess DA)
		{
			// INPUT WRAPPERS
			GH_Structure<GH_Curve> hexCellsInputTree = new GH_Structure<GH_Curve>();
			List<GH_Curve> regionCurvesInputList = new List<GH_Curve>();
			GH_Integer hexagonsAlongApothem = new GH_Integer();

			// OUTPUT WRAPPERS
			GH_Structure<GH_Curve> hexCellsOutputTree = new GH_Structure<GH_Curve>();

			// DATA Validation for input H
			if (DA.GetDataTree<GH_Curve>(0, out hexCellsInputTree))
			{
				bool areClosedPolylines = PolylineValidator.curvesArePolylinesTree(hexCellsInputTree, true);

				if (!areClosedPolylines) {
					AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input H expects closed polyline(s)");
					return;
				}
			}

			// DATA Validation for input R
			if (DA.GetDataList<GH_Curve>(1, regionCurvesInputList))
			{
				bool areClosedRegionCurves = ClosedCurveValidator.areClosedCurves(regionCurvesInputList);

				if (!areClosedRegionCurves)
				{
					AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input R expects closed curve(s)");
					return;
				}
			}

			// DATA Validation for input Hc
			DA.GetData<GH_Integer>(2, ref hexagonsAlongApothem);
		

			if (hexCellsInputTree.PathCount > 0)
			{
				//RUN SPECIAL FUNCTION
				ProcessTreeBranch(hexCellsInputTree, regionCurvesInputList, hexagonsAlongApothem.Value, ref hexCellsOutputTree);

				DA.SetDataTree(0, hexCellsOutputTree);
			}
			else
			{
				AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input H is empty tree");
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

		// ALGORITHM SECTION
		// ALGORITHM SECTION

		private void ProcessTreeBranch(GH_Structure<GH_Curve> traversalTree, List<GH_Curve> regionCurves, int hexagonsAlongApothem,  ref GH_Structure<GH_Curve> subdividedHexes)
		{
			// ******* ITERATE OVER BRANCHES *******
			for (int iBranch = 0; iBranch < traversalTree.PathCount; iBranch++)
			{
				GH_Path currentPath = traversalTree.Paths[iBranch];
				List<GH_Curve> currentBranch = traversalTree.Branches[iBranch];

				for (int i = 0; i < currentBranch.Count; i++)
				{
					// Create SubPath {.;.;... + new}
					GH_Path subPath = new GH_Path(currentPath).AppendElement(i);

					// Main list item
					Curve unwrappedCurve = currentBranch[i].Value;

					// WORK ONLY IF THE THERE ARE REGION CURVES AND IF POLYLINE IS HEXAGON
					if (regionCurves != null && regionCurves.Count > 0)
					{
						// DEFINE THE POLYLINE (segmentOperations) and POLYLINECURVE (booleanOperations)
						Polyline currentPolyline;

						if (!unwrappedCurve.TryGetPolyline(out currentPolyline))
						{
							AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Curve to Polyline");
							return;
						}

						// FIND THE CENTROID
						Point3d centroidPoint = currentPolyline.CenterPoint();

						// FILTER OUT THE SELECTED HEXES (byRegionCurves)
						if (PointInsideRegionValidator.isPointInsideCurves(centroidPoint, regionCurves))
						{
							PolygonIdentifier.PolygonFragmentType polylineType;

							if (!PolygonIdentifier.identifyFragment(currentPolyline, out polylineType))
							{
								AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to classify the polyline fragment");
								return;
							}

							switch (polylineType)
							{
								case PolygonIdentifier.PolygonFragmentType.Hexagon:

									List<GH_Curve> HexagonSubdivisionFragments = Subdivision.HexagonShape.apothemSubdivision(currentPolyline, hexagonsAlongApothem);
									// RETURN THE HEX POLYLINE SUBDIVISION HERE AS GH_PATHS AND PUSH TO
									foreach (var fragment in HexagonSubdivisionFragments)
									{
										subdividedHexes.Append(fragment, subPath);
									}
									break;
								default:
									break;
							}

							// FIND THE REMAINING SHAPES

						}
						else
						{
							// NOT WITHIN REGION HENCE JUST PASS
							subdividedHexes.Append(currentBranch[i], subPath);
						}
					}
				}
			}
		}
	}
}