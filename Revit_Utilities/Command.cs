namespace Revit_Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Revit_Utilities.Utilities;

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

            // var pipeFittingsDictionary = new FilteredElementCollector(doc).WhereElementIsNotElementType()
            //     .WhereElementIsViewIndependent()
            //     .OfCategory(BuiltInCategory.OST_PipeFitting)
            //     .OfClass(typeof(FamilyInstance))
            //     .Cast<FamilyInstance>()
            //     .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
            //     .Select(
            //         i => new
            //         {
            //             connectors = i.MEPModel.ConnectorManager.Connectors.Cast<Connector>().Select(e => e.AllRefs.Cast<Connector>().FirstOrDefault()),
            //             svarka = i
            //         })
            //     .ToDictionary(e => e.svarka, e => e.connectors.ToList());

            var pipeFittingsDictionary2 = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .SelectMany(i => i.MEPModel.ConnectorManager.Connectors.Cast<Connector>().Select(e => e.AllRefs.Cast<Connector>()).FirstOrDefault())
                .GroupBy(e => e.Owner.Name, e => e)
                .ToDictionary(e => e.Key, e => e.ToList());
                


            foreach (var item in pipeFittingsDictionary2)
            {
                foreach (Connector connector in item.Value)
                {
                    if (connector.AllRefs)
                    {
                    
                    }
                }
            }

            TaskDialog.Show("Revit", "Hello");

            return Result.Succeeded;
        }

        private class ConnectorEqualityComparer : IEqualityComparer<KeyValuePair<FamilyInstance, List<Connector>>>
        {
            public bool Equals(KeyValuePair<FamilyInstance, List<Connector>> x, KeyValuePair<FamilyInstance, List<Connector>> y)
            {
                return x.Value.Where((t, i) => t.Owner.Name.Equals(y.Value[i].Owner.Name)).Any();
            }

            public int GetHashCode(KeyValuePair<FamilyInstance, List<Connector>> obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}