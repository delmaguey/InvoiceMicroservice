using MessageContracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
var factory = new ConnectionFactory { HostName = "localhost" };

//replace HostName with your RabbitMQ server address if not running locally

using var connection = await factory.CreateConnectionAsync();
using (var channel = await connection.CreateChannelAsync())
{
await channel.QueueDeclareAsync(queue: "invoice-service", durable: false, exclusive: false, autoDelete: false, arguments: null);

Console.WriteLine($" [*] Waiting for messages. - {DateTime.Now}");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = JsonSerializer.Deserialize<InvoiceCreated>(body);
    Console.WriteLine($" [x] Received invoice number: " +
    $"{message?.InvoiceNumber}");
    return Task.CompletedTask;
};

//for competing consumers

await channel.BasicConsumeAsync("invoice-service", autoAck: true, consumer: consumer);
    
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

};