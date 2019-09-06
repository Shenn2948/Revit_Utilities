namespace Revit_Utilities
{
    using System.Collections.Generic;
    using System.Linq;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

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

            ChangeColor(doc);

            return Result.Succeeded;
        }

        private static void ChangeColor(Document doc)
        {
            var welds = GetWeld(doc).ToList();

            var welds2 = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .GroupBy(
                    i => i.MEPModel.ConnectorManager.Connectors.Cast<Connector>().SelectMany(e => e.AllRefs.Cast<Connector>()).FirstOrDefault().Owner.Name,
                    i => i)
                .Where(
                    e => e.Key.Contains("Азот") || e.Key.Contains("Вода") || e.Key.Contains("Газ") || e.Key.Contains("Дренаж") || e.Key.Contains("Канализация")
                         || e.Key.Contains("Нефтепродукты") || e.Key.Contains("Пенообразователь") || e.Key.Contains("ХимическиеРеагенты"))
                .ToDictionary(e => e.Key, e => e.ToList());

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change");

                // Change(doc, welds, "0_153_255", "Азот");
                // Change(doc, welds, "0_96_0", "Вода");
                // Change(doc, welds, "255_220_112", "Газ");
                // Change(doc, welds, "192_192_192", "Дренаж");
                // Change(doc, welds, "192_192_192", "Канализация");
                // Change(doc, welds, "160_80_0", "Нефтепродукты");
                // Change(doc, welds, "224_0_0", "Пенообразователь");
                // Change(doc, welds, "128_96_0", "ХимическиеРеагенты");
                foreach (var item in welds2)
                {
                    if (item.Key.Contains("Дренаж"))
                    {
                        Change(doc, item.Value, "192_192_192", "Дренаж");
                    }
                }

                tran.Commit();
            }
        }

        private static IEnumerable<FamilyInstance> GetWeld(Document doc)
        {
            return new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"));
        }

        private static void Change(Document doc, IEnumerable<FamilyInstance> welds, string color, string pipeType)
        {
            ElementId material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals(color))?.Id;

            var chemicalPipe = from e in welds
                               from Connector connector in e.MEPModel.ConnectorManager.Connectors
                               from Connector reference in connector.AllRefs
                               where reference.Owner.Name.Contains(pipeType)
                               select e;

            var fittingConnectors = from e in chemicalPipe
                                    from Connector connector in e.MEPModel.ConnectorManager.Connectors
                                    from Connector reference in connector.AllRefs
                                    where (reference.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                                          && !reference.Owner.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные")
                                    select reference.Owner;

            foreach (Element element in fittingConnectors)
            {
                Parameter p = element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals("МатериалФитинга"));
                p?.Set(material);
            }
        }
    }
}