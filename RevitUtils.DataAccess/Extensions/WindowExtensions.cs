using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Windows;

using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using TaskDialogCommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons;
using TaskDialogIcon = Autodesk.Revit.UI.TaskDialogIcon;

namespace RevitUtils.DataAccess.Extensions
{
    public static class WindowExtensions
    {
        public static void ShowDialogResult(this Window mainWindow)
        {
            var helper = new WindowInteropHelper(mainWindow) { Owner = ComponentManager.ApplicationWindow };

            mainWindow.ShowDialog();
        }

        public static void ShowRevitDialog(this Exception exception)
        {
            var td = new TaskDialog("Ошибка")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconError,
                TitleAutoPrefix = false,
                MainInstruction = $"{exception.GetType().Name}",
                MainContent = exception.Message,
                CommonButtons = TaskDialogCommonButtons.Ok
            };
            td.Show();
        }

        public static void ShowRevitDialog(this Exception exception, string largeText)
        {
            var td = new TaskDialog($"{exception.GetType().Name}")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconError,
                TitleAutoPrefix = false,
                MainInstruction = largeText,
                MainContent = exception.Message,
                CommonButtons = TaskDialogCommonButtons.Ok
            };
            td.Show();
        }
    }
}