using System.ComponentModel.DataAnnotations;

namespace Sims.Backend.Models;

public sealed class User
{
    public int Id { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    // Niemals über API-Responses exponieren
    [Required(AllowEmptyStrings = false)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
