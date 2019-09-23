#region Namespaces

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Autodesk.Revit.DB;

using Gladkoe.ParameterDataManipulations.Views;

#endregion

namespace Gladkoe.ParameterDataManipulations
{
    public static class DuplicateValidator
    {
        public static bool ValidateDuplicates(List<Element> elements)
        {
            (StringBuilder stringBuilder, int count) = GetParametersDuplicatesInfo(elements);

            if (count > 0)
            {
                var duplicatesWindow = new DuplicatesWindow(stringBuilder);
                duplicatesWindow.ShowDialog();
                return true;
            }

            return false;
        }

        private static (StringBuilder sb, int Count) GetParametersDuplicatesInfo(IEnumerable<Element> elements)
        {
            Dictionary<string, Dictionary<string, List<(string DuplicateName, int Count)>>> query = elements.Where(
                    e =>
                    {
                        var hasDuplicate = e.GetOrderedParameters()
                            .Where(par => par.IsShared)
                            .Select(i => (Name: i.Definition.Name, GUID: i.GUID))
                            .GroupBy(i => i.Name, i => i.GUID)
                            .Any(i => i.Select(guid => guid).Count() > 1);
                        return hasDuplicate;
                    })
                .Select(
                    e => new
                    {
                        Element = e,
                        Duplicates = e.GetOrderedParameters()
                                 .Where(par => par.IsShared)
                                 .Select(i => (Name: i.Definition.Name, GUID: i.GUID))
                                 .GroupBy(i => i.Name, i => i.GUID)
                                 .Where(i => i.Select(guid => guid).Count() > 1)
                                 .Select(p => new { Name = p.Key, Count = p.Select(i => i).Count() }),
                        Category = e.Category.Name
                    })
                .GroupBy(c => c.Category)
                .ToDictionary(
                    e => e.Key,
                    e => e.Select(x => (FamilyName: x.Element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString(), Duplicates: x.Duplicates))
                        .GroupBy(f => f.FamilyName, f => f.Duplicates)
                        .ToDictionary(
                            m => m.Key,
                            m => m.SelectMany(i => i).GroupBy(p => p.Name, p => p.Count).Select(p => (DuplicateName: p.Key, Count: p.FirstOrDefault())).ToList()));

            var sb = GetResults(query);

            return (sb, query.Count);
        }

        private static StringBuilder GetResults(Dictionary<string, Dictionary<string, List<(string DuplicateName, int Count)>>> query)
        {
            var sb = new StringBuilder();

            foreach (var category in query)
            {
                sb.AppendLine($"Категория: {category.Key} ");
                sb.AppendLine("-------------------------------------------------------------------");
                sb.AppendLine();
                foreach (var family in category.Value)
                {
                    sb.AppendLine($"Семейство: {family.Key} ");
                    sb.AppendLine();

                    foreach (var duplicate in family.Value)
                    {
                        sb.Append($"Parameter: {duplicate.DuplicateName}, count: {duplicate.Count}");
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb;
        }
    }
}