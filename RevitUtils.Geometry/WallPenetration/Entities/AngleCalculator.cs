using System;
using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.WallPenetration.Entities
{
    public class AngleCalculator
    {
        private readonly WallIntersectionData _intersectionData;
        private readonly double _angle90;

        public AngleCalculator(WallIntersectionData intersectionData)
        {
            _intersectionData = intersectionData;

            _angle90 = UnitUtils.ConvertToInternalUnits(90, DisplayUnitType.DUT_DECIMAL_DEGREES);

            GetAngles();
            GetTranslation();
        }

        public XYZ Translation { get; private set; }
        public XYZ LocationPoint { get; private set; }

        public double VerticalAngle { get; private set; }

        public double HorizontalAngle { get; private set; }

        private void GetAngles()
        {
            VerticalAngle = -_intersectionData.WallNormal.Vector.AngleOnPlaneTo(_intersectionData.IntersectionDirection.Vector, _intersectionData.WallDirection.Vector.Normalize());
            HorizontalAngle = _intersectionData.WallDirection.Vector.AngleOnPlaneTo(_intersectionData.IntersectionDirection.Vector, XYZ.BasisZ) - _angle90;
        }

        private void GetTranslation()
        {
            double x = Math.Tan(VerticalAngle) * _intersectionData.Wall.Width / 2;
            double z = Math.Tan(HorizontalAngle) * _intersectionData.Wall.Width / 2;

            var mid = _intersectionData.MidPoint;

            Translation = new XYZ(mid.X + z, mid.Y, mid.Z + x) - mid;
            LocationPoint = new XYZ(mid.X + z, mid.Y, mid.Z + x);
        }
    }
}