using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GeometRi;
using RevitUtils.DataAccess.Extensions;
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

            try
            {
                if (_doc.ActiveView is View3D view3D)
                {
                    (Element element, Transform transform) = PickInstance();
                    Edge edge = PickEdge(element);
                    //PlanarFace face = PickFace(element);

                    //SetCamera(transform, element, edge, view3D);
                    //SetCamera(element, edge, face, view3D);
                    SetCamera(element, edge, view3D);
                }
            }
            catch (Exception e)
            {
                e.ShowRevitDialog();
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        private void SetCamera(Element element, Edge edge, View3D view3D)
        {
            if (edge.AsCurve() is Line locationCurve)
            {
                var vec = Util.GetVector(locationCurve);

                XYZ upDirection = GetUpDirection(edge, locationCurve);
                if (upDirection == null)
                {
                    return;
                }

                view3D.SetOrientation(new ViewOrientation3D(locationCurve.Origin, upDirection, vec));
                _uidoc.ShowElements(element);
                _uidoc.RefreshActiveView();
            }
        }

        private static XYZ DetermineMostSuitableNormal(IReadOnlyCollection<XYZ> normals)
        {
            var upVec = normals.Where(x => x != null).FirstOrDefault(Util.PointsUpwards);

            return upVec ?? normals.First(x => x != null).Negate();
        }

        private static XYZ GetUpDirection(Edge edge, Line loc)
        {
            XYZ f1n = ComputeFaceNormal(edge.GetFace(0), loc);
            XYZ f2n = ComputeFaceNormal(edge.GetFace(1), loc);

            return DetermineMostSuitableNormal(new List<XYZ> { f1n, f2n });
        }

        private static XYZ ComputeFaceNormal(Face f, Line locationCurve)
        {
            if (f == null)
            {
                return null;
            }

            var uv = f.Project(locationCurve.Origin)?.UVPoint;

            return uv == null
                       ? null
                       : f.ComputeNormal(uv);
        }

        private void SetCamera(Transform transform, Element element, Edge edge, View3D view3D)
        {
            if (edge.AsCurve() is Line locationCurve)
            {
                //(Line perpendLine, Line orthLine) = PerpendLine(transform, locationCurve);
                (Line perpendLine, Line orthLine) = PerpendLine(locationCurve);

                using (var tran = new Transaction(_doc))
                {
                    tran.Start("Add line");

                    Creator.CreateModelCurve(_uidoc.Application, locationCurve);
                    Creator.CreateModelCurve(_uidoc.Application, perpendLine);
                    Creator.CreateModelCurve(_uidoc.Application, orthLine);

                    tran.Commit();
                }

                //view3D.SetOrientation(new ViewOrientation3D(transform.Origin, Util.GetVector(perpendLine), Util.GetVector(elementDirectionLine)));
                //_uidoc.ShowElements(element);
                //_uidoc.RefreshActiveView();
            }
        }

        private static (Line perpendLine, Line orthLine) PerpendLine(Transform transform, Line locationCurve)
        {
            Vector3d vector3d = locationCurve.Direction.ToVector3d();
            Vector3d crossVector3d = vector3d.Cross(vector3d.OrthogonalVector);
            XYZ cross = crossVector3d.ToXyz();
            cross = transform.OfPoint(cross);
            Line perpendLine = Line.CreateBound(transform.Origin, cross);

            XYZ orth = vector3d.OrthogonalVector.ToXyz();
            orth = transform.OfPoint(orth);
            Line orthLine = Line.CreateBound(transform.Origin, orth);
            return (perpendLine, orthLine);
        }

        private static (Line perpendLine, Line orthLine) PerpendLine(Line locationCurve)
        {
            XYZ vector = Util.GetVector(locationCurve);
            Transform t = Transform.CreateTranslation(vector);

            Vector3d vector3d = vector.ToVector3d();
            Vector3d crossVector3d = vector3d.Cross(vector3d.OrthogonalVector);
            XYZ cross = crossVector3d.ToXyz();

            cross = t.OfPoint(cross);
            Line perpendLine = Line.CreateBound(locationCurve.Origin, cross);

            XYZ orth = vector3d.OrthogonalVector.ToXyz();
            orth = t.OfPoint(orth);
            Line orthLine = Line.CreateBound(locationCurve.Origin, orth);
            return (perpendLine, orthLine);
        }

        private (Element element, Transform transform) PickInstance()
        {
            var elementRef = _uidoc.Selection.PickObject(ObjectType.Element, new InstanceSelectionFilter(), "Выберите семейство либо сборку.");
            var element = _doc.GetElement(elementRef);
            Transform transform = null;

            if (element is Instance instance)
            {
                transform = instance.GetTotalTransform();
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

        private PlanarFace PickFace(Element instance)
        {
            var edgeRef = _uidoc.Selection.PickObject(ObjectType.Face, new FaceSelectionFilter(instance), "Выберите поверхность, параллельную траектории.");
            GeometryObject geoObject = _doc.GetElement(edgeRef).GetGeometryObjectFromReference(edgeRef);
            return geoObject as PlanarFace;
        }

        private XYZ AngledVector(XYZ xyz, double angle, string s)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            switch (s)
            {
                case "x":
                    var y = xyz.Y * cos - xyz.Z * sin;
                    var z = xyz.Y * sin + xyz.Z * cos;

                    return new XYZ(xyz.X, y, z);
                case "y":
                    var x = xyz.X * cos + xyz.Z * sin;
                    z = -xyz.X * sin + xyz.Z * cos;

                    return new XYZ(x, xyz.Y, z);
                case "z":
                    x = xyz.X * cos - xyz.Y * sin;
                    y = xyz.X * sin + xyz.Y * cos;

                    return new XYZ(x, y, xyz.Z);
            }

            return new XYZ();
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
            return false;
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

    public class FaceSelectionFilter : ISelectionFilter
    {
        private readonly Element _element;
        private readonly Document _doc;

        public FaceSelectionFilter(Element element)
        {
            _doc = element.Document;
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
            return _doc.GetElement(reference).GetGeometryObjectFromReference(reference) is PlanarFace;
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

    public static class GriExtensions
    {
        public static XYZ ToXyz(this Point3d p)
        {
            return new XYZ(p.X, p.Y, p.Z);
        }

        public static Point3d ToPoint3d(this XYZ p)
        {
            return new Point3d(p.X, p.Y, p.Z);
        }

        public static Vector3d ToVector3d(this XYZ p)
        {
            return new Vector3d(p.X, p.Y, p.Z);
        }

        public static XYZ ToXyz(this Vector3d p)
        {
            return new XYZ(p.X, p.Y, p.Z);
        }
    }
}