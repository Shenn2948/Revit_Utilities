using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using RevitUtils.DataAccess.Entities.Handlers;
using RevitUtils.DataAccess.Extensions;
using RevitUtils.Geometry.NavisGeometryListener.Views;

namespace RevitUtils.Geometry.NavisGeometryListener
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            try
            {
                ExternalEventHandler eventHandler = new ExternalEventHandler();
                ExternalEvent externalEvent = ExternalEvent.Create(eventHandler);

                var mainWindow = new ServerView();
                var helper = new WindowInteropHelper(mainWindow) { Owner = ComponentManager.ApplicationWindow };

                mainWindow.Show();
            }
            catch (Exception e)
            {
                e.ShowRevitDialog();
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}