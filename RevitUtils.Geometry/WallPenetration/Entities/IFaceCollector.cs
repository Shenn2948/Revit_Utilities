using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.WallPenetration.Entities
{
    public interface IFaceCollector
    {
        IEnumerable<PlanarFace> GetSideFaces();
    }
}