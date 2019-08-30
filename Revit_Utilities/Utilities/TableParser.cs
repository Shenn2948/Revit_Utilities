namespace Revit_Utilities.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    public static class TableParser
    {
        /// <summary>
        /// The to string table.
        /// </summary>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <param name="columnHeaders">
        /// The column headers.
        /// </param>
        /// <param name="valueSelectors">
        /// The value selectors.
        /// </param>
        /// <typeparam name="T">the T type
        /// </typeparam>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ToStringTable<T>(this IEnumerable<T> values, string[] columnHeaders, params Func<T, object>[] valueSelectors)
        {
            return ToStringTable(values.ToArray(), columnHeaders, valueSelectors);
        }

        /// <summary>
        /// The to string table.
        /// </summary>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <param name="columnHeaders">
        /// The column headers.
        /// </param>
        /// <param name="valueSelectors">
        /// The value selectors.
        /// </param>
        /// <typeparam name="T">the T type
        /// </typeparam>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ToStringTable<T>(this T[] values, string[] columnHeaders, params Func<T, object>[] valueSelectors)
        {
            var arrValues = new string[values.Length + 1, valueSelectors.Length];

            // Fill headers
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                arrValues[0, colIndex] = columnHeaders[colIndex];
            }

            // Fill table rows
            for (int rowIndex = 1; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
                {
                    arrValues[rowIndex, colIndex] = valueSelectors[colIndex].Invoke(values[rowIndex - 1]).ToString();
                }
            }

            return ToStringTable(arrValues);
        }

        /// <summary>
        /// The to string table.
        /// </summary>
        /// <param name="arrValues">
        /// The array values.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ToStringTable(this string[,] arrValues)
        {
            int[] maxColumnsWidth = GetMaxColumnsWidth(arrValues);
            var headerSpliter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

            var sb = new StringBuilder();
            for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
                {
                    // Print cell
                    string cell = arrValues[rowIndex, colIndex];
                    cell = cell.PadRight(maxColumnsWidth[colIndex]);
                    sb.Append(" | ");
                    sb.Append(cell);
                }

                // Print end of line
                sb.Append(" | ");
                sb.AppendLine();

                // Print splitter
                if (rowIndex == 0)
                {
                    sb.AppendFormat(" |{0}| ", headerSpliter);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// The to string table.
        /// </summary>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <param name="valueSelectors">
        /// The value selectors.
        /// </param>
        /// <typeparam name="T">the T type
        /// </typeparam>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ToStringTable<T>(this IEnumerable<T> values, params Expression<Func<T, object>>[] valueSelectors)
        {
            var headers = valueSelectors.Select(func => GetProperty(func).Name).ToArray();
            var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
            return ToStringTable(values, headers, selectors);
        }

        /// <summary>
        /// The to print console.
        /// </summary>
        /// <param name="dataTable">
        /// The data table.
        /// </param>
        public static void ToPrintFile(this DataTable dataTable)
        {
            // Print top line
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(new string('-', 75));
            
            // Print col headers
            var colHeaders = dataTable.Columns.Cast<DataColumn>().Select(arg => arg.ColumnName);
            foreach (string s in colHeaders)
            {
                sb.AppendFormat("| {0,-20}", s);
            }

            sb.AppendLine();

            // Print line below col headers
            sb.AppendLine(new string('-', 75));

            // Print rows
            foreach (DataRow row in dataTable.Rows)
            {
                foreach (object o in row.ItemArray)
                {
                    sb.AppendFormat("| {0,-20}", o);
                }

                sb.AppendLine();
            }

            // Print bottom line
            sb.AppendLine(new string('-', 75));

            ResultsHelper.SaveFile(sb);
        }

        private static PropertyInfo GetProperty<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    return memberExpression.Member as PropertyInfo;
                }
            }

            if (expression.Body is MemberExpression body)
            {
                return body.Member as PropertyInfo;
            }

            return null;
        }

        private static int[] GetMaxColumnsWidth(string[,] arrValues)
        {
            var maxColumnsWidth = new int[arrValues.GetLength(1)];
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
                {
                    int newLength = arrValues[rowIndex, colIndex].Length;
                    int oldLength = maxColumnsWidth[colIndex];

                    if (newLength > oldLength)
                    {
                        maxColumnsWidth[colIndex] = newLength;
                    }
                }
            }

            return maxColumnsWidth;
        }
    }
}