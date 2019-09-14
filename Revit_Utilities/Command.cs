namespace Revit_Utilities
{
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Revit_Utilities.Gladkoe;
    using Revit_Utilities.Gladkoe.LineSectionNumberFillParameter;
    using Revit_Utilities.Gladkoe_Recolor;

    using Application = Autodesk.Revit.ApplicationServices.Application;

    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // RecolorerOneQuery.ChangeColor(doc);
            // LevelMarkFillParameter.FillParams(doc, uidoc);
            LineSectionNumberFillParameter.FillParams(doc, uidoc);

            return Result.Succeeded;
        }
    }
}