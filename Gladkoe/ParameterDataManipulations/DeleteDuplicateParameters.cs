namespace Gladkoe.ParameterDataManipulations
{
    using System;
    using System.Collections.Generic;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.ParameterDataManipulations.Models;

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DeleteDuplicateParameters : IExternalCommand
    {
        private static IGetDataSetStrategy<string, List<Element>> dataSet;

        private static IGetRevitDataStrategy revitData;

        public static Document RevitDocument { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            RevitDocument = uidoc.Document;
            dataSet = new ParamManipulGetDataSet();
            revitData = new ParamManipulGetRvtData();

            try
            {
                // SerializeData(RevitDocument);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Save parameters", e.Message);
            }

            return Result.Succeeded;
        }
    }
}