namespace Revit_Utilities.Utilities
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    /// <summary>
    /// The result showing helper.
    /// </summary>
    public class ResultsHelper
    {
        /// <summary>
        /// The save file.
        /// </summary>
        public static string GetSaveExcelFilePath()
        {
            var saveFileDialog = new SaveFileDialog()
                                 {
                                     Filter = @"Excel Files|*.xlsx",
                                     FilterIndex = 1,
                                     RestoreDirectory = true,
                                     Title = @"Сохранить Excel файл"
                                 };

            return saveFileDialog.ShowDialog() == DialogResult.OK ? saveFileDialog.FileName : string.Empty;
        }

        /// <summary>
        /// The save file.
        /// </summary>
        public static string GetOpenJsonFilePath()
        {
            var openFileDialog = new OpenFileDialog()
                                 {
                                     Filter = @"JSON Files|*.json",
                                     FilterIndex = 1,
                                     RestoreDirectory = true,
                                     Title = @"Открыть файл JSON"
            };

            return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : string.Empty;
        }

        /// <summary>
        /// The save file.
        /// </summary>
        /// <param name="s">
        /// The string
        /// </param>
        public static bool WriteJsonFile(string s)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
                                            {
                                                Filter = @"JSON Files|*.json",
                                                FilterIndex = 1,
                                                RestoreDirectory = true,
                                                Title = @"Сохранить файл JSON"
                                            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;

                using (StreamWriter stream = new StreamWriter(fileName))
                {
                    stream.WriteLine(s);
                }

                return true;
            }

            return false;
        }
    }
}