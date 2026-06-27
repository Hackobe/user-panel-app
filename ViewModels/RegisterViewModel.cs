using System.ComponentModel.DataAnnotations;

namespace UserPanelApp.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email jest wymagany.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane.")]
    [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}