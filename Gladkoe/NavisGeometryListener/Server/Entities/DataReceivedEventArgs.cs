using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace RevitUtils.Geometry.NavisGeometryListener.Server.Entities
{
    [Serializable]
    public sealed class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(List<Point3D[]> data)
        {
            this.Data = data;
        }

        public List<Point3D[]> Data { get; private set; }
    }
}