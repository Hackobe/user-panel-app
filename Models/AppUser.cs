using System.ComponentModel.DataAnnotations;

namespace UserPanelApp.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<UserNote> Notes { get; set; } = new();
}