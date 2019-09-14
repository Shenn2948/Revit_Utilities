using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Revit_Utilities.Utilities;

namespace Revit_Utilities.Gladkoe.LineSectionNumberFillParameter
{
    public static class LineSectionNumberFillParameter
    {
        private static Document revitDocument;

        private static UIDocument uiRevitDocument;

        public static void FillParams(Document doc, UIDocument uidoc)
        {
            revitDocument = doc;
            uiRevitDocument = uidoc;

            try
            {
                FillParametersAction();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction()
        {
            List<Element> elements = GetElements();

            using (Transaction tran = new Transaction(revitDocument))
            {
                tran.Start("Заполнить номер участка линии");

                SetParameters(elements);

                tran.Commit();
            }
        }

        private static Parameter GetParameter(Element element, string parameterName)
        {
            return element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals(parameterName))
                   ?? throw new ArgumentException($"Проблема в нахождении параметра \"{parameterName}\", проверьте верность наименования и наличие параметров");
        }

        private static void SetParameters(List<Element> elements)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (Element element in elements)
            {
                Parameter resultParameter = GetParameter(element, "Номер участка линии");

                string s1 = GetParameter(element, "№ поз. по ГП").GetParameterValue();
                string s2 = GetParameter(element, "Шифр продукта").GetParameterValue();
                string s3 = GetParameter(element, "Номер по технологической схеме").GetParameterValue();
                string s4 = GetParameter(element, "Условный диаметр").GetParameterValue().Split(' ').FirstOrDefault();
                string s5 = GetParameter(element, "Условное давление").GetParameterValue().Split(' ').FirstOrDefault();
                string s6 = GetParameter(element, "Конструкция трубопровода").GetParameterValue();

                if ((s1 != string.Empty) && (s2 != string.Empty) && (s3 != string.Empty) && (s4 != string.Empty) && (s5 != string.Empty) && (s6 != string.Empty))
                {
                    resultParameter.Set($"{s1}-{s2}-{s3}-{s4}-{s5}-{s6}");
                    i++;
                    continue;
                }

                if ((s1 != string.Empty) && (s2 != string.Empty) && (s3 == string.Empty) && (s4 != string.Empty) && (s5 != string.Empty) && (s6 != string.Empty))
                {
                    resultParameter.Set($"{s1}-{s2}-{s4}-{s5}-{s6}");
                    i++;
                }
                else if ((s1 == string.Empty) || (s2 == string.Empty) || (s4 == string.Empty) || (s5 == string.Empty) || (s6 == string.Empty))
                {
                    sb.Append(
                        $"element ID: {element.Id.IntegerValue}, не заполнены: "
                        + $"{(s1 != string.Empty ? string.Empty : GetParameter(element, "№ поз. по ГП").Definition.Name + ",")}"
                        + $"{(s2 != string.Empty ? string.Empty : GetParameter(element, "Шифр продукта").Definition.Name + ",")}"
                        + $"{(s4 != string.Empty ? string.Empty : GetParameter(element, "Условный диаметр").Definition.Name + ",")}"
                        + $"{(s5 != string.Empty ? string.Empty : GetParameter(element, "Условное давление").Definition.Name + ",")}"
                        + $"{(s6 != string.Empty ? string.Empty : GetParameter(element, "Конструкция трубопровода").Definition.Name)}");
                    sb.AppendLine();
                }
            }

            if (sb.Length != 0)
            {
                ResultWindow window = new ResultWindow(sb);
                window.ShowDialog();
            }
            else
            {
                TaskDialog.Show("Fill parameters", $"{i} параметров заполнено");
            }
        }

        private static List<Element> GetElements()
        {
            return new FilteredElementCollector(revitDocument).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(
                    new ElementMulticategoryFilter(
                        new List<BuiltInCategory> { BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeAccessory }))
                .ToElements()
                .ToList();
        }

        private static void FillParametersTestAction()
        {
            Reference pickedObj = uiRevitDocument.Selection.PickObject(ObjectType.Element, new ElementsOfClassSelectionFilter<Pipe>(), "Select pipe");
            var sb = new StringBuilder();
            var stringBuilderForIds = new StringBuilder();

            using (var tran = new Transaction(revitDocument))
            {
                tran.Start("Fill parameters");

                Element element = revitDocument.GetElement(pickedObj.ElementId);

                Parameter resultParameter = GetParameter(element, "Номер участка линии");

                string s1 = GetParameter(element, "№ поз. по ГП").GetParameterValue();
                string s2 = GetParameter(element, "Шифр продукта").GetParameterValue();
                string s3 = GetParameter(element, "Номер по технологической схеме").GetParameterValue();
                string s4 = GetParameter(element, "Условный диаметр").GetParameterValue().Split(' ').FirstOrDefault();
                string s5 = GetParameter(element, "Условное давление").GetParameterValue().Split(' ').FirstOrDefault();
                string s6 = GetParameter(element, "Конструкция трубопровода").GetParameterValue();

                if ((s1 != string.Empty) && (s2 != string.Empty) && (s3 != string.Empty) && (s4 != string.Empty) && (s5 != string.Empty) && (s6 != string.Empty))
                {
                    sb.Append($"{s1}-{s2}-{s3}-{s4}-{s5}-{s6}");
                    resultParameter.Set(sb.ToString());
                }

                if ((s1 != string.Empty) && (s2 != string.Empty) && (s3 == string.Empty) && (s4 != string.Empty) && (s5 != string.Empty) && (s6 != string.Empty))
                {
                    sb.Append($"{s1}-{s2}-{s4}-{s5}-{s6}");
                    resultParameter.Set(sb.ToString());
                }
                else if ((s1 == string.Empty) || (s2 == string.Empty) || (s4 == string.Empty) || (s5 == string.Empty) || (s6 == string.Empty))
                {
                    stringBuilderForIds.Append(
                        $"element ID: {element.Id.IntegerValue}, не заполнены: "
                        + $"{(s1 != string.Empty ? string.Empty : GetParameter(element, "№ поз. по ГП").Definition.Name + ",")}"
                        + $"{(s2 != string.Empty ? string.Empty : GetParameter(element, "Шифр продукта").Definition.Name + ",")}"
                        + $"{(s4 != string.Empty ? string.Empty : GetParameter(element, "Условный диаметр").Definition.Name + ",")}"
                        + $"{(s5 != string.Empty ? string.Empty : GetParameter(element, "Условное давление").Definition.Name + ",")}"
                        + $"{(s6 != string.Empty ? string.Empty : GetParameter(element, "Конструкция трубопровода").Definition.Name)}");
                    stringBuilderForIds.AppendLine();
                }

                tran.Commit();
            }

            TaskDialog.Show("Fill parameters", stringBuilderForIds.Length != 0 ? $"{stringBuilderForIds}" : "Параметры заполнены");
        }
    }
}