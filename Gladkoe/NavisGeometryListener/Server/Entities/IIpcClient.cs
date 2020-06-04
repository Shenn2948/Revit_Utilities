using System.ServiceModel;

namespace RevitUtils.Geometry.NavisGeometryListener.Server.Entities
{
    [ServiceContract]
    public interface IIpcClient
    {
        [OperationContract(IsOneWay = true)]
        void Send(string data);
    }
}