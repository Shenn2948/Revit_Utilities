using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitUtils.Geometry.Utils;

namespace RevitUtils.Geometry.RotateCamera
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private UIDocument _uidoc;
        private Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            _uidoc = uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;

            if (_doc.ActiveView is View3D view3D)
            {
                (Element element, Transform transform) = PickInstance();
                Edge edge = PickEdge(element);

                SetCamera(transform, element, edge, view3D);
            }

            return Result.Succeeded;
        }

        private void SetCamera(Transform transform, Element element, Edge edge, View3D view3D)
        {
            if (edge.AsCurve() is Line locationCurve)
            {
                XYZ dir = transform.OfPoint(locationCurve.Direction);

                var l = Line.CreateBound(transform.Origin, dir);
                XYZ dir2 = XYZ.BasisZ;

                if (Util.IsVertical(locationCurve.Direction))
                {
                    dir2 = XYZ.BasisY;
                }

                var xx = transform.OfPoint(dir2);

                var perpend = Line.CreateBound(transform.Origin, xx);

                //using (var tran = new Transaction(_doc))
                //{
                //    tran.Start("Add line");

                //    Creator.CreateModelCurve(_uidoc.Application, l);
                //    Creator.CreateModelCurve(_uidoc.Application, perpend);

                //    tran.Commit();
                //}

                view3D.SetOrientation(new ViewOrientation3D(transform.Origin, Util.GetVector(perpend), Util.GetVector(l)));
                _uidoc.ShowElements(element);
                _uidoc.RefreshActiveView();
            }
        }

        private (Element element, Transform transform) PickInstance()
        {
            var elementRef = _uidoc.Selection.PickObject(ObjectType.Element, new InstanceSelectionFilter(), "Выберите семейство либо сборку.");
            var element = _doc.GetElement(elementRef);
            Transform transform = null;

            if (element is Instance instance)
            {
                transform = instance.GetTransform();
            }
            else if (element is AssemblyInstance assemblyInstance)
            {
                transform = assemblyInstance.GetTransform();
            }

            return (element, transform);
        }

        private Edge PickEdge(Element instance)
        {
            var edgeRef = _uidoc.Selection.PickObject(ObjectType.Edge,
                                                      new EdgeSelectionFilter(instance),
                                                      "Выберите траекторию, перпендикулярно которой будет расположена точка обзора.");
            GeometryObject geoObject = _doc.GetElement(edgeRef).GetGeometryObjectFromReference(edgeRef);
            Edge edge = geoObject as Edge;
            return edge;
        }
    }

    public class InstanceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return e is Instance || e is AssemblyInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class EdgeSelectionFilter : ISelectionFilter
    {
        private readonly Element _element;

        public EdgeSelectionFilter(Element element)
        {
            _element = element;
        }

        public bool AllowElement(Element elem)
        {
            if (_element is AssemblyInstance assemblyInstance)
            {
                var memberIds = assemblyInstance.GetMemberIds();

                return memberIds.Contains(elem.Id);
            }

            return elem.Id == _element.Id;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class Creator
    {
        public static ModelCurve CreateModelCurve(UIApplication app, Line line)
        {
            SketchPlane sketchPlane = NewSketchPlaneContainCurve(line, app);
            return app.ActiveUIDocument.Document.Create.NewModelCurve(line, sketchPlane);
        }

        private static SketchPlane NewSketchPlaneContainCurve(Line line, UIApplication app)
        {
            Plane plane = GetPlane(line);
            SketchPlane sketchPlane = SketchPlane.Create(app.ActiveUIDocument.Document, plane);
            return sketchPlane;
        }

        private static Plane GetPlane(Line line)
        {
            Plane plane = Plane.CreateByThreePoints(line.GetEndPoint(0), line.Direction, line.GetEndPoint(1));
            return plane;
        }
    }
}