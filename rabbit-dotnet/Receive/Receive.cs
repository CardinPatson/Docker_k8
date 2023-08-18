using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest" , Password = "guest", Port = 5672};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

Console.WriteLine($"[*] waiting for messages to arrive");

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, ea) => 
{
  var body = ea.Body.ToArray();
  var message = Encoding.UTF8.GetString(body);
  Console.WriteLine($"[x] Received {message}");
};

channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);

Console.WriteLine("Press [enter] to exit ");
Console.ReadLine();