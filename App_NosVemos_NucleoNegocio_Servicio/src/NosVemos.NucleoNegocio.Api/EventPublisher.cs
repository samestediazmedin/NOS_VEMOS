using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

internal sealed class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5673;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "nosvemos.domain.events";
}

internal interface IEventPublisher
{
    Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default);
}

internal sealed class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class RabbitMqEventPublisher(RabbitMqSettings settings, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(settings.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: settings.Exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            logger.LogInformation("Evento publicado: {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo publicar evento {RoutingKey}", routingKey);
        }

        return Task.CompletedTask;
    }
}
