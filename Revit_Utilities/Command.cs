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

            Reference sel = uidoc.Selection.PickObject(ObjectType.Element);
            Element element = doc.GetElement(sel);

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change");

                // Change(doc, "0_153_255", "Азот");
                // Change(doc, "0_96_0", "Вода");
                // Change(doc, "255_220_112", "Газ");
                Change(doc, "192_192_192", "Дренаж", element);

                // Change(doc, "192_192_192", "Канализация");
                // Change(doc, "160_80_0", "Нефтепродукты");
                // Change(doc, "224_0_0", "Пенообразователь");
                // Change(doc, "128_96_0", "ХимическиеРеагенты");
                tran.Commit();
            }

            return Result.Succeeded;
        }

        private static void Change(Document doc, string color, string pipeType, Element element)
        {
            var filter = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"));

            ElementId material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals(color))?.Id;

            List<FamilyInstance> chemicalPipe = (from e in filter
                                                 from Connector connector in e.MEPModel.ConnectorManager.Connectors
                                                 from Connector reference in connector.AllRefs
                                                 where reference.Owner.Name.Contains(pipeType)
                                                 select e).ToList();

            List<Element> fittingConnectors = (from e in chemicalPipe
                                               from Connector connector in e.MEPModel.ConnectorManager.Connectors
                                               from Connector reference in connector.AllRefs
                                               where (reference.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                                                     && !reference.Owner.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные")
                                               select reference.Owner).ToList();

            foreach (Element connector in fittingConnectors)
            {
                Parameter p = connector.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals("МатериалФитинга"));
                p?.Set(material);
            }
        }

        private static void ChangeColor(Document doc, Element element)
        {
            var filter = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .SelectMany(i => i.MEPModel.ConnectorManager.Connectors.Cast<Connector>());

            var pipeFittingsDictionary = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .SelectMany(i => i.MEPModel.ConnectorManager.Connectors.Cast<Connector>().SelectMany(e => e.AllRefs.Cast<Connector>().ToList()))
                .Where(
                    e => e.Owner.Name.Contains("Азот") || e.Owner.Name.Contains("Вода") || e.Owner.Name.Contains("Газ") || e.Owner.Name.Contains("Дренаж")
                         || e.Owner.Name.Contains("Канализация") || e.Owner.Name.Contains("Нефтепродукты") || e.Owner.Name.Contains("Пенообразователь")
                         || e.Owner.Name.Contains("ХимическиеРеагенты"))
                .ToList();

            var elementsThatNeedToBeColored = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .SelectMany(i => i.MEPModel.ConnectorManager.Connectors.Cast<Connector>())
                .SelectMany(e => e.AllRefs.Cast<Connector>())
                .Where(
                    e => (e.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                         && !e.Owner.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные"))
                .GroupBy(e => e.Owner.Name, e => e)
                .ToDictionary(e => e.Key, e => e.ToList());

            int c = 0;

            // foreach (var item in pipeFittingsDictionary)
            // {
            // if (item.Key.Contains("Азот"))
            // {
            // ChangeColor(element, doc, item.Value, "0_153_255");
            // }
            // if (item.Key.Contains("Вода"))
            // {
            // ChangeColor(element, doc, item.Value, "0_96_0");
            // }
            // if (item.Key.Contains("Газ"))
            // {
            // ChangeColor(element, doc, item.Value, "255_220_112");
            // }
            // if (item.Key.Contains("Дренаж"))
            // {
            // ChangeColor(element, doc, item.Value, "192_192_192");
            // }
            // if (item.Key.Contains("Канализация"))
            // {
            // ChangeColor(element, doc, item.Value, "192_192_192");
            // }
            // if (item.Key.Contains("Нефтепродукты"))
            // {
            // ChangeColor(element, doc, item.Value, "160_80_0");
            // }
            // if (item.Key.Contains("Пенообразователь"))
            // {
            // ChangeColor(element, doc, item.Value, "224_0_0");
            // }
            // if (item.Key.Contains("ХимическиеРеагенты"))
            // {
            // ChangeColor(element, doc, item.Value, "128_96_0");
            // }
            // }
        }

        private static void ChangeColor(Document doc, List<Connector> items, string color)
        {
            ElementId material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals(color))?.Id;

            foreach (var connector in items)
            {
                foreach (Connector connectorManagerConnector in connector.ConnectorManager.Connectors)
                {
                    foreach (Connector reference in connectorManagerConnector.AllRefs)
                    {
                        // Parameter p = reference.Owner.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals("МатериалФитинга"));
                        // p?.Set(material);
                    }
                }
            }
        }
    }
}