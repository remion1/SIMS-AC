using System.ComponentModel.DataAnnotations;
using Sims.Backend.Models;

namespace Sims.Backend.DTOs;

public sealed class RegisterUserDto
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 3,
        ErrorMessage = "Username must be between 3 and 100 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; } = UserRole.User;
}
