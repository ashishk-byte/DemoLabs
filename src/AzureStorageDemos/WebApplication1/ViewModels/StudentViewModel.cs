using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels;

public class StudentViewModel
{
    public string? RowKey { get; set; }


    [Display(Name = "Name of Student")]
    [Required(ErrorMessage = "{0} cannot be empty.")]
    [StringLength(maximumLength:100, 
                  MinimumLength = 2, 
                  ErrorMessage = "{0} must contain {1} to {2} characters.")]
    public string Name { get; set; } = string.Empty;


    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;


    [Range(minimum: 18, maximum: 65)]
    public int Age { get; set; }

}
