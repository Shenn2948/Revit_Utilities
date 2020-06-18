using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.WallPenetration.Entities
{
    public class WallExtrusion
    {
        private readonly Element _element;
        private readonly Wall _wall;

        public WallExtrusion(Element element, Wall wall)
        {
            _element = element;
            _wall = wall;
            Initialize();
        }

        public double Width { get; private set; }

        public double Height { get; private set; }

        public Curve LocationCurve { get; private set; }

        private void Initialize()
        {
            List<PlanarFace> sideFaces = GetSideFaces().ToList();
            GetGabarits(sideFaces[0]);

            Line locationCurve = Line.CreateBound(Util.MidPoint(sideFaces[0]), Util.MidPoint(sideFaces[1]));
            //LocationCurve = GetIntersectingCurve(locationCurve);
            LocationCurve = locationCurve;
        }

        private Line GetIntersectingCurve(Curve locationCurve)
        {
            Solid wallSolid = _wall.GetSolid();

            SolidCurveIntersection line = wallSolid.IntersectWithCurve(locationCurve, new SolidCurveIntersectionOptions());
            Line curveSegment = line.GetCurveSegment(0) as Line;
            return curveSegment;
        }

        private IEnumerable<PlanarFace> GetSideFaces()
        {
            IFaceCollector collector;

            if (_element is MEPCurve curve)
            {
                collector = new MepCurveFaceCollector(curve);
                return collector.GetSideFaces();
            }

            if (_element is Instance instance)
            {
                collector = new FamilyInstanceFaceCollector(instance);
                return collector.GetSideFaces();
            }

            return Enumerable.Empty<PlanarFace>();
        }

        private void GetGabarits(Face face)
        {
            BoundingBoxUV b = face.GetBoundingBox();
            UV p = b.Min;
            UV q = b.Max;

            Height = Math.Abs(p.U) + Math.Abs(q.U);
            Width = Math.Abs(p.V) + Math.Abs(q.V);
        }
    }
}