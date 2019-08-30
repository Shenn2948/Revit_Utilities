// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Command.cs" company="PMTech">
//   PMTech
// </copyright>
// <summary>
//   The command.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Revit_Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows.Forms;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Revit_Utilities.Utilities;

    using Application = Autodesk.Revit.ApplicationServices.Application;
    using X = Microsoft.Office.Interop.Excel;

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

            GetElementsParameters(doc);

            return Result.Succeeded;
        }

        private static void GetElementsParameters(Document doc)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Dictionary<string, List<Element>> sortedElements = new Dictionary<string, List<Element>>();

            var cats = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => (e.Category != null) && e.Category.HasMaterialQuantities)
                .Select(e => (BuiltInCategory)e.Category.Id.IntegerValue)
                .ToList();

            IList<ElementFilter> a = cats
                .Select(bic => new ElementCategoryFilter(bic))
                .Cast<ElementFilter>()
                .ToList();

            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

            FilteredElementCollector els = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(categoryFilter);

            foreach (Element e in els)
            {
                Category category = e.Category;

                if (category != null)
                {
                    // If this category was not yet encountered, add it and create a new container for its elements.
                    if (!sortedElements.ContainsKey(category.Name))
                    {
                        sortedElements.Add(category.Name, new List<Element>());
                    }

                    sortedElements[category.Name].Add(e);
                }
            }

            var excel = new X.Application { Visible = true };
            var workbook = excel.Workbooks.Add(Missing.Value);

            var keys = new List<string>(sortedElements.Keys);

            keys.Sort();
            keys.Reverse();

            bool first = true;
            int numElements = 0;
            int numCategories = sortedElements.Count;

            foreach (var categoryName in keys)
            {
                List<Element> elementSet = sortedElements[categoryName];

                // Create and name the worksheet
                X.Worksheet worksheet;
                if (first)
                {
                    worksheet = workbook.Sheets.Item[1] as X.Worksheet;

                    first = false;
                }
                else
                {
                    worksheet = excel.Worksheets.Add(Missing.Value, Missing.Value, Missing.Value, Missing.Value) as X.Worksheet;
                }

                sortedElements.TryGetValue(categoryName, out List<Element> el);
                string newName = $"{el.Find(x => x.Category.Name == categoryName).Id.IntegerValue}";
                var name = categoryName.Length > 31 ? newName : categoryName;

                name = name.Replace(':', '_').Replace('/', '_');

                worksheet.Name = name;

                // Determine the names of all parameters 
                // defined for the elements in this set.
                List<string> paramNames = new List<string>();

                foreach (Element e in elementSet)
                {
                    ParameterSet parameters = e.Parameters;

                    foreach (Parameter parameter in parameters)
                    {
                        name = parameter.Definition.Name;

                        if (!paramNames.Contains(name))
                        {
                            paramNames.Add(name);
                        }
                    }
                }

                paramNames.Sort();

                // Add the header row in bold.
                worksheet.Cells[1, 1] = "ID";
                worksheet.Cells[1, 2] = "IsType";

                int column = 3;

                foreach (string paramName in paramNames)
                {
                    worksheet.Cells[1, column] = paramName;
                    ++column;
                }

                var range = worksheet.Range["A1", "Z1"];

                range.Font.Bold = true;
                range.EntireColumn.AutoFit();

                int row = 2;

                foreach (Element e in elementSet)
                {
                    worksheet.Cells[row, 1] = e.Id.IntegerValue;
                    worksheet.Cells[row, 2] = e is ElementType ? 1 : 0;
                    column = 3;

                    foreach (string paramName in paramNames)
                    {
                        var paramValue = "*NA*";

                        Parameter p = e.LookupParameter(paramName);

                        if (p != null)
                        {
                            paramValue = LabUtils.GetParameterValue(p);
                        }

                        worksheet.Cells[row, column++] = paramValue;
                    }

                    ++numElements;
                    ++row;
                }
            }

            sw.Stop();

            TaskDialog.Show("Parameter Export", $"{numCategories} categories and a total " + $"of {numElements} elements exported " + $"in {sw.Elapsed.TotalSeconds:F2} seconds.");
        }
    }
}