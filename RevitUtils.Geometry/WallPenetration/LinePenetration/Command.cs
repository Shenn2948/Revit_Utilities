using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitUtils.DataAccess.Extensions;
using RevitUtils.Geometry.Entities.Extensions;
using RevitUtils.Geometry.Entities.Selection;
using RevitUtils.Geometry.RotateCamera;
using RevitUtils.Geometry.Utils;
using RevitUtils.Geometry.WallPenetration.Entities;
using RevitUtils.Geometry.WallPenetration.RotateFamilyPenetration.Entities;

namespace RevitUtils.Geometry.WallPenetration.LinePenetration
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private Document _doc;
        private UIDocument _uidoc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            _uidoc = uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;

            Wall wall = GetWall();

            Intersection(wall);

            return Result.Succeeded;
        }

        private IEnumerable<Element> GetIntersectElements(Element element)
        {
            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .WhereElementIsViewIndependent()
                                                     .WherePasses(new ElementIntersectsElementFilter(element))
                                                     .ToElements();
        }

        private Wall GetWall()
        {
            Reference wallPickObject = _uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "pick a wall");
            Wall wall = (Wall)_doc.GetElement(wallPickObject);
            return wall;
        }

        private void Intersection(Wall wall)
        {
            var intersectElement = GetIntersectElements(wall);

            using (TransactionGroup tranGr = new TransactionGroup(_doc))
            {
                tranGr.Start("wall penetration");

                using (Transaction tran = new Transaction(_doc))
                {
                    tran.Start("Load family symbols");

                    FamilySymbol rectOpen = _doc.GetFamilySymbol("Extrusion", "Type 1");
                    FamilySymbol roundOpen = _doc.GetFamilySymbol("Extrusion_round", "Type 1");

                    tran.Commit();

                    foreach (Element intersector in intersectElement)
                    {
                        if (intersector is MEPCurve mep)
                        {
                            CutInMepCurve(tran, wall, mep, rectOpen, roundOpen);
                            continue;
                        }

                        CutBasicRectangular(tran, wall, intersector, rectOpen);
                    }
                }

                tranGr.Assimilate();
            }
        }

        private void CutBasicRectangular(Transaction trans, Wall wall, Element intersector, FamilySymbol rectOpen)
        {
            WallExtrusion extrusion = new WallExtrusion(intersector, wall);
            Curve extrusionCurve = extrusion.LocationCurve;

            trans.Start("Creating extrusion");

            var fi = _doc.Create.NewFamilyInstance(extrusionCurve, rectOpen, _doc.GetElement(intersector.LevelId) as Level, StructuralType.Beam);

            InstanceVoidCutUtils.AddInstanceVoidCut(_doc, wall, fi);

            fi.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION).Set(0);
            fi.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(1);
            fi.get_Parameter(BuiltInParameter.Y_JUSTIFICATION).Set(1);
            fi.LookupParameter("w").Set(extrusion.Width);
            fi.LookupParameter("h").Set(extrusion.Height);

            trans.Commit();

            trans.Start("Setting appropriate extrusion dimensions");
            SetAppropriateDimensions(intersector, fi, extrusion);
            trans.Commit();
        }

        private void CutInMepCurve(Transaction tran, Element wall, MEPCurve intersector, FamilySymbol rectOpen, FamilySymbol roundOpen)
        {
            Curve extrusionCurve = ((LocationCurve)intersector.Location).Curve;
            Connector connector = intersector.ConnectorManager.Connectors.Cast<Connector>().FirstOrDefault();

            double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);

            switch (connector?.Shape)
            {
                case ConnectorProfileType.Rectangular:

                    tran.Start("Creating wall penetration");

                    FamilyInstance fi = _doc.Create.NewFamilyInstance(extrusionCurve, rectOpen, _doc.GetElement(intersector.LevelId) as Level, StructuralType.Beam);
                    InstanceVoidCutUtils.AddInstanceVoidCut(_doc, wall, fi);

                    fi.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION).Set(0);
                    fi.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(1);
                    fi.get_Parameter(BuiltInParameter.Y_JUSTIFICATION).Set(1);
                    fi.LookupParameter("w").Set(connector.Width + offset);
                    fi.LookupParameter("h").Set(connector.Height + offset);

                    tran.Commit();

                    break;
                case ConnectorProfileType.Round:

                    tran.Start("Creating wall penetration");

                    fi = _doc.Create.NewFamilyInstance(extrusionCurve, roundOpen, _doc.GetElement(intersector.LevelId) as Level, StructuralType.Beam);

                    InstanceVoidCutUtils.AddInstanceVoidCut(_doc, wall, fi);

                    fi.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION).Set(0);
                    fi.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(1);
                    fi.get_Parameter(BuiltInParameter.Y_JUSTIFICATION).Set(1);
                    fi.LookupParameter("r").Set(connector.Radius * 1.2);

                    tran.Commit();

                    break;
            }
        }

        private static void SetAppropriateDimensions(Element intersector, Element element, WallExtrusion extrusion)
        {
            BoundingBoxXYZ extrusionBb = element.get_BoundingBox(null);
            var interBb = intersector.get_BoundingBox(null);

            double offset = UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS);

            if (!extrusionBb.Min.IsAlmostEqualTo(interBb.Min))
            {
                element.LookupParameter("h").Set(extrusion.Width + offset);
                element.LookupParameter("w").Set(extrusion.Height + offset);
            }
            else
            {
                element.LookupParameter("h").Set(extrusion.Height + offset);
                element.LookupParameter("w").Set(extrusion.Width + offset);
            }
        }
    }
}