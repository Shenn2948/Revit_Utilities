namespace Gladkoe.FillingParameters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Autodesk.Revit.ApplicationServices;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyParametersFromPipe : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var welds = GetWelds(doc);
                var parameters = GetPipeParameters(welds);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Recolor", e.Message);
            }

            return Result.Succeeded;
        }

        private static List<FamilyInstance> GetWelds(Document doc)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .ToList();
        }

        private static IEnumerable<FamilyInstance> GetPipeParameters(IEnumerable<FamilyInstance> welds)
        {
            var query = welds.SelectMany(e => e.MEPModel.ConnectorManager.Connectors.Cast<Connector>(), (weld, connector) => new { weld, connector })
                .SelectMany(t => t.connector.AllRefs.Cast<Connector>(), (tuple, reference) => new { tuple, reference })
                .GroupBy(w => w.tuple.weld, arg => arg.reference.Owner)
                .ToDictionary(
                    e => e.Key,
                    elements => elements.Select(
                        i => new
                             {
                                 PipeParameters = i.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves ? i.GetOrderedParameters() : null,
                                 Fitings = i.Category.Id.IntegerValue != (int)BuiltInCategory.OST_PipeCurves ? i : null
                             }));


                // .Where(t => t.reference.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves)
                // .Select(t => t.tuple.weld);

            return null;
        }

    }
}