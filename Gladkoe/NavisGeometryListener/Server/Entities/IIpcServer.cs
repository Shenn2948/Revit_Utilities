using System;

namespace RevitUtils.Geometry.NavisGeometryListener.Server.Entities
{
    public interface IIpcServer : IDisposable
    {
        void Start();
        void Stop();

        event EventHandler<DataReceivedEventArgs> Received;
    }
}