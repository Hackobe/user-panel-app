using System.ComponentModel.DataAnnotations;

namespace UserPanelApp.Models;

public class UserNote
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? AppUser { get; set; }
}