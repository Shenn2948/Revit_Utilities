using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitUtils.DataAccess.Extensions;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace RevitUtils.Geometry.RoofPenetration
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private Document _doc;
        private UIDocument _uidoc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            _uidoc = uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;

            try
            {
                using (TransactionGroup transGroup = new TransactionGroup(_doc))
                {
                    transGroup.Start("Placing a void family");

                    using (var trans = new Transaction(_doc))
                    {
                        trans.Start("Placing a void family");

                        FamilyInstance fi = CreateVoid();
                        fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).Set(UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS));
                        if (trans.Commit() != TransactionStatus.Committed)
                        {
                            return Result.Failed;
                        }

                        IEnumerable<Element> intersects = GetIntersectsBoundingBox(fi);

                        trans.Start("Adding cuts");

                        foreach (Element intersect in intersects)
                        {
                            if (InstanceVoidCutUtils.CanBeCutWithVoid(intersect))
                            {
                                InstanceVoidCutUtils.AddInstanceVoidCut(_doc, intersect, fi);
                            }
                        }

                        if (trans.Commit() != TransactionStatus.Committed)
                        {
                            return Result.Failed;
                        }

                        transGroup.Assimilate();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return Result.Failed;
            }
            catch (Exception e)
            {
                e.ShowRevitDialog();
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        private IEnumerable<Element> GetIntersectsBoundingBox(Element e)
        {
            BoundingBoxXYZ bb = e.get_BoundingBox(null);

            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .WhereElementIsViewIndependent()
                                                     .WherePasses(new BoundingBoxIntersectsFilter(new Outline(bb.Min, bb.Max)))
                                                     .ToElements();
        }

        private FamilyInstance CreateVoid()
        {
            var pointRef = _uidoc.Selection.PickObject(ObjectType.PointOnElement, "pick a point");

            FamilySymbol open = new FilteredElementCollector(_doc).OfClass(typeof(FamilySymbol))
                                                                  .OfType<FamilySymbol>()
                                                                  .FirstOrDefault(x => x.FamilyName == "Family1" && x.Name == "Family1");

            return _doc.Create.NewFamilyInstance(pointRef, pointRef.GlobalPoint, new XYZ(1, 0, 0), open);
        }
    }
}