using System.ComponentModel.DataAnnotations;

namespace Sims.Backend.DTOs;

/// <summary>Credentials for authenticating an existing user.</summary>
public sealed class LoginDto
{
    [Required(AllowEmptyStrings = false)]
    public string Username { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = string.Empty;
}
