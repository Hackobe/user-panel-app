using System.ComponentModel.DataAnnotations;

namespace UserPanelApp.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email jest wymagany.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}