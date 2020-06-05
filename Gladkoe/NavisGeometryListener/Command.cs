using System;
using System.Collections.Generic;
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

                var mainWindow = new ServerView(uidoc, eventHandler, externalEvent);
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

        public void CreateSphereDirectShape(Document doc)
        {
            List<Curve> profile = new List<Curve>();
            XYZ center = XYZ.Zero;

            double radius = UnitUtils.Convert(500, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
            XYZ profile00 = center;
            XYZ profilePlus = center + new XYZ(0, radius, 0);
            XYZ profileMinus = center - new XYZ(0, radius, 0);
            profile.Add(Line.CreateBound(profilePlus, profileMinus));
            profile.Add(Arc.Create(profileMinus, profilePlus, center + new XYZ(radius, 0, 0)));
            CurveLoop curveLoop = CurveLoop.Create(profile);
            SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);
            Frame frame = new Frame(center, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
            if (Frame.CanDefineRevitGeometry(frame))
            {
                Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(frame, new[] { curveLoop }, 0, 2 * Math.PI, options);
                using (Transaction t = new Transaction(doc, "Create sphere direct shape"))
                {
                    t.Start();
                    DirectShapeLibrary directShapeLibrary = DirectShapeLibrary.GetDirectShapeLibrary(doc);
                    DirectShapeType directShapeType = DirectShapeType.Create(doc, "Tested", new ElementId(BuiltInCategory.OST_GenericModel));
                    directShapeType.SetShape(new List<GeometryObject>() { sphere });
                    directShapeLibrary.AddDefinitionType("Tested", directShapeType.Id);

                    DirectShape ds = DirectShape.CreateElementInstance(doc, directShapeType.Id, directShapeType.Category.Id, "Tested", Transform.Identity);
                    ds.SetTypeId(directShapeType.Id);
                    ds.ApplicationId = "Application id";
                    ds.ApplicationDataId = "Geometry object id";
                    ds.SetShape(new GeometryObject[] { sphere });
                    t.Commit();
                }
            }
        }
    }
}