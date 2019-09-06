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
    using Autodesk.Revit.UI.Selection;

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

            return Result.Succeeded;
        }

        private static void ChangeColor(Document doc)
        {
            var welds = GetWeld(doc).ToList();

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change");

                Change(doc, welds, "0_153_255", "Азот");
                Change(doc, welds, "0_96_0", "Вода");
                Change(doc, welds, "255_220_112", "Газ");
                Change(doc, welds, "192_192_192", "Дренаж");
                Change(doc, welds, "192_192_192", "Канализация");
                Change(doc, welds, "160_80_0", "Нефтепродукты");
                Change(doc, welds, "224_0_0", "Пенообразователь");
                Change(doc, welds, "128_96_0", "ХимическиеРеагенты");
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