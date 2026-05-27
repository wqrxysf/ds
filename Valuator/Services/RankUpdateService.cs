using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Valuator.Hubs;

namespace Valuator.Services;

public class RankUpdateService : BackgroundService
{
    private readonly ILogger<RankUpdateService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<RabbitMQSettings> _rabbitSettings;
    public RankUpdateService(ILogger<RankUpdateService> logger, IServiceProvider serviceProvider, IOptions<RabbitMQSettings> rabbitSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _rabbitSettings = rabbitSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис обновления Rank запущен");

        try
        {
            var factory = new ConnectionFactory 
            {
                HostName = _rabbitSettings.Value.Host,
                UserName = _rabbitSettings.Value.Username,
                Password = _rabbitSettings.Value.Password
            };
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: "events.rank.fanout",
                type: "fanout",
                durable: true
            );

            var queueResult = await channel.QueueDeclareAsync(
                queue: "",
                exclusive: true,
                autoDelete: true
            );
            var queueName = queueResult.QueueName;

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: "events.rank.fanout",
                routingKey: ""
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (ch, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var evt = JsonSerializer.Deserialize<RankCalculatedEvent>(message);

                    if (evt != null)
                    {
                        _logger.LogInformation($"Событие получено: Id={evt.Id}, Rank={evt.Rank}");

                        using var scope = _serviceProvider.CreateScope();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<RankHub>>();

                        await hubContext.Clients.All.SendAsync("ReceiveRankUpdate", new
                        {
                            Id = evt.Id,
                            Rank = evt.Rank
                        }, stoppingToken);

                        _logger.LogInformation($"Отправлено в браузер: {evt.Id}");
                    }
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки");
                }
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка сервиса");
        }
    }
}
public class RankCalculatedEvent
{
    public string Id { get; set; } = string.Empty;
    public double Rank { get; set; }
}