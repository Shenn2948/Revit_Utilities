using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.WallPenetration.Entities
{
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
}