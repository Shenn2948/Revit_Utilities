using System.Linq;
using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.Utils
{
    public static class Util
    {
        public static XYZ GetVector(Curve curve)
        {
            XYZ vectorAb = curve.GetEndPoint(0);
            XYZ vectorAc = curve.GetEndPoint(1);
            XYZ vectorBc = vectorAc - vectorAb;
            return vectorBc;
        }

        public static Solid GetSolid(this Element e, bool notVoid = false)
        {
            if (notVoid)
            {
                return e?.get_Geometry(new Options { ComputeReferences = true }).OfType<Solid>().FirstOrDefault(s => !s.Edges.IsEmpty);
            }

            return e?.get_Geometry(new Options { ComputeReferences = true }).OfType<Solid>().FirstOrDefault();
        }

        public static XYZ MidPoint(XYZ p, XYZ q)
        {
            return 0.5 * (p + q);
        }

        public static XYZ MidPoint(Line line)
        {
            return MidPoint(line.GetEndPoint(0), line.GetEndPoint(1));
        }
    }
}