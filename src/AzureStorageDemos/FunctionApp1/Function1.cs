using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClassLibrary1;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace FunctionApp1;


public class Function1
{

    private const string QueueNAME = "manoj-queue";

    private readonly ILogger<Function1> _logger;
    private readonly IConfiguration _config;


    public Function1(
        ILogger<Function1> logger,
        IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }


    [Function(name: "ProcessImage")]
    public async Task Run(
        [QueueTrigger(QueueNAME, Connection = "AzureWebJobsStorage")] QueueMessage message,
        FunctionContext context,
        int dequeueCount)
    {
        try
        {
            _logger.LogInformation($"Queue message received: {message.MessageText}");

            var data = JsonSerializer.Deserialize<ImageMessageModel>(message.MessageText);
            if (data is null)
            {
                throw new InvalidCastException($"Unable to extract ImageMessageModel from '{message.MessageText}'");
            }


            // DEMO: force failure for specific file containing the phrase "FAIL"
            if (data.FileName.Contains("fail") == true)
            {
                _logger.LogWarning("Forcing failure for DEMO ONLY! - Retry attempt #: {count}", dequeueCount);
                throw new Exception("Demo failure!");
            }


            bool allOk = await GenerateThumbnail(context, data);
            if (!allOk)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue message in Azure Function!");
            throw;          // required for triggering the RETRY Mechanism
        }

        _logger.LogInformation("Azure Function ran at {timeStamp}", System.DateTimeOffset.UtcNow);
    }


    private async Task<bool> GenerateThumbnail(FunctionContext context, ImageMessageModel data)
    {
        var connectionString = _config["AzureWebJobsStorage"];
        var sourceImageContainerName = _config["BlobContainerName"];
        var processedImageContainerName = _config["ProcessedBlobContainerName"];

        var blobServiceClient = new BlobServiceClient(connectionString);
        var sourceContainer = blobServiceClient.GetBlobContainerClient(sourceImageContainerName);
        var processedContainer = blobServiceClient.GetBlobContainerClient(processedImageContainerName);

        await processedContainer.CreateIfNotExistsAsync();

        var processedFileName = $"processed_{data.FileName}";

        var sourceBlob = sourceContainer.GetBlobClient(data.FileName);
        if (!await sourceBlob.ExistsAsync())
        {
            _logger.LogWarning($"Blob not found: {data.FileName}");
            return false;
        }

        var targetBlob = processedContainer.GetBlobClient(processedFileName);

        // Download original image
        var download = await sourceBlob.DownloadAsync();
        using var stream = download.Value.Content;

        // Create the thumbnail image
        using var image = await Image.LoadAsync(stream);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(200, 200),
            Mode = ResizeMode.Max
        }));
        using var outputStream = new MemoryStream();
        await image.SaveAsPngAsync(outputStream);

        // upload the generated thumbnail image to processed images blob container
        outputStream.Position = 0;
        await targetBlob.UploadAsync(
            outputStream,
            new Azure.Storage.Blobs.Models.BlobUploadOptions
            {
                HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
                {
                    ContentType = "image/png"
                }
            });

        _logger.LogInformation($"Processed image saved as: {processedFileName}");
        return true;
    }

}
