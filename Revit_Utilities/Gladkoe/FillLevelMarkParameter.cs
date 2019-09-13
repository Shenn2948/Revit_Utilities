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
    public static class FillLevelMarkParameter
    {
        private static Document revitDocument;
        private static UIDocument uiRevitDocument;

        public static void FillParams(Document doc, UIDocument uidoc)
        {
            revitDocument = doc;
            uiRevitDocument = uidoc;
            try
            {
                FillParametersAction();

                // Elevation2(doc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction()
        {
            Reference pickedObj = uiRevitDocument.Selection.PickObject(ObjectType.Element, new ElementsOfClassSelectionFilter<Pipe>(), "Select pipe");
            var sb = new StringBuilder();
            using (var tx = new Transaction(revitDocument))
            {
                tx.Start("GetInfo");

                if (revitDocument.GetElement(pickedObj.ElementId) is Pipe e)
                {
                    sb.Append(GetStartToEndElementOffset(e) + GetStartToEndElementOffsetFromSurveyPoint(e));
                    TaskDialog.Show("Info", sb.ToString());
                }

                tx.Commit();
            }
        }

        private static string GetStartToEndElementOffsetFromSurveyPoint(Pipe element)
        {
            StringBuilder sb = new StringBuilder();
            BasePoint projectPoint = new FilteredElementCollector(revitDocument).OfClass(typeof(BasePoint)).Cast<BasePoint>().First(x => !x.IsShared);

            var px = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
            var py = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
            var pz = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();
            XYZ project = new XYZ(px, py, pz);

            LocationCurve lc = element.Location as LocationCurve;
            Curve c = lc.Curve;

            var elementStartPoint = c.GetEndPoint(0).Add(project);
            var elementEndPoint = c.GetEndPoint(1).Add(project);

            sb.Append(
                $" ({Math.Round(elementStartPoint.Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)} - "
                + $"{Math.Round(elementEndPoint.Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)})");
            return sb.ToString();
        }

        private static string GetStartToEndElementOffset(Pipe element)
        {
            StringBuilder sb = new StringBuilder();
            LocationCurve lc = element.Location as LocationCurve;
            Curve c = lc.Curve;

            sb.Append(
                $"{Math.Round(c.GetEndPoint(0).Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)} - "
                + $"{Math.Round(c.GetEndPoint(1).Z.FeetAsMillimeters(), 1, MidpointRounding.ToEven)}");

            return sb.ToString();
        }

        private static void GetStartToEndOffset()
        {
            var pipes = new FilteredElementCollector(revitDocument).OfClass(typeof(Pipe)).Cast<Pipe>();

            foreach (Pipe p in pipes)
            {
                string parameterData = GetStartToEndElementOffset(p) + GetStartToEndElementOffsetFromSurveyPoint(p);
            }
        }
    }
}