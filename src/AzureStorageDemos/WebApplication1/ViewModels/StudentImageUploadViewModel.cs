using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels;


public class StudentImageUploadViewModel
    : IValidatableObject
{

    [Required(ErrorMessage = "Image file cannot be empty!")]
    [Display(Name = "Select an Image file")]
    public IFormFile? ImageFile { get; set; }


    public string? ImageFileName { get; set;  }


    #region System.ComponentModel.DataAnnotations.IValidatableObject members

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var allowedTypes = new[] {
            System.Net.Mime.MediaTypeNames.Image.Png,
            System.Net.Mime.MediaTypeNames.Image.Jpeg,
            System.Net.Mime.MediaTypeNames.Image.Gif
        };

        if (ImageFile != null && !allowedTypes.Contains(ImageFile.ContentType))
        {
            yield return new ValidationResult(
                "Only JPG, PNG, or GIF images are allowed.",
                new[] { nameof(ImageFile) });
        }
    }

    #endregion

}
