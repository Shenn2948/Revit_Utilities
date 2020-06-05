using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Media.Media3D;

namespace RevitUtils.Geometry.NavisGeometryListener.Server.Entities
{
    [ServiceContract]
    public interface IIpcClient
    {
        [OperationContract(IsOneWay = true)]
        void Send(List<Point3D[]> data);
    }
}