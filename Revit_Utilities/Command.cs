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

            // Iterate over all elements, both symbols and 
            // model elements, and them in the dictionary.
            ElementFilter f = new LogicalOrFilter(new ElementIsElementTypeFilter(false), new ElementIsElementTypeFilter(true));
            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(f);

            foreach (Element e in collector)
            {
                Category category = e.Category;

                if (category != null)
                {
                    // If this category was not yet encountered,
                    // add it and create a new container for its
                    // elements.
                    if (!sortedElements.ContainsKey(category.Name))
                    {
                        sortedElements.Add(category.Name, new List<Element>());
                    }

                    sortedElements[category.Name].Add(e);
                }
            }

            // Launch or access Excel via COM Interop:
            X.Application excel = new X.Application { Visible = true };
            X.Workbook workbook = excel.Workbooks.Add(Missing.Value);

            // We cannot delete all work sheets, 
            // Excel requires at least one.
            // while( 1 < workbook.Sheets.Count ) 
            // {
            // worksheet = workbook.Sheets.get_Item(1) as X.Worksheet;
            // worksheet.Delete();
            // }

            // Loop through all collected categories and 
            // create a worksheet for each except the first.
            // We sort the categories and work trough them 
            // from the end, since the worksheet added last 
            // shows up first in the Excel tab.
            List<string> keys = new List<string>(sortedElements.Keys);

            keys.Sort();
            keys.Reverse();

            bool first = true;
            int nElements = 0;
            int nCategories = sortedElements.Count;

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
                    // First column is the element id,
                    // second a flag indicating type (symbol)
                    // or not, both displayed as an integer.
                    worksheet.Cells[row, 1] = e.Id.IntegerValue;
                    worksheet.Cells[row, 2] = e is ElementType ? 1 : 0;
                    column = 3;

                    foreach (string paramName in paramNames)
                    {
                        var paramValue = "*NA*";

                        // Parameter p = e.get_Parameter( paramName ); // 2014

                        // Careful! This returns the first best param found.
                        Parameter p = e.LookupParameter(paramName); // 2015

                        if (p != null)
                        {
                            // try
                            // {
                            paramValue = LabUtils.GetParameterValue(p);

                            // }
                            // catch( Exception ex )
                            // {
                            // Debug.Print( ex.Message );
                            // }
                        }

                        worksheet.Cells[row, column++] = paramValue;
                    }

                    // column
                    ++nElements;
                    ++row;
                }

                // row
            }

            // category == worksheet
            sw.Stop();

            TaskDialog.Show(
                "Parameter Export",
                $"{nCategories} categories and a total " + $"of {nElements} elements exported " + $"in {sw.Elapsed.TotalSeconds:F2} seconds.");
        }
    }
}