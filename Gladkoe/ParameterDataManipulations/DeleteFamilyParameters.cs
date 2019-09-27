namespace Gladkoe.ParameterDataManipulations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Forms;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.ParameterDataManipulations.Models;

    using MessageBox = System.Windows.Forms.MessageBox;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DeleteFamilyParameters : IExternalCommand
    {
        public static Document RevitDocument { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            RevitDocument = uidoc.Document;

            try
            {
                DeleteFamilyParametersOfGroup(RevitDocument);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Delete parameters", e.Message);
            }

            return Result.Succeeded;
        }

        private static void DeleteFamilyParametersOfGroup(Document doc)
        {
            if (!doc.IsFamilyDocument)
            {
                throw new ArgumentException("Документ не является документом-семейством");
            }

            var familyParameters = doc.FamilyManager.GetParameters()
                .Where(p => p.IsShared && (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES))
                .ToList();

            if (MessageBox.Show(@"Вы действительно хотите удалить все параметры группы ""Свойства модели""?", @"Delete parameters", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                using (var tran = new Transaction(doc))
                {
                    tran.Start("Deleting parameters");

                    foreach (FamilyParameter parameter in familyParameters)
                    {
                        doc.FamilyManager.RemoveParameter(parameter);
                    }

                    tran.Commit();
                }
            }
        }
    }
}