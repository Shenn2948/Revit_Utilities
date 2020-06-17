using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration.Entities
{
    public class DirectionInfo
    {
        public DirectionInfo(Line line, XYZ vector)
        {
            Line = line;
            Vector = vector;
        }

        public Line Line { get; }

        public XYZ Vector { get; }
    }
}