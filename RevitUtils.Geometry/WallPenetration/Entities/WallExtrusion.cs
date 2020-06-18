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

    public class FamilyInstanceFaceCollector : IFaceCollector
    {
        private readonly Instance _element;

        public FamilyInstanceFaceCollector(Instance element)
        {
            _element = element;
        }

        public IEnumerable<PlanarFace> GetSideFaces()
        {
            Solid solid = _element.GetSolid(true);

            var topFace = GetTopFace(_element, solid);

            Line topFaceCurve = topFace.GetEdgesAsCurveLoops()
                                       .First()
                                       .Cast<Line>()
                                       .Aggregate((i1, i2) => i1.Length > i2.Length
                                                                  ? i1
                                                                  : i2);

            return solid.Faces.OfType<PlanarFace>().Where(f => Util.IsParallel(f.FaceNormal, topFaceCurve.Direction));
        }

        private static PlanarFace GetTopFace(Instance element, Solid solid)
        {
            XYZ origin = element.GetTransform().Origin;
            XYZ originOffsetZ = new XYZ(origin.X, origin.Y, (origin.Z + 50));
            Line originIntersect = Line.CreateBound(origin, originOffsetZ);

            foreach (PlanarFace f in solid.Faces.OfType<PlanarFace>())
            {
                SetComparisonResult result = f.Intersect(originIntersect, out var results);
                if (result == SetComparisonResult.Overlap)
                {
                    return f;
                }
            }

            return null;
        }
    }

    public class MepCurveFaceCollector : IFaceCollector
    {
        private readonly MEPCurve _mepCurve;

        public MepCurveFaceCollector(MEPCurve mepCurve)
        {
            _mepCurve = mepCurve;
        }

        public IEnumerable<PlanarFace> GetSideFaces()
        {
            Solid solid = _mepCurve.GetSolid(true);

            Connector connector = _mepCurve.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault();

            return solid.Faces.OfType<PlanarFace>();
        }
    }

    public interface IFaceCollector
    {
        IEnumerable<PlanarFace> GetSideFaces();
    }
}