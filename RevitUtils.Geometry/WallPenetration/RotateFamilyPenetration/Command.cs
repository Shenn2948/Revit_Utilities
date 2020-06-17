using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitUtils.Geometry.Entities.Selection;
using RevitUtils.Geometry.RotateCamera;
using RevitUtils.Geometry.Utils;
using RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration.Entities;

namespace RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private FamilySymbol _roundOpen;
        private FamilySymbol _rectOpen;
        private Document _doc;
        private UIDocument _uidoc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            _uidoc = uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;

            _roundOpen = GetFamilySymbol(_doc, "Отверстие_Поворотное", "Отверстие");
            _rectOpen = GetFamilySymbol(_doc, "ОтверстиеПрямоугольное_Поворотное", "Отверстие");

            Wall wall = GetWall();

            //MEPIntersection(wall);
            FamilyInstanceIntersection(wall);

            return Result.Succeeded;
        }

        private void FamilyInstanceIntersection(Wall wall)
        {
            FamilyInstance intersector = (FamilyInstance)GetIntersectingElement();
            WallExtrusion extrusion = new WallExtrusion(intersector);

            WallIntersectionData data = new WallIntersectionData(wall, intersector);
            AngleCalculator calculator = new AngleCalculator(data);

            Cut(data, calculator, extrusion);

            //Solid solid = intersector.GetSolid(true);
            //PlanarFace topFace = solid.Faces.OfType<PlanarFace>().FirstOrDefault(f => Util.PointsUpwards(f.FaceNormal));
            //XYZ centroid = solid.ComputeCentroid();

            //var transform = Transform.CreateTranslation(centroid);

            //var xx = transform.OfPoint(topFace.FaceNormal);
            //var perpend = Line.CreateBound(transform.Origin, xx);

            //using (var tran = new Transaction(_doc))
            //{
            //    tran.Start("Add line");

            //    Creator.CreateModelCurve(_uidoc.Application, perpend);

            //    tran.Commit();
            //}
        }

        private void MEPIntersection(Wall wall)
        {
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

                            tran.Commit();

                            tran.Start("SetPar");

                            double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);

                            fi.LookupParameter("ШиринаОтверстия").Set(connector.Width + offset);
                            fi.LookupParameter("ВысотаОтверстия").Set(connector.Height + offset);
                            fi.LookupParameter("УголВертикальногоПоворота").Set(calculator.VerticalAngle);
                            fi.LookupParameter("УголГоризонтальногоПоворота").Set(calculator.HorizontalAngle);

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

        private void Cut(WallIntersectionData data, AngleCalculator calculator, WallExtrusion extrusion)
        {
            using (TransactionGroup tranGr = new TransactionGroup(_doc))
            {
                tranGr.Start("wall penetration one element");

                using (Transaction tran = new Transaction(_doc))
                {
                    tran.Start("Creating wall penetration");

                    FamilyInstance fi = _doc.Create.NewFamilyInstance(data.WallSideFaceRef, calculator.LocationPoint, new XYZ(1, 0, 0), _rectOpen);

                    tran.Commit();

                    tran.Start("SetPar");

                    double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);

                    fi.LookupParameter("ШиринаОтверстия").Set(extrusion.Width + offset);
                    fi.LookupParameter("ВысотаОтверстия").Set(extrusion.Height + offset);
                    fi.LookupParameter("УголВертикальногоПоворота").Set(calculator.VerticalAngle);
                    fi.LookupParameter("УголГоризонтальногоПоворота").Set(calculator.HorizontalAngle);

                    tran.Commit();
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

        private Element GetIntersectingElement()
        {
            Reference interPickObject = _uidoc.Selection.PickObject(ObjectType.Element, "pick a inter element");
            return _doc.GetElement(interPickObject);
        }

        private Wall GetWall()
        {
            Reference wallPickObject = _uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "pick a wall");
            Wall wall = (Wall)_doc.GetElement(wallPickObject);
            return wall;
        }

        private IEnumerable<MEPCurve> GetIntersectElements(Element element)
        {
            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .WhereElementIsViewIndependent()
                                                     .WherePasses(new ElementIntersectsElementFilter(element))
                                                     .OfType<MEPCurve>();
        }
    }
}