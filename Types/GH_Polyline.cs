using GH_IO.Serialization;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;

namespace Tortoise.Types
{
    public abstract class GH_Polyline : GH_Goo<Rhino.Geometry.Polyline>, IGH_Goo
    {
        public GH_Polyline()
        {
            Value = new Rhino.Geometry.Polyline();
        }

        // (DE)SERIALIZATION
        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("PolylineData"))
            {
                string serializedPolyline = reader.GetString("PolylineData");
                Value = DeserializePolyline(serializedPolyline);
                return Value != null;
            }

            return false;
        }
        public override bool Write(GH_IWriter writer)
        {
            string serializedPolyline = SerializePolyline(Value);
            writer.SetString("PolylineData", serializedPolyline);
            return true;
        }

        // Helper Functions
        private Rhino.Geometry.Polyline DeserializePolyline(string serializedPolyline)
        {
            Rhino.Geometry.Polyline polyline = new Rhino.Geometry.Polyline();

            string[] pointStrings = serializedPolyline.Split(';');

            foreach (string pointString in pointStrings)
            {
                string[] coordinates = pointString.Split(',');

                if (coordinates.Length == 3 &&
                    double.TryParse(coordinates[0], out double x) &&
                    double.TryParse(coordinates[1], out double y) &&
                    double.TryParse(coordinates[2], out double z))
                {
                    polyline.Add(new Point3d(x, y, z));
                }
                else
                {
                    // Handle invalid point data
                    return null;
                }
            }

            return polyline;
        }

        private string SerializePolyline(Rhino.Geometry.Polyline polyline)
        {
            List<string> points = new List<string>();

            foreach (Point3d point in polyline)
            {
                points.Add($"{point.X},{point.Y},{point.Z}");
            }

            return string.Join(";", points);
        }

        public override bool IsValid => Value != null && Value.IsValid;

    }
}
