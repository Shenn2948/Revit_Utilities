using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit_Utilities.Gladkoe
{
    using System.Diagnostics;
    using System.Text;

    using Autodesk.Revit.Creation;
    using Autodesk.Revit.DB.Mechanical;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.UI.Selection;

    using Revit_Utilities.Utilities;

    using Document = Autodesk.Revit.DB.Document;

    public class FillLevelMarkParameter
    {
        public static void FillParams(Document doc, UIDocument uidoc)
        {
            try
            {
                FillParametersAction(doc, uidoc);

                //Elevation2(doc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction(Document doc, UIDocument uidoc)
        {
            Reference pickedObj = uidoc.Selection.PickObject(ObjectType.Element, "Select element");

            using (Transaction tx = new Transaction(doc))
            {
                StringBuilder sb = new StringBuilder();
                tx.Start("GetInfo");

                Element e = doc.GetElement(pickedObj.ElementId);

                LocationCurve lc = e.Location as LocationCurve;
                Curve c = lc.Curve;

                sb.Append("\n" + $"Pipe {e.Id.IntegerValue} from {c.GetEndPoint(0).Z.FeetAsMillimeters()} to { c.GetEndPoint(1).Z.FeetAsMillimeters()}");

                TaskDialog.Show("Info", sb.ToString());

                tx.Commit();
            }
        }

        private static void Elevation(Document doc, Element element)
        {
            BasePoint projectPoint = new FilteredElementCollector(doc).OfClass(typeof(BasePoint)).Cast<BasePoint>().First(x => !x.IsShared);

            var px = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
            var py = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
            var pz = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();
            XYZ project = new XYZ(px, py, pz);

            if (element.Location is LocationCurve lc)
            {
            }

            // var elementPoint = loc.Point.Subtract(project).Z;
            // return elementPoint;
        }

        private static void GetStartToEndOffset(Document doc)
        {
            var a = new FilteredElementCollector(doc).OfClass(typeof(Pipe)).Cast<Pipe>();

            int numDucts = 0;
            int numCurves = 0;
            StringBuilder sb = new StringBuilder();
            foreach (Pipe d in a)
            {
                ++numDucts;

                LocationCurve lc = d.Location as LocationCurve;

                ++numCurves;

                Curve c = lc.Curve;
                sb.Append("\n" + $"Pipe {d.Id.IntegerValue} from {c.GetEndPoint(0).Z.FeetAsMillimeters()} to { c.GetEndPoint(1).Z.FeetAsMillimeters()}");
            }

            sb.AppendLine();
            sb.Append($"{numDucts} Pipes analysed, and {numCurves} curve listed.");
            TaskDialog.Show("I", sb.ToString());
        }
    }
}