namespace Revit_Utilities.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    /// <summary>
    /// The parameters helper.
    /// </summary>
    public class ParametersHelper
    {
        /// <summary>
        /// The get elements by parameter name and parameter value filter.
        /// </summary>
        /// <param name="searchParameterName">
        /// The search parameter name.
        /// </param>
        /// <param name="searchValue">
        /// The search value.
        /// </param>
        /// <param name="doc">
        /// The doc.
        /// </param>
        /// <returns>
        /// The <see cref="IList{T}"/>.
        /// </returns>
        public static IList<Element> GetElementsByParameterNameAndParameterValueFilter(string searchParameterName, string searchValue, Document doc)
        {
            Array bips = Enum.GetValues(typeof(BuiltInParameter));
            DefinitionBindingMapIterator bmlist = doc.ParameterBindings.ForwardIterator();
            List<BuiltInParameter> bipList = new List<BuiltInParameter>();

            ElementId paramId = null;
            SharedParameterApplicableRule sharedParamRule = new SharedParameterApplicableRule(searchParameterName);
            FilterStringEquals evaluator = new FilterStringEquals();

            Element parameter = null;
            FilteredElementCollector collector = null;
            FilteredElementCollector collector2 = null;

            while (bmlist.MoveNext())
            {
                InternalDefinition bindDef = (InternalDefinition)bmlist.Key;

                if (bindDef.Name == searchParameterName)
                {
                    paramId = bindDef.Id;
                    break;
                }
            }

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

                    if (name == searchParameterName)
                    {
                        bipList.Add(bip);
                    }
                }
            }

            if ((paramId == null) && (bipList.Count == 0))
            {
                var coll = new FilteredElementCollector(doc).OfClass(typeof(ParameterElement)).Cast<ParameterElement>();

                foreach (ParameterElement param in coll)
                {
                    if (param.GetDefinition().Name == searchParameterName)
                    {
                        parameter = param;
                    }
                }

                if (parameter != null)
                {
                    List<FilterRule> rulesList = new List<FilterRule>();
                    ParameterValueProvider provider = new ParameterValueProvider(parameter.Id);
                    FilterStringRule filterRule = new FilterStringRule(provider, evaluator, searchValue, false);
                    rulesList.Add(filterRule);
                    rulesList.Add(sharedParamRule);
                    ElementParameterFilter paramFilter = new ElementParameterFilter(rulesList);
                    collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WherePasses(paramFilter);
                    collector2 = new FilteredElementCollector(doc).OfClass(typeof(HostObject)).WherePasses(paramFilter).UnionWith(collector);
                }
            }
            else if (paramId == null)
            {
                List<ElementFilter> filterList = new List<ElementFilter>();

                foreach (BuiltInParameter parameterGroup in bipList)
                {
                    ParameterValueProvider provider = new ParameterValueProvider(new ElementId(parameterGroup));
                    FilterStringRule filterRule = new FilterStringRule(provider, evaluator, searchValue, false);
                    ElementParameterFilter paramFilter = new ElementParameterFilter(filterRule);
                    filterList.Add(paramFilter);
                }

                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(filterList);
                collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WherePasses(logicalOrFilter);
                collector2 = new FilteredElementCollector(doc).OfClass(typeof(HostObject)).WherePasses(logicalOrFilter).UnionWith(collector);
            }
            else
            {
                List<FilterRule> rulesList = new List<FilterRule>();
                ParameterValueProvider provider = new ParameterValueProvider(paramId);
                FilterStringRule filterRule = new FilterStringRule(provider, evaluator, searchValue, false);
                rulesList.Add(filterRule);
                rulesList.Add(sharedParamRule);
                ElementParameterFilter paramFilter = new ElementParameterFilter(rulesList);
                collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WherePasses(paramFilter);
                collector2 = new FilteredElementCollector(doc).OfClass(typeof(HostObject)).WherePasses(paramFilter).UnionWith(collector);
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

        private static IEnumerable<Parameter> GetParametersFromSchedule(ViewSchedule viewSchedule)
        {
            FilteredElementCollector instCollector = new FilteredElementCollector(viewSchedule.Document, viewSchedule.Id).WhereElementIsNotElementType();

            List<Parameter> allParameters = instCollector.SelectMany(x => x.Parameters.Cast<Parameter>().Select(p => p)).ToList();

            ScheduleDefinition definition = viewSchedule.Definition;
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
    }
}