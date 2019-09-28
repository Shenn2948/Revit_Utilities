namespace Gladkoe.FillingParameters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Autodesk.Revit.ApplicationServices;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Gladkoe.Utilities;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyParametersFromPipe : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            {
                // try
                FillParametersAction(doc);
            }
            {
                // catch (Exception e)
                // TaskDialog.Show("Recolor", e.Message);
            }

            return Result.Succeeded;
        }

        private static void FillParametersAction(Document doc)
        {
            var sw = Stopwatch.StartNew();

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Скопировать параметры из труб в коннекторы и арматуру");
                SetParameters(doc);
                tran.Commit();
            }

            sw.Stop();

            TaskDialog.Show("Заполнение параметров", $"Параметры заполнены " + $"за {sw.Elapsed.TotalSeconds:F2} секунд.");
        }

        private static void SetParameters(Document doc)
        {
            var welds = GetWelds(doc);
            var elements = GetWeldsData(welds);

            foreach (KeyValuePair<FamilyInstance, (Element, Element)> weld in elements)
            {
                Dictionary<string, Parameter> pipeParameters = weld.Value.Item1?.ParametersMap.Cast<Parameter>()
                    .Where(p => p.IsShared && (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES))
                    .GroupBy(p => p.Definition.Name, p => p)
                    .OrderBy(p => p.Key)
                    .ToDictionary(p => p.Key, p => p.FirstOrDefault());

                Dictionary<string, Parameter> weldResultParameters = weld.Key.ParametersMap.Cast<Parameter>()
                    .Where(p => p.IsShared && (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES) && !p.IsReadOnly)
                    .GroupBy(p => p.Definition.Name, p => p)
                    .OrderBy(p => p.Key)
                    .ToDictionary(p => p.Key, p => p.FirstOrDefault());

                Dictionary<string, Parameter> fitingResultParameters = weld.Value.Item2?.ParametersMap.Cast<Parameter>()
                    .Where(p => p.IsShared && (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES) && !p.IsReadOnly)
                    .GroupBy(p => p.Definition.Name, p => p)
                    .OrderBy(p => p.Key)
                    .ToDictionary(p => p.Key, p => p.FirstOrDefault());

                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Давление рабочее", "Давление");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Давление гидравлич. испытания на прочн.", "Давление гидравлич. испытания на прочн.");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Давление доп. пневмоиспытания на герм.", "Давление доп. пневмоиспытания на герм.");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Давление испытательное", "Давление испытательное");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Давление рабочее", "Давление рабочее");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Класс среды", "Класс среды");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Наименование продукта", "Наименование продукта");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Шифр продукта", "Шифр продукта");
                SetValue(pipeParameters, weldResultParameters, fitingResultParameters, "Номер участка линии", "Номер участка линии");
            }
        }

        private static void SetValue(
            Dictionary<string, Parameter> pipeParameters,
            Dictionary<string, Parameter> weldResultParameters,
            Dictionary<string, Parameter> fitingResultParameters,
            string from,
            string to)
        {
            if ((pipeParameters != null) && pipeParameters.ContainsKey(from))
            {
                if (weldResultParameters.ContainsKey(to))
                {
                    weldResultParameters[to].SetParameterValue(pipeParameters[from]);
                }

                if ((fitingResultParameters != null) && fitingResultParameters.ContainsKey(to))
                {
                    fitingResultParameters[to].SetParameterValue(pipeParameters[from]);
                }
            }
        }

        private static List<FamilyInstance> GetWelds(Document doc)
        {
            return new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .ToList();
        }

        private static Dictionary<FamilyInstance, (Element, Element)> GetWeldsData(IEnumerable<FamilyInstance> welds)
        {
            return welds.SelectMany(e => e.MEPModel.ConnectorManager.Connectors.Cast<Connector>(), (weld, connector) => (weld, connector))
                .SelectMany(t => t.connector.AllRefs.Cast<Connector>(), (tuple, reference) => new { tuple, reference })
                .GroupBy(w => w.tuple.weld, arg => arg.reference.Owner)
                .ToDictionary(
                    e => e.Key,
                    elements => (Pipe: elements.Where(i => i.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves).Select(p => p).FirstOrDefault(),
                                    Fitings: elements.Where(i => i.Category.Id.IntegerValue != (int)BuiltInCategory.OST_PipeCurves).Select(p => p).FirstOrDefault()));

            // elements => elements.Select(
            // i => (PipeParameters: i.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves ? i.GetOrderedParameters() : null,
            // Fitings: i.Category.Id.IntegerValue != (int)BuiltInCategory.OST_PipeCurves ? i : null)));
        }
    }
}