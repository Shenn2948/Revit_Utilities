namespace Gladkoe.Gladkoe_Recolor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    public static class Recolorer
    {
        public static void ChangeColor(Document doc)
        {
            try
            {
                Change(doc);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Recolor", ex.Message);
            }
        }

        private static void Change(Document doc)
        {
            var sw = Stopwatch.StartNew();

            List<FamilyInstance> welds = GetWeld(doc) ?? throw new ArgumentException("Проблема в нахождении сварки, проверьте наименования семейств");

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change");

                ChangeColor(doc, welds, "Азот_");
                ChangeColor(doc, welds, "Вода_");
                ChangeColor(doc, welds, "Газ_");
                ChangeColor(doc, welds, "Дренаж_");
                ChangeColor(doc, welds, "Канализация_");
                ChangeColor(doc, welds, "Нефтепродукты_");
                ChangeColor(doc, welds, "Пенообразователь_");
                ChangeColor(doc, welds, "ХимическиеРеагенты_");

                tran.Commit();
            }

            sw.Stop();

            TaskDialog.Show("Parameter Export", $"Proceed " + $"in {sw.Elapsed.TotalSeconds:F2} seconds.");
        }

        private static void ChangeColor(Document doc, IEnumerable<FamilyInstance> welds, string pipeType)
        {
            ElementId material = GetMaterialId(doc, pipeType) ?? throw new ArgumentException("Проблема в нахождении материалов, проверьте наименования материалов");

            IEnumerable<FamilyInstance> pipeTypes = GetPipeType(welds, pipeType) ?? throw new ArgumentException("Проблема в нахождении типов труб, проверьте наименования семейств");
            IEnumerable<Element> connectorsToRecolor = GetElementsToRecolor(pipeTypes) ?? throw new ArgumentException("Проблема в нахождении коннекторов, проверьте наименования семейств");

            SetColor(connectorsToRecolor, material);
        }

        private static List<FamilyInstance> GetWeld(Document doc)
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

        private static IEnumerable<FamilyInstance> GetPipeType(IEnumerable<FamilyInstance> welds, string pipeType)
        {
            return from e in welds
                   from Connector connector in e.MEPModel.ConnectorManager.Connectors
                   from Connector reference in connector.AllRefs
                   where reference.Owner.Name.StartsWith(pipeType)
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

        private static ElementId GetMaterialId(Document doc, string pipeType)
        {
            switch (pipeType)
            {
                case "Азот":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("0_153_255"))?.Id;
                case "Вода":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("0_96_0"))?.Id;
                case "Газ":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("255_220_112"))?.Id;
                case "Дренаж":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("192_192_192"))?.Id;
                case "Канализация":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("192_192_192"))?.Id;
                case "Нефтепродукты":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("160_80_0"))?.Id;
                case "Пенообразователь":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("224_0_0"))?.Id;
                case "ХимическиеРеагенты":
                    return new FilteredElementCollector(doc).OfClass(typeof(Material)).FirstOrDefault(m => m.Name.Equals("128_96_0"))?.Id;
            }

            return null;
        }

        private static void SetColor(IEnumerable<Element> elements, ElementId materialId)
        {
            foreach (Element element in elements)
            {
                Parameter p = element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals("МатериалФитинга"))
                              ?? throw new ArgumentNullException(
                                  nameof(p),
                                  "Проблема в нахождении параметра \"МатериалФитинга\", проверьте наименования параметров");
                p.Set(materialId);
            }
        }
    }
}