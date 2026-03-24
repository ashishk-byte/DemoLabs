namespace WebApplication1.DtoModels;

public class BlobImageItemDto
{
    public string FileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long? Size { get; set;  }

    public DateTimeOffset? UploadedOn { get; set; }

}