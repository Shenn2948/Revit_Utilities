namespace Revit_Utilities.VM
{
    using System.Collections.Generic;
    using System.Linq;

    using Autodesk.Revit.DB;

    public class Recolorer
    {
        public static void ChangeColor(Document doc)
        {
            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change");

                // Change(doc, welds, "0_153_255", "Азот");
                // Change(doc, welds, "0_96_0", "Вода");
                // Change(doc, welds, "255_220_112", "Газ");
                // Change(doc, welds, "192_192_192", "Дренаж");
                //
                // Change(doc, welds, "192_192_192", "Канализация");
                // Change(doc, welds, "160_80_0", "Нефтепродукты");
                // Change(doc, welds, "224_0_0", "Пенообразователь");
                // Change(doc, welds, "128_96_0", "ХимическиеРеагенты");

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

        private static IEnumerable<FamilyInstance> GetPipeType(IEnumerable<FamilyInstance> welds, string pipeType)
        {
            return from e in welds
                   from Connector connector in e.MEPModel.ConnectorManager.Connectors
                   from Connector reference in connector.AllRefs
                   where reference.Owner.Name.Contains(pipeType)
                   select e;
        }

        private static IEnumerable<Element> GetElementsToRecolor(IEnumerable<FamilyInstance> pipes)
        {
            return from e in pipes
                   from Connector connector in e.MEPModel.ConnectorManager.Connectors
                   from Connector reference in connector.AllRefs
                   where (reference.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                         && !reference.Owner.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные")
                   select reference.Owner;
        }

        private static void Change(Document doc, string pipeType)
        {
            var welds = GetWeld(doc).ToList();
            IEnumerable<FamilyInstance> pipeTypes = null;
            ElementId material = null;
            switch (pipeType)
            {
                case "Азот":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("0_153_255"))?.Id;
                    pipeTypes = GetPipeType(welds, pipeType);
                    break;
                case "Вода":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("0_96_0"))?.Id;
                    break;
                case "Газ":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("255_220_112"))?.Id;
                    break;
                case "Дренаж":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("192_192_192"))?.Id;
                    break;
                case "Канализация":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("192_192_192"))?.Id;
                    break;
                case "Нефтепродукты":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("160_80_0"))?.Id;
                    break;
                case "Пенообразователь":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("224_0_0"))?.Id;
                    break;
                case "ХимическиеРеагенты":
                    material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("128_96_0"))?.Id;
                    break;
            }

            IEnumerable<Element> connectorsToRecolor = GetElementsToRecolor(pipeTypes);

            foreach (Element element in connectorsToRecolor)
            {
                Parameter p = element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals("МатериалФитинга"));
                p?.Set(material);
            }
        }

        private static void ChangeColorOneQuery(Document doc)
        {
            var chemicalPipe = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .SelectMany(e => e.MEPModel.ConnectorManager.Connectors.Cast<Connector>(), (e, connector) => (e, connector))
                .SelectMany(t => t.connector.AllRefs.Cast<Connector>(), (familyInstance, reference) => (familyInstance, reference))
                .Where(
                    t => t.reference.Owner.Name.Contains("Азот") || t.reference.Owner.Name.Contains("Вода") || t.reference.Owner.Name.Contains("Газ")
                         || t.reference.Owner.Name.Contains("Дренаж") || t.reference.Owner.Name.Contains("Канализация") || t.reference.Owner.Name.Contains("Нефтепродукты")
                         || t.reference.Owner.Name.Contains("Пенообразователь") || t.reference.Owner.Name.Contains("ХимическиеРеагенты"))
                .Select((t, f) => (t.familyInstance.e, t.reference.Owner))
                .SelectMany(e => e.e.MEPModel.ConnectorManager.Connectors.Cast<Connector>(), (e, connector) => (familyInstance: e.e, connector, Owner: e.Owner))
                .SelectMany(t => t.connector.AllRefs.Cast<Connector>(), (t, reference) => (t, reference))
                .Where(
                    t => (t.reference.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting)
                         && !t.reference.Owner.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные"))
                .GroupBy(e => e.t.Owner.Name, e => e.reference.Owner)
                .ToDictionary(e => e.Key, e => e.ToList());

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change");

                foreach (KeyValuePair<string, List<Element>> valuePair in chemicalPipe)
                {
                    if (valuePair.Key.Contains("Азот"))
                    {
                        ElementId material = new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("0_153_255"))?.Id;
                        foreach (Element element in valuePair.Value)
                        {
                            Parameter p = element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals("МатериалФитинга"));
                            p?.Set(material);
                        }
                    }
                }

                tran.Commit();
            }
        }
    }
}