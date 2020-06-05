using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitUtils.DataAccess.Entities.Handlers;
using RevitUtils.Geometry.NavisGeometryListener.Server;
using DataReceivedEventArgs = RevitUtils.Geometry.NavisGeometryListener.Server.Entities.DataReceivedEventArgs;

namespace RevitUtils.Geometry.NavisGeometryListener.Views
{
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : Window
    {
        private readonly UIDocument _uidoc;
        private readonly ExternalEventHandler _eventHandler;
        private readonly ExternalEvent _externalEvent;
        private readonly WcfServer _server;
        private readonly Document _doc;

        public ServerView(UIDocument uidoc, ExternalEventHandler eventHandler, ExternalEvent externalEvent)
        {
            _uidoc = uidoc;
            _eventHandler = eventHandler;
            _externalEvent = externalEvent;
            _doc = uidoc.Document;
            InitializeComponent();

            _server = new WcfServer();
            _server.Received += ServerOnReceived;
        }

        private void ServerOnReceived(object sender, DataReceivedEventArgs e)
        {
            var projectLocation = _doc.ActiveProjectLocation;
            XYZ origin = new XYZ(0, 0, 0);
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
            XYZ projP = new XYZ(position.EastWest, position.NorthSouth, position.Elevation);

            // Rotate the point (110,110,0) around the base point (100,100,0) where the angle is 60 degrees
            //Transform rot = Transform.CreateRotationAtPoint(XYZ.BasisZ, position.Angle, projP);
            //Transform pos = Transform.CreateTranslation(new XYZ(110, 110, 0));
            //Transform rotPos = pos.Multiply(rot);

            Transform rot = Transform.CreateRotation(XYZ.BasisZ, position.Angle);
            //XYZ transformed = rot.OfPoint(new XYZ(110, 110, 0));

            List<List<XYZ>> s = e.Data.Select(x =>
                                 {
                                     var p1 = ToXyz(x[0]).Subtract(projP);
                                     var p2 = ToXyz(x[1]).Subtract(projP);
                                     var p3 = ToXyz(x[2]).Subtract(projP);

                                     Transform rot1 = Transform.CreateRotationAtPoint(XYZ.BasisZ, position.Angle, p1);
                                     Transform rot2 = Transform.CreateRotationAtPoint(XYZ.BasisZ, position.Angle, p2);
                                     Transform rot3 = Transform.CreateRotationAtPoint(XYZ.BasisZ, position.Angle, p3);
                                     var transformed1 = rot.OfVector(p1);
                                     var transformed2 = rot.OfVector(p2);
                                     var transformed3 = rot.OfVector(p3);

                                     return new List<XYZ> { transformed1, transformed2, transformed3 };
                                 })
                                 .ToList();

            //Execute(s);
        }

        private void StopServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _server.Stop();
        }

        private void StartServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _server.Start();
        }

        private void ServerView_OnClosed(object sender, EventArgs e)
        {
            _server.Stop();
        }

        private static XYZ ToXyz(Point3D p)
        {
            return new XYZ(p.X, p.Y, p.Z);
        }

        private void Execute(IEnumerable<List<XYZ>> triangles)
        {
            void Run(Document doc)
            {
                TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                builder.OpenConnectedFaceSet(false);

                foreach (List<XYZ> triangle in triangles)
                {
                    TessellatedFace tessellatedFace = new TessellatedFace(triangle, ElementId.InvalidElementId);

                    if (builder.DoesFaceHaveEnoughLoopsAndVertices(tessellatedFace))
                    {
                        builder.AddFace(tessellatedFace);
                    }
                }

                builder.CloseConnectedFaceSet();
                builder.Build();
                TessellatedShapeBuilderResult result = builder.GetBuildResult();

                ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                DirectShape ds = DirectShape.CreateElement(doc, categoryId);
                ds.SetShape(result.GetGeometricalObjects());
                ds.Name = "MyShape";
            }

            _eventHandler.Action = Run;
            _eventHandler.TransactionName = "Creating elements";

            _externalEvent.Raise();
        }
    }
}