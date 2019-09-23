#region Namespaces

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
#endregion

namespace Gladkoe.ParameterDataManipulations
{
    using System.Windows;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.ParameterDataManipulations.Models;

    using Application = Autodesk.Revit.ApplicationServices.Application;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LoadParameters : IExternalCommand
    {
        private static IGetRevitDataStrategy revitData;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            revitData = new ParamManipulGetRvtData();
            try
            {
                Deserialize(doc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Load parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void Deserialize(Document doc)
        {
            string jsonFilePath = ResultsHelper.GetOpenJsonFilePath();

            if (jsonFilePath == null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            List<Element> elements = revitData.GetElements(doc).ToList();

            if (DuplicateValidator.ValidateDuplicates(elements))
            {
                return;
            }

            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(jsonFilePath));

            var (parametersCount, elementsCount) = SetParameterValuesFromDataSet(doc, dataSet, elements);

            sw.Stop();

            if (parametersCount != 0)
            {
                TaskDialog.Show("Parameter Export", $"{parametersCount} parameters and a total of {elementsCount} elements proceed in {sw.Elapsed.TotalSeconds:F2} seconds.");
            }
        }

        private static (int parametersCount, int elementsCount) SetParameterValuesFromDataSet(Document doc, DataSet dataSet, List<Element> elements)
        {
            Dictionary<int, List<Parameter>> elementParameters = GetElementParameters(elements);

            int count = 0;
            if (dataSet != null)
            {
                var groupedByIdData = GetParameterDataFromDataSet(dataSet);

                using (var tran = new Transaction(doc))
                {
                    tran.Start("Перенос параметров из JSON");

                    foreach (var element in elementParameters)
                    {
                        foreach (var parameter in element.Value)
                        {
                            foreach (var paramData in groupedByIdData[element.Key].Where(paramData => parameter.GUID.ToString().Equals(paramData.ParamName)))
                            {
                                parameter.SetObjectParameterValue(paramData.ParamValue);
                                count++;
                            }
                        }
                    }

                    tran.Commit();
                }
            }

            return (count, elementParameters.Count);
        }

        private static Dictionary<int, List<(string ParamName, object ParamValue)>> GetParameterDataFromDataSet(DataSet dataSet)
        {
            return dataSet.Tables.Cast<DataTable>()
                .SelectMany(e => e.AsEnumerable())
                .Select(
                    p => new
                    {
                        UID = p.Field<string>("f776cdec-f4d6-491d-a342-ef50f8f09d4e").ToInt32(),
                        ParamData = p.Table.Columns.Cast<DataColumn>().Select(c => (ParamName: c.ColumnName, ParamValue: p[c]))
                    })
                .GroupBy(p => p.UID, p => p.ParamData)
                .OrderBy(i => i.Key)
                .ToDictionary(r => r.Key, r => r.SelectMany(p => p).ToList());
        }

        private static Dictionary<int, List<Parameter>> GetElementParameters(IEnumerable<Element> elements)
        {
            return elements.Where(
                    e =>
                    {
                        if (e.ParametersMap.Contains("UID"))
                        {
                            Parameter p = e.ParametersMap.get_Item("UID");
                            return p.HasValue && !string.IsNullOrEmpty(p.AsString());
                        }

                        return false;
                    })
                .GroupBy(e => e.LookupParameter("UID").AsString().ToInt32(), e => e)
                .OrderBy(i => i.Key)
                .ToDictionary(
                    e => e.Key,
                    e => e.FirstOrDefault()?.ParametersMap.Cast<Parameter>().Where(p => !p.IsReadOnly && (p.StorageType != StorageType.ElementId) && p.IsShared).ToList());
        }
    }
}