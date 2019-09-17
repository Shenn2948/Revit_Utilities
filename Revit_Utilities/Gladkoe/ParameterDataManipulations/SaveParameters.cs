namespace Revit_Utilities.Gladkoe.ParameterDataManipulations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.UI;

    using Revit_Utilities.Utilities;

    public class SaveParameters
    {
        public static Document RevitDocument { get; private set; }

        public static void FillParams(Document doc, UIDocument uidoc)
        {
            RevitDocument = doc;

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
            var elements = GetElements(RevitDocument);

            using (Transaction tran = new Transaction(RevitDocument))
            {
                tran.Start("Заполнить номер участка линии");

                // SetParameters(elements);
                tran.Commit();
            }
        }

        private static void ExportElementParameters(List<Parameter> pickedDefinitions, Dictionary<string, List<Element>> sortedElements)
        {
            string savingPath = ResultsHelper.GetSaveFilePath();
            if (savingPath == string.Empty)
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            using (var workbook = new XLWorkbook())
            {
                DataSet ds = GetDataSet(sortedElements, pickedDefinitions);
                workbook.Worksheets.Add(ds);
                workbook.SaveAs(savingPath);
            }

            sw.Stop();

            TaskDialog.Show(
                "Parameter Export",
                $"{sortedElements.Count} categories and a total of {sortedElements.Values.Sum(list => list.Count)} elements exported in {sw.Elapsed.TotalSeconds:F2} seconds.");
        }

        private static DataSet GetDataSet(Dictionary<string, List<Element>> sortedElements, List<Parameter> pickedDefinitions)
        {
            var ds = new DataSet();
            foreach (var element in sortedElements)
            {
                ds.Tables.Add(GetTable(element, pickedDefinitions));
            }

            return ds;
        }

        private static DataTable GetTable(KeyValuePair<string, List<Element>> element, List<Parameter> pickedDefinitions)
        {
            var table = new DataTable { TableName = element.Key };

            table.Columns.Add("ID");
            foreach (Element item in element.Value)
            {
                DataRow row = table.NewRow();
                row["ID"] = item.Id.IntegerValue.ToString();
                foreach (Parameter parameter in item.GetOrderedParameters().Where(p => p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES && p.IsShared))
                {
                    table.Columns.Add(parameter.Definition.Name);

                    row[parameter.Definition.Name] = parameter.GetStringParameterValue();
                }

                table.Rows.Add(row);
            }

            return table;
        }

        private static Dictionary<string, List<Parameter>> FindParameterCategories(Dictionary<string, List<Element>> elements)
        {
            return elements.Values.SelectMany(e => e)
                .SelectMany(e => e.GetOrderedParameters())
                .GroupBy(p => p.Definition.Name)
                .Select(p => p.First())
                .OrderBy(p => p.Definition.Name)
                .GroupBy(p => p.Definition.ParameterGroup, p => p)
                .OrderBy(grp => LabelUtils.GetLabelFor(grp.Key))
                .ToDictionary(e => LabelUtils.GetLabelFor(e.Key), e => e.ToList());
        }

        private static Dictionary<string, List<Element>> GetElements(Document doc)
        {
            return new FilteredElementCollector(doc).WhereElementIsNotElementType()
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
                .Where(e => e.Category != null)
                .Where(
                    delegate (Element e)
                    {
                        Parameter volume = e.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);

                        if ((e is FamilyInstance fs && (fs.SuperComponent != null)) || ((volume != null) && !volume.HasValue))
                        {
                            return false;
                        }

                        return true;
                    })
                .GroupBy(e => e.Category.Name, e => e)
                .ToDictionary(e => e.Key, e => e.ToList());
        }
    }
}