namespace Revit_Utilities.ParameterDataManipulations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Newtonsoft.Json;

    using Revit_Utilities.Utilities;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SaveParameters : IExternalCommand
    {
        public static Document RevitDocument { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            RevitDocument = uidoc.Document;

            try
            {
                SerializeDataToJson();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Save parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void SerializeDataToJson()
        {
            var sw = Stopwatch.StartNew();

            Dictionary<string, List<Element>> elements = GetElements(RevitDocument);
            DataSet ds = GetDataSet(elements);

            string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

            sw.Stop();

            if (ResultsHelper.WriteJsonFile(json))
            {
                TaskDialog.Show(
                    "Parameter Export",
                    $"{elements.Count} categories and a total of {elements.Values.Sum(list => list.Count)} elements exported in {sw.Elapsed.TotalSeconds:F2} seconds.");
            }
        }

        private static DataSet GetDataSet(Dictionary<string, List<Element>> sortedElements)
        {
            var ds = new DataSet();
            foreach (var element in sortedElements)
            {
                ds.Tables.Add(GetTable(element));
            }

            return ds;
        }

        private static DataTable GetTable(KeyValuePair<string, List<Element>> element)
        {
            var table = new DataTable { TableName = element.Key };

            table.Columns.Add("ID");
            foreach (Element item in element.Value)
            {
                DataRow row = table.NewRow();
                row["ID"] = item.Id.IntegerValue.ToString();
                foreach (Parameter parameter in item.GetOrderedParameters()
                    .Where(p => (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES) && p.IsShared))
                {
                    if (!table.Columns.Contains(parameter.Definition.Name))
                    {
                        table.Columns.Add(parameter.Definition.Name);
                    }

                    row[parameter.Definition.Name] = parameter.GetStringParameterValue();
                }

                table.Rows.Add(row);
            }

            return table;
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
                    delegate(Element e)
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