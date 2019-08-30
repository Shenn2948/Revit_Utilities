#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace Revit_Utilities
{
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Windows.Forms;

    using Application = Autodesk.Revit.ApplicationServices.Application;

    /// <summary>
    /// The command.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="commandData">
        /// The command data.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="elements">
        /// The elements.
        /// </param>
        /// <returns>
        /// The <see cref="Result"/>.
        /// </returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            var results = this.FilterStringEquals("Комментарии", string.Empty, doc);
            StringBuilder sb = new StringBuilder();

            foreach (Element e in results)
            {
                sb.AppendLine(e.Name);
            }

            TaskDialog.Show("Revit", sb.ToString());

            return Result.Succeeded;
        }

        private static void WriteFile(IList<Element> col)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Element e in col)
            {
                foreach (Parameter p in e.Parameters)
                {
                    if (p.Definition.Name.Equals("Имя семейства"))
                    {
                        sb.AppendLine(p.AsString());
                    }
                }
            }

            SaveFile(sb);
        }

        private static void SaveFile(StringBuilder sb)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
                                                {
                                                    Filter = @"Text Files|*.txt",
                                                    FilterIndex = 1,
                                                    RestoreDirectory = true,
                                                    Title = @"Создать файл общих параметров"
                                                };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;

                using (StreamWriter stream = new StreamWriter(fileName))
                {
                    stream.WriteLine(sb.ToString());
                }
            }
        }

        private IEnumerable<Parameter> GetParametersFromSchedule(ViewSchedule viewSchedule)
        {
            var instCollector = new FilteredElementCollector(viewSchedule.Document, viewSchedule.Id)
                .WhereElementIsNotElementType();

            List<Parameter> allParameters =
                instCollector.SelectMany(x => x.Parameters.Cast<Parameter>().Select(p => p)).ToList();

            var definition = viewSchedule.Definition;
            ScheduleField foundField = null;

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                foundField = definition.GetField(fieldId);

                if (foundField.IsCombinedParameterField)
                {
                    IList<TableCellCombinedParameterData> combinedParams = foundField.GetCombinedParameters();
                    foreach (TableCellCombinedParameterData param in combinedParams)
                    {
                        Parameter p = allParameters.FirstOrDefault(x => x.Id.Compare(param.ParamId) == 0);
                        if (p != null)
                        {
                            yield return p;
                        }
                    }
                }
                else
                {
                    Parameter p = allParameters.FirstOrDefault(x => x.Id == foundField.ParameterId);
                    if (p != null)
                    {
                        yield return p;
                    }
                }
            }
        }

        private IList<Element> FilterStringEquals(string caseParameter, string searchValue, Document doc)
        {
            Array bips = Enum.GetValues(typeof(BuiltInParameter));
            BindingMap bm = doc.ParameterBindings;

            DefinitionBindingMapIterator bmlist = bm.ForwardIterator();
            ElementId paramId = null;

            while (bmlist.MoveNext())
            {
                InternalDefinition bindDef = (InternalDefinition)bmlist.Key;

                if (bindDef.Name == caseParameter)
                {
                    paramId = bindDef.Id;
                    break;
                }
            }

            List<BuiltInParameter> bipList = new List<BuiltInParameter>();

            if (paramId == null)
            {
                foreach (BuiltInParameter bip in bips)
                {
                    string name;
                    try
                    {
                        name = LabelUtils.GetLabelFor(bip);
                    }
                    catch
                    {
                        continue;
                    }

                    if (name == caseParameter)
                    {
                        bipList.Add(bip);
                    }
                }
            }

            var sharedParamRule = new SharedParameterApplicableRule(caseParameter);
            var evaluator = new FilterStringEquals();

            Element parameter = null;
            FilteredElementCollector collector = null;
            FilteredElementCollector collector2 = null;

            if ((paramId == null) && (bipList.Count == 0))
            {
                var coll = new FilteredElementCollector(doc).OfClass(typeof(ParameterElement)).Cast<ParameterElement>();

                foreach (ParameterElement param in coll)
                {
                    if (param.GetDefinition().Name == caseParameter)
                    {
                        parameter = param;
                    }
                }

                if (parameter != null)
                {
                    var rulesList = new List<FilterRule>();
                    var provider = new ParameterValueProvider(parameter.Id);
                    var filterRule = new FilterStringRule(provider, evaluator, searchValue, false);
                    rulesList.Add(filterRule);
                    rulesList.Add(sharedParamRule);
                    var paramFilter = new ElementParameterFilter(rulesList);
                    collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                        .WherePasses(paramFilter);

                    collector2 = new FilteredElementCollector(doc).OfClass(typeof(HostObject)).WherePasses(paramFilter)
                        .UnionWith(collector);
                }
            }
            else if (paramId == null)
            {
                var filterList = new List<ElementFilter>();
                foreach (BuiltInParameter parameterGroup in bipList)
                {
                    var provider = new ParameterValueProvider(new ElementId(parameterGroup));
                    var filterRule = new FilterStringRule(provider, evaluator, searchValue, false);
                    var paramFilter = new ElementParameterFilter(filterRule);
                    filterList.Add(paramFilter);
                }

                var logicalOrFilter = new LogicalOrFilter(filterList);
                collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                    .WherePasses(logicalOrFilter);
                collector2 = new FilteredElementCollector(doc).OfClass(typeof(HostObject)).WherePasses(logicalOrFilter)
                    .UnionWith(collector);
            }
            else
            {
                var rulesList = new List<FilterRule>();
                var provider = new ParameterValueProvider(paramId);
                var filterRule = new FilterStringRule(provider, evaluator, searchValue, false);
                rulesList.Add(filterRule);
                rulesList.Add(sharedParamRule);
                var paramFilter = new ElementParameterFilter(rulesList);
                collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WherePasses(paramFilter);
                collector2 = new FilteredElementCollector(doc).OfClass(typeof(HostObject)).WherePasses(paramFilter)
                    .UnionWith(collector);
            }

            if ((parameter != null) && (paramId == null) && (bipList.Count == 0))
            {
                TaskDialog.Show("Revit", "Does not work with non-shared family parameters");
            }
            else
            {
                if (collector2 != null)
                {
                    return collector2.ToElements();
                }
            }

            return null;
        }
    }
}