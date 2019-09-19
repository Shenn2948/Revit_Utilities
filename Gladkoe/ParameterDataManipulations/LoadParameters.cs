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

// ReSharper disable StyleCop.SA1108
//// ReSharper disable StyleCop.SA1512
// ReSharper disable StyleCop.SA1515
namespace Gladkoe.ParameterDataManipulations
{
    using System.Windows.Forms;

    using Application = Autodesk.Revit.ApplicationServices.Application;

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
            {
                // try
                DeserializeFromJson2(doc);
            }
            {
                // catch (Exception e)
                // TaskDialog.Show("Fill parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void DeserializeFromJson(Document doc)
        {
            var sw = Stopwatch.StartNew();

            var elements = GetElements(doc)
                .Where(e => e.LookupParameter("UID") != null)
                .GroupBy(e => e.LookupParameter("UID").AsString(), e => e)
                .ToDictionary(e => e.Key, e => e.FirstOrDefault());

            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(ResultsHelper.GetOpenJsonFilePath()));

            if (dataSet != null)
            {
                var groupedByIdData = dataSet.Tables.Cast<DataTable>()
                    .SelectMany(e => e.AsEnumerable())
                    .GroupBy(p => p.Field<string>("f776cdec-f4d6-491d-a342-ef50f8f09d4e"))
                    .ToDictionary(r => r.Key, r => r.SelectMany(p => p.Table.Columns.Cast<DataColumn>().Select(c => new { Name = c.ColumnName, ParamValue = p[c] })));

                using (Transaction tran = new Transaction(doc))
                {
                    tran.Start("Перенос параметров из JSON");
                    foreach (var element in elements)
                    {
                        foreach (var parameter in element.Value.GetOrderedParameters().Where(p => !p.IsReadOnly && (p.StorageType != StorageType.ElementId) && p.IsShared))
                        {
                            foreach (var paramData in groupedByIdData[element.Key])
                            {
                                if (parameter.GUID.ToString().Equals(paramData.Name))
                                {
                                    parameter.SetObjectParameterValue(paramData.ParamValue);
                                }
                            }
                        }
                    }

                    tran.Commit();
                }
            }

            sw.Stop();

            // if ()
            // {
            // TaskDialog.Show(
            // "Parameter Export",
            // $"{elements.Count} categories and a total of {elements.Values.Sum(list => list.Count)} elements exported in {sw.Elapsed.TotalSeconds:F2} seconds.");
            // }
        }

        private static void DeserializeFromJson2(Document doc)
        {
            var elements = GetElements(doc)
                .Where(e => e.LookupParameter("UID") != null)
                .SelectMany(e => e.GetOrderedParameters(), (element, parameter) => (element, parameter))
                .Where(p => p.parameter.IsShared)
                .Select(i => new { GUID = i.parameter.GUID.ToString(), UID = i.element.LookupParameter("UID").AsString().ToInt32(), Parameter = i.parameter })
                .OrderBy(i => i.UID)
                .GroupBy(i => i.UID, arg => (GUID: arg.GUID, Parameter: arg.Parameter)).ToDictionary();

            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(ResultsHelper.GetOpenJsonFilePath()));

            var groupedByIdData = dataSet.Tables.Cast<DataTable>()
                .SelectMany(e => e.AsEnumerable())
                .GroupBy(p => p.Field<string>("f776cdec-f4d6-491d-a342-ef50f8f09d4e"))
                .ToDictionary(r => r.Key, r => r.SelectMany(p => p.Table.Columns.Cast<DataColumn>().Select(c => new { Name = c.ColumnName, ParamValue = p[c] })));

            var groupedByIdData2 = dataSet.Tables.Cast<DataTable>()
                .SelectMany(e => e.AsEnumerable())
                .SelectMany(
                    p => p.Table.Columns.Cast<DataColumn>()
                        .Select(c => new { Name = c.ColumnName, ParamValue = p[c], GUID = p.Field<string>("f776cdec-f4d6-491d-a342-ef50f8f09d4e") }))
                .GroupBy(p => p.Name, (p) => (GUID: p.GUID, ParamVal: p.ParamValue))
                .ToDictionary(e => e.Key, e => e.ToList());

            // .GroupBy(p => p, p => p.Table.Columns.Cast<DataColumn>().Select(c => new { Name = c.ColumnName, ParamValue = p[c] }))
            // .ToDictionary();
            // .GroupBy(p => p.Field<string>("f776cdec-f4d6-491d-a342-ef50f8f09d4e"))
            // .ToDictionary(r => r.Key, r => r.SelectMany(p => p.Table.Columns.Cast<DataColumn>().Select(c => new { Name = c.ColumnName, ParamValue = p[c] })));
            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Перенос параметров из JSON");
                {
                    // foreach (var data in groupedByIdData)
                    // foreach (var parameter in element.Value.GetOrderedParameters().Where(p => !p.IsReadOnly && (p.StorageType != StorageType.ElementId) && p.IsShared))
                    // {
                    // foreach (var paramData in groupedByIdData[element.Key])
                    // {
                    // if (parameter.GUID.ToString().Equals(paramData.Name))
                    // {
                    // parameter.SetObjectParameterValue(paramData.ParamValue);
                    // }
                    // }
                    // }
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
    }
}