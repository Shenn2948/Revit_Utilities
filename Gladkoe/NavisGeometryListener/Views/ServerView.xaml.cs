using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitUtils.DataAccess.Entities.Handlers;
using ZetaIpc.Runtime.Server;

namespace RevitUtils.Geometry.NavisGeometryListener.Views
{
    public partial class ServerView : Window
    {
        private readonly ExternalEventHandler _eventHandler;
        private readonly ExternalEvent _externalEvent;
        private readonly IpcServer _server;
        private readonly Document _doc;

        public ServerView(UIDocument uidoc, ExternalEventHandler eventHandler, ExternalEvent externalEvent)
        {
            _eventHandler = eventHandler;
            _externalEvent = externalEvent;
            _doc = uidoc.Document;
            InitializeComponent();

            _server = new IpcServer();
            _server.ReceivedRequest += ServerOnReceivedRequest;
        }

        private void ServerOnReceivedRequest(object sender, ReceivedRequestEventArgs e)
        {
            var projectLocation = _doc.ActiveProjectLocation;
            Transform t = projectLocation.GetTotalTransform();

            List<GeometryPoint[]> points = JsonConvert.DeserializeObject<List<GeometryPoint[]>>(e.Request);

            List<List<XYZ>> s = points.Select(x =>
                                      {
                                          var p1 = ToXyz(x[0]);
                                          var p2 = ToXyz(x[1]);
                                          var p3 = ToXyz(x[2]);

                                          var t1 = t.OfPoint(p1);
                                          var t2 = t.OfPoint(p2);
                                          var t3 = t.OfPoint(p3);

                                          return new List<XYZ> { t1, t2, t3 };
                                      })
                                      .ToList();

            Execute(s);

            e.Handled = true;
        }

        private void StopServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _server.Stop();
        }

        private void StartServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _server.Start(31306);
        }

        private void ServerView_OnClosed(object sender, EventArgs e)
        {
            _server.Stop();
        }

        private static XYZ ToXyz(GeometryPoint p)
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
                builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
                builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
                builder.Build();

                TessellatedShapeBuilderResult result = builder.GetBuildResult();

                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));

                ds.ApplicationId = Assembly.GetExecutingAssembly().GetType().GUID.ToString();
                ds.ApplicationDataId = Guid.NewGuid().ToString();
                ds.Name = "NavisWorksShape";
                DirectShapeOptions dsOptions = ds.GetOptions();
                dsOptions.ReferencingOption = DirectShapeReferencingOption.Referenceable;
                ds.SetOptions(dsOptions);

                ds.SetShape(result.GetGeometricalObjects());
            }

            _eventHandler.Action = Run;
            _eventHandler.TransactionName = "Importing navisWorks elements";

            _externalEvent.Raise();
        }
    }
}