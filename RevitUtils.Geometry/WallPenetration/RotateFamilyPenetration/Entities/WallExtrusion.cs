using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration.Entities
{
    public class WallExtrusion
    {
        private readonly Element _element;

        public WallExtrusion(Element element)
        {
            _element = element;
            LocationCurve = GetCenterLineFromFaceAnalyze();
        }

        public double Width { get; set; }

        public double Height { get; set; }

        public Curve LocationCurve { get; }

        private Curve GetCenterLineFromFaceAnalyze()
        {
            List<PlanarFace> sideFaces = GetSideFaces();
            GetGabarits(sideFaces[0]);

            return Line.CreateBound(Util.MidPoint(sideFaces[0]), Util.MidPoint(sideFaces[1]));
        }

        private List<PlanarFace> GetSideFaces()
        {
            var collector = new FaceCollector();
            return collector.GetSideFaces(_element);
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