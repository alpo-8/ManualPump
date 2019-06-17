using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using static MessagePack.MessagePackSerializer;

namespace ManualPump
{
    public static class RabbitMqPublisher
    {
        private const int Dimension = 4;
        public static Uri ConnectionString;

        private static IConnectionFactory _factory;
        private static readonly IConnection[] ConnectionSet = new IConnection[Dimension];
        private static readonly IModel[,] ChannelSet = new IModel[Dimension, Dimension];

        private static IConnectionFactory Factory()
            => _factory ??= new ConnectionFactory
            {
                Uri = ConnectionString,
                UseBackgroundThreadsForIO = true,
                DispatchConsumersAsync = true
            };

        private static IConnection Connection(int i)
            => ConnectionSet[i] = ConnectionSet[i]?.IsOpen == true
                ? ConnectionSet[i]
                : Factory()
                    .CreateConnection($"{Environment.MachineName}_{i}");

        private static IModel Channel((int i, int j) x)
            => ChannelSet[x.i, x.j] = ChannelSet[x.i, x.j]?.IsOpen == true
                ? ChannelSet[x.i, x.j]
                : Connection(x.i)
                    .CreateModel();

        public static void Publish(string exchange, Orderbook message, string routingKey = null)
            => Channel(TheRandom.Pair(Dimension))
                .BasicPublish(
                    exchange,
                    body: Serialize(message),
                    routingKey: routingKey ?? string.Empty,
                    mandatory: false,
                    basicProperties: new BasicProperties
                    {
                        Persistent = false,
                        DeliveryMode = 1
                    });
    }
}