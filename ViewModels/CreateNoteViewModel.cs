using System.ComponentModel.DataAnnotations;

namespace UserPanelApp.ViewModels;

public class CreateNoteViewModel
{
    [Required(ErrorMessage = "Tytuł jest wymagany.")]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Treść jest wymagana.")]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}