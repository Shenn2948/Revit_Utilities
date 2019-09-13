using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Revit_Utilities.Utilities;

namespace Revit_Utilities.Gladkoe
{
    public class FillLevelMarkParameter
    {
        public static void FillParams(Document doc, UIDocument uidoc)
        {
            try
            {
                FillParametersAction(doc, uidoc);

                // Elevation2(doc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction(Document doc, UIDocument uidoc)
        {
            Reference pickedObj = uidoc.Selection.PickObject(ObjectType.Element, "Select element");
            StringBuilder sb = new StringBuilder();
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("GetInfo");

                Element e = doc.GetElement(pickedObj.ElementId);

                LocationCurve lc = e.Location as LocationCurve;
                Curve c = lc.Curve;

                sb.Append(
                    "\n"
                    + $"Pipe {e.Id.IntegerValue} from {Math.Round(c.GetEndPoint(0).Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)} to"
                    + $" {Math.Round(c.GetEndPoint(1).Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)}");
                sb.Append(GetStartToEndOffsetFromSurveyPoint(doc, e));

                TaskDialog.Show("Info", sb.ToString());

                tx.Commit();
            }
        }

        private static string GetStartToEndOffsetFromSurveyPoint(Document doc, Element e)
        {
            StringBuilder sb = new StringBuilder();
            BasePoint projectPoint = new FilteredElementCollector(doc).OfClass(typeof(BasePoint)).Cast<BasePoint>().First(x => !x.IsShared);

            var px = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
            var py = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
            var pz = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();
            XYZ project = new XYZ(px, py, pz);

            LocationCurve lc = e.Location as LocationCurve;
            Curve c = lc.Curve;

            var elementStartPoint = c.GetEndPoint(0).Add(project);
            var elementEndPoint = c.GetEndPoint(1).Add(project);

            sb.Append(
                "\n"
                + $"Pipe {e.Id.IntegerValue} from {Math.Round(elementStartPoint.Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)} to "
                + $"{Math.Round(elementEndPoint.Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)}");
            return sb.ToString();
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
                sb.Append(
                    "\n"
                    + $"Pipe {d.Id.IntegerValue} from {Math.Round(c.GetEndPoint(0).Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)} to"
                    + $" {Math.Round(c.GetEndPoint(1).Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)}");
            }

            sb.AppendLine();
            sb.Append($"{numDucts} Pipes analysed, and {numCurves} curve listed.");
            TaskDialog.Show("I", sb.ToString());
        }
    }
}