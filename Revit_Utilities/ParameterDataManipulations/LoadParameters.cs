using Application = Autodesk.Revit.ApplicationServices.Application;

// ReSharper disable StyleCop.SA1108
//// ReSharper disable StyleCop.SA1512
// ReSharper disable StyleCop.SA1515
namespace Gladkoe.ParameterDataManipulations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Gladkoe.Utilities;

    using Newtonsoft.Json;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LoadParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            try
            {
                // SerializeDataToJson(doc);
                CopyParameters(doc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void SerializeDataToJson(Document doc)
        {
            var sw = Stopwatch.StartNew();

            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(ResultsHelper.GetOpenJsonFilePath()));

            var groupedByIdData = dataSet.Tables.Cast<DataTable>()
                .SelectMany(e => e.AsEnumerable())
                .GroupBy(p => p.Field<string>("UID"))
                .ToDictionary(r => r.Key, r => r.SelectMany(p => p.Table.Columns.Cast<DataColumn>().Select(c => new { Name = c.ColumnName, Value = p[c] })));

            foreach (var item in groupedByIdData)
            {
                // Element e = doc.GetElement(new ElementId(Convert.ToInt32(item.Key)));
            }

            sw.Stop();

            // if (ResultsHelper.WriteJsonFile(json))
            // {
            // TaskDialog.Show(
            // "Parameter Export",
            // $"{elements.Count} categories and a total of {elements.Values.Sum(list => list.Count)} elements exported in {sw.Elapsed.TotalSeconds:F2} seconds.");
            // }
        }

        private static void CopyParameters(Document doc)
        {
            var elements = GetElements(doc).Where(e => GetParameter(e, "UID") != null);
            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(ResultsHelper.GetOpenJsonFilePath()));

            var groupedByIdData = dataSet.Tables.Cast<DataTable>()
                .SelectMany(e => e.AsEnumerable())
                .GroupBy(p => p.Field<string>("UID"))
                .ToDictionary(r => r.Key, r => r.SelectMany(p => p.Table.Columns.Cast<DataColumn>().Select(c => new { Name = c.ColumnName, Value = p[c] })));

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Перенос параметров из JSON");

                foreach (Element element in elements)
                {
                    foreach (Parameter parameter in element.GetOrderedParameters())
                    {
                    }

                    foreach (var item in groupedByIdData)
                    {
                        Parameter uid = GetParameter(element, "UID");
                        if (uid.GetStringParameterValue().Equals("1933405"))
                        {
                            // item.Key
                            foreach (var parameter in item.Value)
                            {
                                if ((parameter.Value != null) && (parameter.Value.ToString() != string.Empty) && parameter.Name.Equals("Автор"))
                                {
                                    Parameter resultParameter = GetParameter(element, parameter.Name);
                                    resultParameter?.SetValueString(parameter.Value.ToString());
                                }
                            }
                        }
                    }
                }

                tran.Commit();
            }
        }

        private static List<Element> GetElements(Document doc)
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
                .Where(e => e.GetOrderedParameters().FirstOrDefault(p => p.Definition.Name.Equals("UID")) != null)
                .ToList();
        }

        private static Parameter GetParameter(Element element, string parameterName)
        {
            return element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals(parameterName));
        }
    }
}