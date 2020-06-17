using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.Utils
{
    public static class Util
    {
        public const double Eps = 1.0e-9;

        public static XYZ GetVector(Curve curve)
        {
            XYZ vectorAb = curve.GetEndPoint(0);
            XYZ vectorAc = curve.GetEndPoint(1);
            XYZ vectorBc = vectorAc - vectorAb;
            return vectorBc;
        }

        public static Solid GetSolid(this Element e, bool notVoid = false)
        {
            GeometryElement geo = e.get_Geometry(new Options { ComputeReferences = true });

            if (e is FamilyInstance instance)
            {
                Options geometryOptions = e.Document.Application.Create.NewGeometryOptions();
                GeometryElement slaveGeo = instance.GetOriginalGeometry(geometryOptions);
                geo = slaveGeo.GetTransformed(instance.GetTransform());
            }

            if (notVoid)
            {
                return geo.OfType<Solid>().FirstOrDefault(s => !s.Edges.IsEmpty);
            }

            return geo.OfType<Solid>().FirstOrDefault();
        }

        public static XYZ MidPoint(XYZ p, XYZ q)
        {
            return 0.5 * (p + q);
        }

        public static XYZ MidPoint(Face face)
        {
            BoundingBoxUV b = face.GetBoundingBox();
            UV p = b.Min;
            UV q = b.Max;
            UV midparam = p + 0.5 * (q - p);
            XYZ midpoint = face.Evaluate(midparam);

            return midpoint;
        }

        public static XYZ MidPoint(Line line)
        {
            return MidPoint(line.GetEndPoint(0), line.GetEndPoint(1));
        }

        private const double MinimumSlope = 0.3;

        public static bool PointsUpwards(XYZ v)
        {
            double horizontalLength = v.X * v.X + v.Y * v.Y;
            double verticalLength = v.Z * v.Z;

            return 0 < v.Z && MinimumSlope < verticalLength / horizontalLength;
        }

        public static bool IsVertical(XYZ v)
        {
            return IsZero(v.X) && IsZero(v.Y);
        }

        public static bool IsParallel(XYZ p, XYZ q)
        {
            return p.CrossProduct(q).IsZeroLength();
        }

        public static bool IsZero(double a, double tolerance = Eps)
        {
            return tolerance > Math.Abs(a);
        }
    }
}