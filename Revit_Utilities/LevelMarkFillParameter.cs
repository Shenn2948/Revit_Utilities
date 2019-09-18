namespace Gladkoe
{
    using System;
    using System.Linq;
    using System.Text;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;

    using Gladkoe.Utilities;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LevelMarkFillParameter : IExternalCommand
    {
        public static Document RevitDocument { get; private set; }

        public static UIDocument UiRevitDocument { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UiRevitDocument = uiapp.ActiveUIDocument;
            RevitDocument = UiRevitDocument.Document;

            try
            {
                FillParametersAction();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void FillParametersAction()
        {
            Reference pickedObj = UiRevitDocument.Selection.PickObject(ObjectType.Element, new ElementsOfClassSelectionFilter<Pipe>(), "Select pipe");
            var sb = new StringBuilder();
            using (var tx = new Transaction(RevitDocument))
            {
                tx.Start("GetInfo");

                if (RevitDocument.GetElement(pickedObj.ElementId) is Pipe e)
                {
                    sb.Append(GetStartToEndPipeOffset(e));
                    sb.Append(GetStartToEndPipeOffsetFromSurveyPoint(e));
                    TaskDialog.Show("Info", sb.ToString());
                }

                tx.Commit();
            }
        }

        private static string GetStartToEndPipeOffsetFromSurveyPoint(Pipe element)
        {
            StringBuilder sb = new StringBuilder();
            BasePoint projectPoint = new FilteredElementCollector(RevitDocument).OfClass(typeof(BasePoint)).Cast<BasePoint>().First(x => !x.IsShared);

            var px = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
            var py = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
            var pz = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();
            XYZ project = new XYZ(px, py, pz);

            LocationCurve lc = element.Location as LocationCurve;
            Curve c = lc.Curve;

            var elementStartPoint = c.GetEndPoint(0).Add(project);
            var elementEndPoint = c.GetEndPoint(1).Add(project);

            sb.Append(
                $" ({Math.Round(UnitUtils.ConvertFromInternalUnits(elementStartPoint.Z, DisplayUnitType.DUT_MILLIMETERS), 1, MidpointRounding.ToEven)} - "
                + $"{Math.Round(UnitUtils.ConvertFromInternalUnits(elementEndPoint.Z, DisplayUnitType.DUT_MILLIMETERS), 1, MidpointRounding.ToEven)})");

            return sb.ToString();
        }

        private static string GetStartToEndPipeOffset(Pipe element)
        {
            StringBuilder sb = new StringBuilder();
            LocationCurve lc = element.Location as LocationCurve;
            Curve c = lc.Curve;

            sb.Append(
                $"From {Math.Round(UnitUtils.ConvertFromInternalUnits(c.GetEndPoint(0).Z, DisplayUnitType.DUT_MILLIMETERS), 1, MidpointRounding.ToEven)} to "
                + $"{Math.Round(UnitUtils.ConvertFromInternalUnits(c.GetEndPoint(1).Z, DisplayUnitType.DUT_MILLIMETERS), 1, MidpointRounding.ToEven)}");

            return sb.ToString();
        }

        private static void GetPipeOffsets()
        {
            var pipes = new FilteredElementCollector(RevitDocument).OfClass(typeof(Pipe)).Cast<Pipe>();

            foreach (Pipe p in pipes)
            {
                string parameterData = GetStartToEndPipeOffset(p) + GetStartToEndPipeOffsetFromSurveyPoint(p);
            }
        }
    }
}