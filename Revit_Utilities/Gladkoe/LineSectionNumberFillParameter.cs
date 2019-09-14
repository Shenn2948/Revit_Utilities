namespace Revit_Utilities.Gladkoe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    public class LineSectionNumberFillParameter
    {
        public static void FillParams(Document doc, UIDocument uidoc)
        {
            try
            {
                FillParametersAction(doc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction(Document doc)
        {
            List<Element> welds = GetElements(doc);

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Fill parameters");

                SetParameters(doc, welds);

                tran.Commit();
            }

            TaskDialog.Show("Fill parameters", "Параметры заполнены");
        }

        private static Parameter GetParameter(Element element, string parameterName)
        {
            return element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals(parameterName)) ?? throw new ArgumentNullException(
                       parameterName,
                       $"Проблема в нахождении параметра \"{parameterName}\", проверьте наименования параметров");
        }

        private static void SetParameters(Document doc, List<Element> elements)
        {
            foreach (Element element in elements)
            {
                Parameter p = GetParameter(element, "Концевое условие");
                p.Set("Сварной шов");

                p = GetParameter(element, "Концевое условие 2");
                p.Set("Сварной шов");

                if (element is FamilyInstance fs && (fs.Symbol.FamilyName.Contains("Тройник") || fs.Symbol.FamilyName.Contains("Фильтр")))
                {
                    p = GetParameter(element, "Концевое условие 3");
                    p.Set("Сварной шов");

                    if (fs.Symbol.FamilyName.Contains("Крестовина"))
                    {
                        p = GetParameter(element, "Концевое условие 3");
                        p.Set("Сварной шов");
                        p = GetParameter(element, "Концевое условие 4");
                        p.Set("Сварной шов");
                    }
                }
            }
        }

        private static List<Element> GetElements(Document doc)
        {
            return new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory> { BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeAccessory }))
                .ToElements()
                .Where(e => e is FamilyInstance)
                .Select(e => e)
                .ToList();
        }
    }
}