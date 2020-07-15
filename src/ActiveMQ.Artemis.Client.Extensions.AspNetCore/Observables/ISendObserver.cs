namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore
{
    public interface ISendObserver
    {
        void PreSend(string address, RoutingType? routingType, Message message);
        void PostSend(string address, RoutingType? routingType, Message message);
    }
}