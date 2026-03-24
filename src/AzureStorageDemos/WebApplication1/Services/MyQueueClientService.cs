using System.Text.Json;
using Azure.Storage.Queues;

using ClassLibrary1;


namespace WebApplication1.Services;


public class MyQueueClientService
{

    private readonly QueueClient _queue;


    public MyQueueClientService(IConfiguration config)
    {
        var conn = config["AzureQueueStorage:ConnectionString"];
        var queueName = config["AzureQueueStorage:QueueName"];

        _queue = new QueueClient(conn, queueName);
        _queue.CreateIfNotExists();
    }


    public async Task SendMessageAsync(string fileName)
    {
        var messageObj = new ImageMessageModel
        {
            FileName = fileName,
            UploadedOn = DateTime.UtcNow
        };

        var message = JsonSerializer.Serialize(messageObj);

        await _queue.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message)));
    }

}