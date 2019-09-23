using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Gladkoe.Utilities;

using Newtonsoft.Json;

namespace Gladkoe.ParameterDataManipulations
{
    using System.Windows;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.ParameterDataManipulations.Models;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SaveParameters : IExternalCommand
    {
        private static IGetDataSetStrategy<string, List<Element>> dataSet;

        private static IGetRevitDataStrategy revitData;

        public static Document RevitDocument { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            RevitDocument = uidoc.Document;
            dataSet = new ParamManipulGetDataSet();
            revitData = new ParamManipulGetRvtData();

            try
            {
                SerializeData(RevitDocument);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Save parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void SerializeData(Document doc)
        {
            var sw = Stopwatch.StartNew();

            List<Element> elements = revitData.GetElements(doc).ToList();

            if (DuplicateValidator.ValidateDuplicates(elements))
            {
                return;
            }

            Dictionary<string, List<Element>> elementsWithUidNoDuplicates = GetElementsWithUidNoDuplicatesParams(elements);

            if (elementsWithUidNoDuplicates != null)
            {
                DataSet ds = dataSet.GetDataSet(elementsWithUidNoDuplicates);

                string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

                sw.Stop();

                if (ResultsHelper.WriteJsonFile(json))
                {
                    TaskDialog.Show(
                        "Parameter Export",
                        $"{elementsWithUidNoDuplicates.Count} categories and a total of {elementsWithUidNoDuplicates.Values.Sum(list => list.Count)} elements exported in {sw.Elapsed.TotalSeconds:F2} seconds.");
                }
            }
        }

        private static Dictionary<string, List<Element>> GetElementsWithUidNoDuplicatesParams(IEnumerable<Element> elements)
        {
            List<Element> enumerable = elements.ToList();
            List<Element> elementsWithUid = GetElementsWithUid(enumerable).ToList();

            if (elementsWithUid.Count != enumerable.Count)
            {
                throw new ArgumentException("Заполните у элементов параметр UID (параметр проекта)");
            }

            return elementsWithUid.Where(
                    e =>
                    {
                        var hasDuplicate = e.GetOrderedParameters()
                            .Where(par => par.IsShared)
                            .Select(i => new { Name = i.Definition.Name, GUID = i.GUID })
                            .GroupBy(i => i.Name, i => i.GUID)
                            .Any(i => i.Select(guid => guid).Count() > 1);
                        return !hasDuplicate;
                    })
                .GroupBy(e => e.Category.Name, e => e)
                .ToDictionary(e => e.Key, e => e.ToList());
        }

        private static IEnumerable<Element> GetElementsWithUid(IEnumerable<Element> elements)
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
                });
        }
    }
}