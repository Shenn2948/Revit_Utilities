using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitUtils.Geometry.Entities.Selection;
using RevitUtils.Geometry.WallPenetration.Entities;

namespace RevitUtils.Geometry.WallPenetration
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private FamilySymbol _roundOpen;
        private FamilySymbol _rectOpen;
        private Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            _doc = uidoc.Document;

            _roundOpen = GetFamilySymbol(_doc, "Отверстие_Поворотное", "Отверстие");
            _rectOpen = GetFamilySymbol(_doc, "DVS_Opening_Rectangle_FaceBased", "DVS_Opening_Rectangle_FaceBased");

            Wall wall = GetWall(uidoc, _doc);
            IEnumerable<MEPCurve> intersectElement = GetIntersectElements(wall);

            using (TransactionGroup tranGr = new TransactionGroup(_doc))
            {
                tranGr.Start("wall penetration");

                foreach (MEPCurve intersector in intersectElement)
                {
                    Connector connector = intersector.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault();
                    WallIntersectionData data = new WallIntersectionData(wall, intersector);
                    AngleCalculator calculator = new AngleCalculator(data);

                    Cut(data, calculator, connector);
                }

                tranGr.Assimilate();
            }

            return Result.Succeeded;
        }

        private void Cut(WallIntersectionData data, AngleCalculator calculator, IConnector connector)
        {
            using (TransactionGroup tranGr = new TransactionGroup(_doc))
            {
                tranGr.Start("wall penetration one element");

                using (Transaction tran = new Transaction(_doc))
                {
                    switch (connector?.Shape)
                    {
                        case ConnectorProfileType.Rectangular:
                            tran.Start("Creating wall penetration");

                            FamilyInstance fi = _doc.Create.NewFamilyInstance(data.WallSideFaceRef, calculator.LocationPoint, new XYZ(1, 0, 0), _rectOpen);
                            fi.LookupParameter("Width").Set(connector.Width);
                            fi.LookupParameter("Height").Set(connector.Height);

                            tran.Commit();
                            break;
                        case ConnectorProfileType.Round:

                            tran.Start("Creating wall penetration");

                            fi = _doc.Create.NewFamilyInstance(data.WallSideFaceRef, calculator.LocationPoint, new XYZ(1, 0, 0), _roundOpen);

                            tran.Commit();

                            tran.Start("SetPar");

                            fi.LookupParameter("НаружныйДиаметр").Set(connector.Radius * 2 * 1.2);
                            fi.LookupParameter("УголВертикальногоПоворота").Set(calculator.VerticalAngle);
                            fi.LookupParameter("УголГоризонтальногоПоворота").Set(calculator.HorizontalAngle);

                            tran.Commit();

                            break;
                    }
                }

                tranGr.Assimilate();
            }
        }

        private static FamilySymbol GetFamilySymbol(Document doc, string familyName, string name)
        {
            FamilySymbol symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                                                                   .OfType<FamilySymbol>()
                                                                   .FirstOrDefault(x => x.FamilyName == familyName && x.Name == name);

            if (symbol != null && !symbol.IsActive)
            {
                symbol.Activate();
            }

            return symbol;
        }

        private static MEPCurve GetIntersectingElement(UIDocument uidoc, Document doc)
        {
            Reference interPickObject = uidoc.Selection.PickObject(ObjectType.Element, "pick a inter element");
            return doc.GetElement(interPickObject) as MEPCurve;
        }

        private static Wall GetWall(UIDocument uidoc, Document doc)
        {
            Reference wallPickObject = uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "pick a wall");
            Wall wall = (Wall)doc.GetElement(wallPickObject);
            return wall;
        }

        private IEnumerable<MEPCurve> GetIntersectElements(Wall wall)
        {
            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .WhereElementIsViewIndependent()
                                                     .WherePasses(new ElementIntersectsElementFilter(wall))
                                                     .OfType<MEPCurve>();
        }
    }
}