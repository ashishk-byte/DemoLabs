using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using WebApplication1.DtoModels;


namespace WebApplication1.Services;


public class MyImagesBlobClientService
{
    private readonly BlobContainerClient _container;


    public MyImagesBlobClientService(IConfiguration config)
    {
        var conn = config["AzureBlobStorage:ConnectionString"];
        var containerName = config["AzureBlobStorage:ContainerName"];

        var client = new BlobServiceClient(conn);
        _container = client.GetBlobContainerClient(containerName);

        _container.CreateIfNotExists(PublicAccessType.Blob);
    }


    public async Task<string> UploadAsync(IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var blobClient = _container.GetBlobClient(fileName);

        using var stream = file.OpenReadStream();

        // to explicitly ensure to overwrite the file delete it first, since update API does not exist!
        await blobClient.DeleteIfExistsAsync();

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            });

        // return blobClient.Uri.ToString();
        return fileName;
    }


    public string GetBlobUrl(string fileName)
    {
        return _container.GetBlobClient(fileName).Uri.ToString();
    }


    public async Task<(Stream Content, string ContentType)> GetBlobItemAsync(string fileName)
    {
        var blobClient = _container.GetBlobClient(fileName);

        var response = await blobClient.DownloadAsync();

        var contentType = response.Value.Details.ContentType ?? "application/octet-stream";

        return (response.Value.Content, contentType);
    }


    public async Task<List<BlobImageItemDto>> GetAllAsync()
    {
        var result = new List<BlobImageItemDto>();

        await foreach (var blob in _container.GetBlobsAsync())
        {
            result.Add(new BlobImageItemDto
            {
                FileName = blob.Name,
                ContentType = blob.Properties.ContentType,
                UploadedOn = blob.Properties.CreatedOn,
                Size = blob.Properties.ContentLength
            });
        }

        return result;
    }


    public async Task DeleteAsync(string fileName)
    {
        var blobClient = _container.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();
    }

}