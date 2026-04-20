using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NosVemos.Auditoria.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _rabbitSettings;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        Microsoft.Extensions.Options.IOptions<RabbitMqSettings> rabbitOptions,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitOptions.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitSettings.HostName,
                Port = _rabbitSettings.Port,
                UserName = _rabbitSettings.UserName,
                Password = _rabbitSettings.Password,
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(_rabbitSettings.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            channel.QueueDeclare(_rabbitSettings.Queue, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(_rabbitSettings.Queue, _rabbitSettings.Exchange, "expediente.creado");
            channel.QueueBind(_rabbitSettings.Queue, _rabbitSettings.Exchange, "expediente.cerrado");
            channel.QueueBind(_rabbitSettings.Queue, _rabbitSettings.Exchange, "ia.camara.analizado");
            channel.QueueBind(_rabbitSettings.Queue, _rabbitSettings.Exchange, "ia.rostro.reconocido");
            channel.QueueBind(_rabbitSettings.Queue, _rabbitSettings.Exchange, "sensor.proximidad.detectada");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AuditoriaDbContext>();
                    db.Eventos.Add(new AuditoriaEvento
                    {
                        Fecha = DateTime.UtcNow,
                        RoutingKey = ea.RoutingKey,
                        Payload = message
                    });
                    await db.SaveChangesAsync(stoppingToken);
                }

                _logger.LogInformation(
                    "Evento auditoria recibido. RoutingKey={RoutingKey} Payload={Payload}",
                    ea.RoutingKey,
                    message);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
                await Task.CompletedTask;
            };

            channel.BasicConsume(_rabbitSettings.Queue, autoAck: false, consumer);
            _logger.LogInformation("Auditoria consumer escuchando cola {Queue}", _rabbitSettings.Queue);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Auditoria worker detenido por cancelacion.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auditoria worker finalizo con error.");
        }
    }
}

public sealed class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5673;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "nosvemos.domain.events";
    public string Queue { get; set; } = "nosvemos.auditoria.events";
}
