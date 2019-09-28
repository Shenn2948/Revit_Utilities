namespace Gladkoe.ParameterDataManipulations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.UI;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.ParameterDataManipulations.Models;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FillParameters : IExternalCommand
    {
        private static IGetRevitDataStrategy revitData;

        public static Document RevitDocument { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            RevitDocument = uidoc.Document;

            revitData = new ParamManipulGetRvtData();

            try
            {
                FillParametersAction();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void FillParametersAction()
        {
            List<Element> elements = revitData.GetElements(RevitDocument).ToList();

            using (Transaction tran = new Transaction(RevitDocument))
            {
                tran.Start("Заполнить UID, Длину, Наружный диаметр, Условный диаметр");

                SetParameters(elements);

                tran.Commit();
            }
        }

        private static Parameter GetParameter(Element element, string parameterName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Проблема в нахождении параметра \"{parameterName}\", проверьте верность наименования и наличие параметров.");
            sb.AppendLine("Необходимо, чтобы категории:");
            sb.AppendLine("\"Арматура трубопроводов\",");
            sb.AppendLine("\"Оборудование\",");
            sb.AppendLine("\"Гибкие трубы\",");
            sb.AppendLine("\"Сантехнические приборы\",");
            sb.AppendLine("\"Соединительные детали трубопроводов\"");
            sb.AppendLine();
            sb.AppendLine("содержали параметр:");
            sb.AppendLine("\"UID\" (тип текст)");
            sb.AppendLine();
            sb.AppendLine("\"Трубы:\",");
            sb.AppendLine("содержали параметр:");
            sb.AppendLine("\"Наружный диаметр (тип - длина)\",");
            sb.AppendLine("\"Условный диаметр (тип - длина)\",");
            sb.AppendLine("\"Длина\"(тип - длина)");

            return element.GetOrderedParameters().FirstOrDefault(e => e.Definition.Name.Equals(parameterName))
                   ?? throw new ArgumentException(sb.ToString());
        }

        private static void SetParameters(List<Element> elements)
        {
            int i = 0;

            foreach (Element element in elements)
            {
                Parameter resultParameter = GetParameter(element, "UID");
                resultParameter.Set(element.Id.IntegerValue.ToString());

                if (element is Pipe pipe)
                {
                    Dictionary<string, Parameter> pipeResultParameters = pipe.ParametersMap.Cast<Parameter>()
                        .Where(p => p.IsShared && (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES))
                        .GroupBy(p => p.Definition.Name, p => p)
                        .ToDictionary(p => p.Key, p => p.FirstOrDefault());

                    Dictionary<string, Parameter> pipeParameters = pipe.GetOrderedParameters()
                        .Where(
                            p => !p.IsShared
                                 && ((p.Definition.ParameterGroup == BuiltInParameterGroup.PG_GEOMETRY) || (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_MECHANICAL)))
                        .GroupBy(p => p.Definition.Name, p => p)
                        .ToDictionary(p => p.Key, p => p.FirstOrDefault());

                    if (pipeParameters.ContainsKey("Длина"))
                    {
                        pipeResultParameters["Длина"]?.Set(pipeParameters["Длина"].AsDouble());
                    }

                    if (pipeParameters.ContainsKey("Внешний диаметр"))
                    {
                        pipeResultParameters["Наружный диаметр"]?.Set(pipeParameters["Внешний диаметр"].AsDouble());
                    }

                    if (pipeParameters.ContainsKey("Диаметр"))
                    {
                        pipeResultParameters["Условный диаметр"]?.Set(pipeParameters["Диаметр"].AsDouble());
                    }
                }

                i++;
            }

            TaskDialog.Show("Info", $"Элементов обработано {i}");
        }
    }
}