using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Tortoise
{
    public class TortoiseInfo : GH_AssemblyInfo
    {
        public override string Name => "Tortoise";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("b5196f76-255f-40ba-8e16-688c6a69ed00");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}