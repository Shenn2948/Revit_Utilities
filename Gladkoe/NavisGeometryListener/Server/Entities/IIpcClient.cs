using System.ServiceModel;

namespace Gladkoe.NavisGeometryListener.Server.Entities
{
    [ServiceContract]
    public interface IIpcClient
    {
        [OperationContract(IsOneWay = true)]
        void Send(string data);
    }
}