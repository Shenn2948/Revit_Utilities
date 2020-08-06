using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.Utils
{
    public static class Util
    {
        public const double Eps = 1.0e-9;

        public static double MinLineLength =>
            Eps;

        public static double TolPointOnPlane =>
            Eps;

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

        public static int Compare(double a, double b, double tolerance = Eps)
        {
            return IsEqual(a, b, tolerance)
                       ? 0
                       : (a < b
                              ? -1
                              : 1);
        }

        public static int Compare(XYZ p, XYZ q, double tolerance = Eps)
        {
            int d = Compare(p.X, q.X, tolerance);

            if (0 == d)
            {
                d = Compare(p.Y, q.Y, tolerance);

                if (0 == d)
                {
                    d = Compare(p.Z, q.Z, tolerance);
                }
            }

            return d;
        }

        /// <summary>
        /// Implement a comparison operator for lines
        /// in the XY plane useful for sorting into
        /// groups of parallel lines.
        /// </summary>
        public static int Compare(Line a, Line b)
        {
            XYZ pa = a.GetEndPoint(0);
            XYZ qa = a.GetEndPoint(1);
            XYZ pb = b.GetEndPoint(0);
            XYZ qb = b.GetEndPoint(1);
            XYZ va = qa - pa;
            XYZ vb = qb - pb;

            // Compare angle in the XY plane

            double ang_a = Math.Atan2(va.Y, va.X);
            double ang_b = Math.Atan2(vb.Y, vb.X);

            int d = Compare(ang_a, ang_b);

            if (0 == d)
            {
                // Compare distance of unbounded line to origin
                double da = (qa.X * pa.Y - qa.Y * pa.Y) / va.GetLength();
                double db = (qb.X * pb.Y - qb.Y * pb.Y) / vb.GetLength();

                d = Compare(da, db);

                if (0 == d)
                {
                    // Compare distance of start point to origin
                    d = Compare(pa.GetLength(), pb.GetLength());

                    if (0 == d)
                    {
                        // Compare distance of end point to origin
                        d = Compare(qa.GetLength(), qb.GetLength());
                    }
                }
            }

            return d;
        }

        public static int Compare(Plane a, Plane b)
        {
            int d = Compare(a.Normal, b.Normal);

            if (0 == d)
            {
                d = Compare(a.SignedDistanceTo(XYZ.Zero), b.SignedDistanceTo(XYZ.Zero));

                if (0 == d)
                {
                    d = Compare(a.XVec.AngleOnPlaneTo(b.XVec, b.Normal), 0);
                }
            }

            return d;
        }

        public static bool IsZero(double a, double tolerance = Eps)
        {
            return tolerance > Math.Abs(a);
        }

        public static bool IsEqual(double a, double b, double tolerance = Eps)
        {
            return IsZero(b - a, tolerance);
        }

        /// <summary>
        /// Predicate to test whether two points or
        /// vectors can be considered equal with the
        /// given tolerance.
        /// </summary>
        public static bool IsEqual(XYZ p, XYZ q, double tolerance = Eps)
        {
            return 0 == Compare(p, q, tolerance);
        }


        public static Transform GetTransformToZ(XYZ v)
        {
            Transform t;

            double angle = XYZ.BasisZ.AngleTo(v);

            if (IsZero(angle))
            {
                t = Transform.Identity;
            }
            else
            {
                XYZ axis = IsEqual(angle, Math.PI)
                               ? XYZ.BasisX
                               : v.CrossProduct(XYZ.BasisZ);

                t = Transform.CreateRotationAtPoint(axis, angle, XYZ.Zero);
            }

            return t;
        }
    }

    public static class JtPlaneExtensionMethods
    {
        /// <summary>
        /// Return the signed distance from
        /// a plane to a given point.
        /// </summary>
        public static double SignedDistanceTo(this Plane plane, XYZ p)
        {
            XYZ v = p - plane.Origin;

            return plane.Normal.DotProduct(v);
        }

        /// <summary>
        /// Project given 3D XYZ point onto plane.
        /// </summary>
        public static XYZ ProjectOnto(this Plane plane, XYZ p)
        {
            double d = plane.SignedDistanceTo(p);

            XYZ q = p - d * plane.Normal;

            return q;
        }

        /// <summary>
        /// Project given 3D XYZ point into plane,
        /// returning the UV coordinates of the result
        /// in the local 2D plane coordinate system.
        /// </summary>
        public static UV ProjectInto(this Plane plane, XYZ p)
        {
            XYZ q = plane.ProjectOnto(p);
            XYZ o = plane.Origin;
            XYZ d = q - o;
            double u = d.DotProduct(plane.XVec);
            double v = d.DotProduct(plane.YVec);
            return new UV(u, v);
        }
    }
}