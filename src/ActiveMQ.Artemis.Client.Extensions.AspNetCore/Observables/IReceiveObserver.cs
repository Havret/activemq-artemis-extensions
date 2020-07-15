namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    public interface IReceiveObserver
    {
        void PreReceive(string address, RoutingType routingType, string queue, Message message);
        void PostReceive(string address, RoutingType routingType, string queue, Message message);
    }
}