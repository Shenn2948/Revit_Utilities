using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration.Entities
{
    public class FaceCollector
    {
        public List<PlanarFace> GetSideFaces(Element element)
        {
            Solid solid = element.GetSolid(true);
            PlanarFace topFace = GetTopFace(solid);

            Line topFaceCurve = topFace.GetEdgesAsCurveLoops()
                                       .First()
                                       .Cast<Line>()
                                       .Aggregate((i1, i2) => i1.Length > i2.Length
                                                                  ? i1
                                                                  : i2);

            List<PlanarFace> sideFaces = new List<PlanarFace>();

            sideFaces.AddRange(solid.Faces.OfType<PlanarFace>().Where(f => Util.IsParallel(f.FaceNormal, topFaceCurve.Direction)));

            return sideFaces;
        }

        private static PlanarFace GetTopFace(Solid solid)
        {
            return solid.Faces.OfType<PlanarFace>().FirstOrDefault(f => Util.PointsUpwards(f.FaceNormal));
        }
    }
}