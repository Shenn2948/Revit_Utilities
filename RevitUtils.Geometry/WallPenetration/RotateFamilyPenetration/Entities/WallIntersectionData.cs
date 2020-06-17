using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration.Entities
{
    public class WallIntersectionData
    {
        private readonly Line _intersectingCurve;

        private readonly Transform _transform;

        public WallIntersectionData(Wall wall, Element intersector)
        {
            Wall = wall;

            _intersectingCurve = GetIntersectingCurve(intersector);

            WallSideFaceRef = GetWallSideFaceRef();
            MidPoint = Util.MidPoint(_intersectingCurve);

            _transform = Transform.CreateTranslation(MidPoint);

            WallDirection = GetWallDirectionLineInfo();
            WallNormal = GetWallNormalInfo();
            IntersectionDirection = GetIntersectionLineInfo();
        }

        public Wall Wall { get; }

        public XYZ MidPoint { get; }

        public Reference WallSideFaceRef { get; }

        public DirectionInfo WallDirection { get; }

        public DirectionInfo WallNormal { get; }

        public DirectionInfo IntersectionDirection { get; }

        private DirectionInfo GetWallDirectionLineInfo()
        {
            LocationCurve locationCurve = Wall.Location as LocationCurve;
            Line locLine = locationCurve?.Curve as Line;
            XYZ wallDir = _transform.OfPoint(locLine?.Direction);

            var l = Line.CreateBound(MidPoint, wallDir);

            return new DirectionInfo(l, Util.GetVector(l));
        }

        private DirectionInfo GetIntersectionLineInfo()
        {
            XYZ interDir = _transform.OfPoint(_intersectingCurve.Direction);
            var l = Line.CreateBound(MidPoint, interDir);

            return new DirectionInfo(l, Util.GetVector(l));
        }

        private DirectionInfo GetWallNormalInfo()
        {
            PlanarFace wallSideFace = Wall.Document.GetElement(WallSideFaceRef).GetGeometryObjectFromReference(WallSideFaceRef) as PlanarFace;
            XYZ wallNormalDir = _transform.OfPoint(wallSideFace?.FaceNormal);
            var l = Line.CreateBound(MidPoint, wallNormalDir);

            return new DirectionInfo(l, Util.GetVector(l));
        }

        private Reference GetWallSideFaceRef()
        {
            IList<Reference> wallSideFaceRefs = HostObjectUtils.GetSideFaces(Wall, ShellLayerType.Interior);
            return wallSideFaceRefs[0];
        }

        private Line GetIntersectingCurve(Element intersector)
        {
            Solid wallSolid = Wall.GetSolid();

            if (intersector.Location is LocationCurve locationCurve)
            {
                SolidCurveIntersection line = wallSolid.IntersectWithCurve(locationCurve.Curve, new SolidCurveIntersectionOptions());
                Line curveSegment = line.GetCurveSegment(0) as Line;
                return curveSegment;
            }

            return null;
        }
    }
}