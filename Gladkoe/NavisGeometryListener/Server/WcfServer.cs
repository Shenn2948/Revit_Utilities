using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Media.Media3D;
using RevitUtils.Geometry.NavisGeometryListener.Server.Entities;

namespace RevitUtils.Geometry.NavisGeometryListener.Server
{
    public sealed class WcfServer : IIpcServer
    {
        private readonly ServiceHost _host;

        public WcfServer()
        {
            this._host = new ServiceHost(new Server(this), new Uri($"net.pipe://localhost/{nameof(IIpcClient)}"));
        }

        public event EventHandler<DataReceivedEventArgs> Received;

        public void Start()
        {
            this._host.Open();
        }

        public void Stop()
        {
            this._host.Close();
        }

        private void OnReceived(DataReceivedEventArgs e)
        {
            var handler = this.Received;

            handler?.Invoke(this, e);
        }

        void IDisposable.Dispose()
        {
            this.Stop();

            (this._host as IDisposable).Dispose();
        }

        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
        private class Server : IIpcClient
        {
            private readonly WcfServer _server;

            public Server(WcfServer server)
            {
                this._server = server;
            }

            public void Send(List<Point3D[]> data)
            {
                this._server.OnReceived(new DataReceivedEventArgs(data));
            }
        }
    }
}