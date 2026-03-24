namespace ClassLibrary1;

/// <summary>
///     The shared Message Model for the message added to the Queueu
/// </summary>
public class ImageMessageModel
{

    public string FileName { get; set; } = string.Empty;

    public DateTime UploadedOn { get; set; }

}
