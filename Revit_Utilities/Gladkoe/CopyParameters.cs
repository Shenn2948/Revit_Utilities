namespace Revit_Utilities.Gladkoe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Revit_Utilities.Gladkoe.LineSectionNumberFillParameter;
    using Revit_Utilities.Utilities;

    public static class CopyParameters
    {
        public static Document RevitDocument { get; private set; }

        public static UIDocument UiRevitDocument { get; private set; }

        public static void FillParams(Document doc, UIDocument uidoc)
        {
            RevitDocument = doc;
            UiRevitDocument = uidoc;

            try
            {
                FillParametersAction();

                // uidoc.Selection.SetElementIds(GetElements().Select(e => e.Id).ToList());
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction()
        {
            List<Element> elements = GetElements();

            using (Transaction tran = new Transaction(RevitDocument))
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
            int i = 0;

            foreach (Element element in elements)
            {
                Parameter resultParameter = GetParameter(element, "UID");

                resultParameter.Set(element.Id.IntegerValue.ToString());
                i++;
            }

            TaskDialog.Show("Info", $"Параметров заполнено {i}");
        }

        private static List<Element> GetElements()
        {
            return new FilteredElementCollector(RevitDocument)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(
                    new ElementMulticategoryFilter(
                        new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_PipeAccessory,
                            BuiltInCategory.OST_PipeCurves,
                            BuiltInCategory.OST_MechanicalEquipment,
                            BuiltInCategory.OST_PipeFitting,
                            BuiltInCategory.OST_FlexPipeCurves,
                            BuiltInCategory.OST_PlumbingFixtures
                        }))
                .ToElements()
                .Where(
                    delegate(Element e)
                    {
                        Parameter volume = e.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);

                        if ((e is FamilyInstance fs && (fs.SuperComponent != null)) || ((volume != null) && !volume.HasValue))
                        {
                            return false;
                        }

                        return true;
                    })
                .ToList();
        }
    }
}