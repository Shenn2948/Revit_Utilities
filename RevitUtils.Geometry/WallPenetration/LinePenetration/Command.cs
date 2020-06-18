using System;
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
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            try
            {
                var wallPickObject = uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "pick a wall");
                Wall wall = (Wall)doc.GetElement(wallPickObject);

                Reference interPickObject = uidoc.Selection.PickObject(ObjectType.Element, "pick a intersection element");
                Element intersectElement = doc.GetElement(interPickObject);

                //Solid solid = intersectElement.GetSolid(true);
                //PlanarFace topFace = solid.Faces.OfType<PlanarFace>().FirstOrDefault(f => Util.PointsUpwards(f.FaceNormal));
                //XYZ centroid = solid.ComputeCentroid();

                //var transform = Transform.CreateTranslation(centroid);

                //var xx = transform.OfPoint(topFace.FaceNormal);
                //var perpend = Line.CreateBound(transform.Origin, xx);

                //using (var tran = new Transaction(doc))
                //{
                //    tran.Start("Add line");

                //    Creator.CreateModelCurve(uidoc.Application, perpend);

                //    tran.Commit();
                //}


                WallExtrusion extrusion = new WallExtrusion(intersectElement, wall);
                Curve extrusionCurve = extrusion.LocationCurve;

                using (TransactionGroup transGroup = new TransactionGroup(doc))
                {
                    transGroup.Start("Wall extrusion");

                    using (Transaction trans = new Transaction(doc))
                    {
                        trans.Start("Creating extrusion");

                        FamilySymbol rectOpen = doc.GetFamilySymbol("Extrusion", "Type 1");
                        var fi = doc.Create.NewFamilyInstance(extrusionCurve, rectOpen, doc.GetElement(intersectElement.LevelId) as Level, StructuralType.Beam);
                        ((LocationCurve)fi.Location).Curve = extrusionCurve;
                        InstanceVoidCutUtils.AddInstanceVoidCut(doc, wall, fi);
                        fi.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION).Set(0);
                        fi.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(1);
                        fi.get_Parameter(BuiltInParameter.Y_JUSTIFICATION).Set(1);
                        fi.LookupParameter("w").Set(extrusion.Width);
                        fi.LookupParameter("h").Set(extrusion.Height);

                        if (trans.Commit() != TransactionStatus.Committed)
                        {
                            return Result.Failed;
                        }

                        trans.Start("Setting appropriate extrusion dimensions");

                        BoundingBoxXYZ extrusionBb = fi.get_BoundingBox(null);
                        var interBb = intersectElement.get_BoundingBox(null);
                        if (!extrusionBb.Min.IsAlmostEqualTo(interBb.Min))
                        {
                            fi.LookupParameter("h").Set(extrusion.Width);
                            fi.LookupParameter("w").Set(extrusion.Height);
                        }

                        if (trans.Commit() != TransactionStatus.Committed)
                        {
                            return Result.Failed;
                        }

                        trans.Start("IncreaseSizes");

                        var w = fi.LookupParameter("w");
                        w.Set(w.AsDouble() + w.AsDouble() * 0.2);
                        var h = fi.LookupParameter("h");
                        h.Set(h.AsDouble() + h.AsDouble() * 0.2);

                        if (trans.Commit() != TransactionStatus.Committed)
                        {
                            return Result.Failed;
                        }
                    }

                    transGroup.Assimilate();
                }
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