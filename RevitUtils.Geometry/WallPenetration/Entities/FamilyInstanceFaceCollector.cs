using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.WallPenetration.Entities
{
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
}