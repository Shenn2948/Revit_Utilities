using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitUtils.Geometry.WallPenetration
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private FamilySymbol _roundOpen;
        private FamilySymbol _rectOpen;
        private Wall _wall;
        private MEPCurve _intersectingElement;
        private Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            _doc = uidoc.Document;

            _roundOpen = GetFamilySymbol(_doc, "Отверстие_Поворотное", "Отверстие");
            _rectOpen = GetFamilySymbol(_doc, "DVS_Opening_Rectangle_FaceBased", "DVS_Opening_Rectangle_FaceBased");

            _wall = GetWall(uidoc, _doc);
            _intersectingElement = GetIntersectingElement(uidoc, _doc);

            Run();

            return Result.Succeeded;
        }

        private void Run()
        {
            Connector connector = _intersectingElement.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault();
            Solid wallSolid = _wall.GetSolid();
            Line intersectingCurve = GetIntersectingCurve(_intersectingElement, wallSolid);

            IList<Reference> wallSideFaceRefs = HostObjectUtils.GetSideFaces(_wall, ShellLayerType.Interior);
            Reference wallSideFaceRef = wallSideFaceRefs[0];

            using (TransactionGroup tranGr = new TransactionGroup(_doc))
            {
                tranGr.Start("wall penetration");

                using (Transaction tran = new Transaction(_doc))
                {
                    switch (connector?.Shape)
                    {
                        case ConnectorProfileType.Rectangular:
                            tran.Start("Creating wall penetration");

                            FamilyInstance fi = _doc.Create.NewFamilyInstance(wallSideFaceRef, intersectingCurve.Origin, new XYZ(1, 0, 0), _rectOpen);
                            fi.LookupParameter("Width").Set(connector.Width);
                            fi.LookupParameter("Height").Set(connector.Height);

                            tran.Commit();
                            break;
                        case ConnectorProfileType.Round:

                            tran.Start("Creating wall penetration");

                            XYZ mid = Util.MidPoint(intersectingCurve);

                            fi = _doc.Create.NewFamilyInstance(wallSideFaceRef, mid, new XYZ(1, 0, 0), _roundOpen);

                            tran.Commit();

                            tran.Start("Create lines");

                            PlanarFace wallSideFace = _doc.GetElement(wallSideFaceRef).GetGeometryObjectFromReference(wallSideFaceRef) as PlanarFace;

                            Transform t = Transform.CreateTranslation(mid);

                            LocationCurve locationCurve = _wall.Location as LocationCurve;
                            Line locLine = locationCurve.Curve as Line;

                            XYZ wallDir = t.OfPoint(locLine.Direction);
                            XYZ interDir = t.OfPoint(intersectingCurve.Direction);
                            XYZ wallNormalDir = t.OfPoint(wallSideFace.FaceNormal);

                            Line interDirLine = Line.CreateBound(mid, interDir);
                            Line wallNormalLine = Line.CreateBound(mid, wallNormalDir);
                            Line wallDirLine = Line.CreateBound(mid, wallDir);

                            tran.Commit();

                            tran.Start("SetPar");

                            XYZ vectorInterDir = Util.GetVector(interDirLine);

                            XYZ vectorWallNormal = Util.GetVector(wallNormalLine);
                            XYZ vectorWallDir = Util.GetVector(wallDirLine);

                            double vertical = vectorWallNormal.AngleOnPlaneTo(vectorInterDir, vectorWallDir.Normalize());
                            double horizontal = vectorWallDir.AngleOnPlaneTo(vectorInterDir, XYZ.BasisZ);

                            double angle90 = UnitUtils.ConvertToInternalUnits(90, DisplayUnitType.DUT_DECIMAL_DEGREES);

                            fi.LookupParameter("НаружныйДиаметр").Set(connector.Radius * 2 * 1.2);
                            fi.LookupParameter("УголВертикальногоПоворота").Set(-vertical);
                            fi.LookupParameter("УголГоризонтальногоПоворота").Set(horizontal - angle90);

                            double x = Math.Tan(-vertical) * _wall.Width / 2;
                            double z = Math.Tan(horizontal - angle90) * _wall.Width / 2;

                            XYZ translation = new XYZ(mid.X + z, mid.Y, mid.Z + x) - mid;

                            fi.Location.Move(translation);

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

        private static Line GetIntersectingCurve(Element inter, Solid geomSolid)
        {
            if (inter.Location is LocationCurve locationCurve)
            {
                SolidCurveIntersection line = geomSolid.IntersectWithCurve(locationCurve.Curve, new SolidCurveIntersectionOptions());
                Line curveSegment = line.GetCurveSegment(0) as Line;
                return curveSegment;
            }

            return null;
        }
    }

    public static class Util
    {
        public static XYZ GetVector(Curve curve)
        {
            XYZ vectorAb = curve.GetEndPoint(0);
            XYZ vectorAc = curve.GetEndPoint(1);
            XYZ vectorBc = vectorAc - vectorAb;
            return vectorBc;
        }

        public static Solid GetSolid(this Element e, bool notVoid = false)
        {
            if (notVoid)
            {
                return e?.get_Geometry(new Options { ComputeReferences = true }).OfType<Solid>().FirstOrDefault(s => !s.Edges.IsEmpty);
            }

            return e?.get_Geometry(new Options { ComputeReferences = true }).OfType<Solid>().FirstOrDefault();
        }

        public static XYZ MidPoint(XYZ p, XYZ q)
        {
            return 0.5 * (p + q);
        }

        public static XYZ MidPoint(Line line)
        {
            return MidPoint(line.GetEndPoint(0), line.GetEndPoint(1));
        }
    }

    public class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}